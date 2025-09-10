using DotNesJit.Core.Interfaces;
using DotNesJit.Core.Enums;
using DotNesJit.Hardware.CPU;
using DotNesJit.Hardware.PPU;
using DotNesJit.Hardware.Memory;
using DotNesJit.Hardware.Input;

namespace DotNesJit.Hardware;

/// <summary>
/// Unified hardware abstraction layer for compatibility with JIT instruction handlers
/// This provides the same interface that the old NesHal class provided
/// </summary>
public class NESHardware
{
    private readonly Cpu6502 _cpu;
    private readonly Ppu2C02 _ppu;
    private readonly MemoryBus _memory;
    private readonly Controller[] _controllers = new Controller[2];

    public NESHardware(byte[] prgRom, byte[] chrRom)
    {
        _memory = new MemoryBus(prgRom, chrRom);
        _cpu = new Cpu6502(_memory);
        _ppu = new Ppu2C02();
        
        _controllers[0] = new Controller();
        _controllers[1] = new Controller();
    }

    // CPU-related methods that JIT instruction handlers expect
    public byte ReadMemory(ushort address) => _cpu.ReadMemory(address);
    public void WriteMemory(ushort address, byte value) => _cpu.WriteMemory(address, value);
    public ushort ReadMemory16(ushort address) => _cpu.ReadMemory16(address);
    public void WriteMemory16(ushort address, ushort value) => _cpu.WriteMemory16(address, value);

    public bool GetFlag(CpuStatusFlags flag) => _cpu.GetFlag(flag);
    public void SetFlag(CpuStatusFlags flag, bool value) => _cpu.SetFlag(flag, value);

    public byte GetAccumulator() => _cpu.GetAccumulator();
    public void SetAccumulator(byte value) => _cpu.SetAccumulator(value);
    public byte GetIndexX() => _cpu.GetIndexX();
    public void SetIndexX(byte value) => _cpu.SetIndexX(value);
    public byte GetIndexY() => _cpu.GetIndexY();
    public void SetIndexY(byte value) => _cpu.SetIndexY(value);

    public ushort GetProgramCounter() => _cpu.GetProgramCounter();
    public void SetProgramCounter(ushort address) => _cpu.SetProgramCounter(address);
    
    public byte GetStackPointer() => _cpu.GetStackPointer();
    public void SetStackPointer(byte value) => _cpu.SetStackPointer(value);

    public byte GetProcessorStatus() => _cpu.GetProcessorStatus();
    public void SetProcessorStatus(byte value) => _cpu.SetProcessorStatus(value);

    // Timing and synchronization methods
    public void WaitForVBlank() => _cpu.WaitForVBlank();
    public bool ExecuteCPUCycle() => _cpu.ExecuteCycle();

    // System control methods
    public void Reset()
    {
        _cpu.Reset();
        _ppu.Reset();
        _memory.Reset();
        _controllers[0].Reset();
        _controllers[1].Reset();
    }

    public void HandleNMI() => _cpu.HandleNMI();
    public void HandleIRQ() => _cpu.HandleIRQ();

    // Controller methods
    public void SetControllerState(int controller, NESController state)
    {
        if (controller >= 0 && controller < _controllers.Length)
        {
            _controllers[controller].State = state;
        }
    }

    public NESController GetControllerState(int controller)
    {
        return controller >= 0 && controller < _controllers.Length 
            ? _controllers[controller].State 
            : NESController.None;
    }

    // Methods for registering JIT functions
    public void RegisterJITFunction(ushort address, Func<bool> function, string name)
    {
        _cpu.RegisterJITFunction(address, function, name);
    }

    // Access to individual components
    public ICPU CPU => _cpu;
    public IPPU PPU => _ppu;
    public IMemory Memory => _memory;

    // Status and debugging methods
    public string GetCPUState() => $"PC:${_cpu.GetProgramCounter():X4} SP:${_cpu.GetStackPointer():X2} A:${_cpu.GetAccumulator():X2}";
    public string GetPPUStatus() => $"Scanline:{_ppu.GetState().Scanline} VBlank:{_ppu.IsInVBlank()}";
    public string GetInterruptState() => $"IRQ:{!_cpu.GetFlag(CpuStatusFlags.InterruptDisable)}";

    public (ushort nmi, ushort reset, ushort irq) GetInterruptVectors()
    {
        return (
            nmi: _memory.Read16(0xFFFA),
            reset: _memory.Read16(0xFFFC), 
            irq: _memory.Read16(0xFFFE)
        );
    }

    // Additional methods required by JIT instruction handlers
    
    /// <summary>
    /// Jump to specified address (used by JMP instruction)
    /// </summary>
    public void JumpToAddress(ushort address)
    {
        _cpu.SetProgramCounter(address);
    }

    /// <summary>
    /// Call function at specified address (used by JSR instruction)
    /// Pushes return address-1 to stack, then jumps to target
    /// </summary>
    public void CallFunction(ushort address)
    {
        // Get current PC (which points to next instruction after JSR)
        var returnAddress = (ushort)(_cpu.GetProgramCounter() - 1);
        
        // Push return address to stack (high byte first, then low byte)
        PushStack((byte)((returnAddress >> 8) & 0xFF));
        PushStack((byte)(returnAddress & 0xFF));
        
        // Jump to target address
        _cpu.SetProgramCounter(address);
    }

    /// <summary>
    /// Return from subroutine (used by RTS instruction)
    /// Pulls return address from stack and jumps to it + 1
    /// </summary>
    public void ReturnFromSubroutine()
    {
        // Pull return address from stack (low byte first, then high byte)
        var lowByte = PullStack();
        var highByte = PullStack();
        
        var returnAddress = (ushort)((highByte << 8) | lowByte);
        
        // RTS jumps to return address + 1
        _cpu.SetProgramCounter((ushort)(returnAddress + 1));
    }

    /// <summary>
    /// Return from interrupt (used by RTI instruction)
    /// Pulls processor status and return address from stack
    /// </summary>
    public void ReturnFromInterrupt()
    {
        // Pull processor status from stack
        var status = PullStack();
        _cpu.SetProcessorStatus(status);
        
        // Pull return address from stack (low byte first, then high byte)
        var lowByte = PullStack();
        var highByte = PullStack();
        
        var returnAddress = (ushort)((highByte << 8) | lowByte);
        _cpu.SetProgramCounter(returnAddress);
    }

    /// <summary>
    /// Trigger software interrupt (used by BRK instruction)
    /// </summary>
    public void TriggerSoftwareInterrupt()
    {
        // Get current PC + 2 (BRK is a 2-byte instruction)
        var pc = (ushort)(_cpu.GetProgramCounter() + 2);
        
        // Push PC to stack (high byte first, then low byte)
        PushStack((byte)((pc >> 8) & 0xFF));
        PushStack((byte)(pc & 0xFF));
        
        // Push processor status with B flag set
        var status = (byte)(_cpu.GetProcessorStatus() | 0x10); // Set B flag
        PushStack(status);
        
        // Set interrupt disable flag
        _cpu.SetFlag(CpuStatusFlags.InterruptDisable, true);
        
        // Jump to IRQ vector
        var irqVector = _memory.Read16(0xFFFE);
        _cpu.SetProgramCounter(irqVector);
    }

    /// <summary>
    /// Push byte to stack
    /// </summary>
    public void PushStack(byte value)
    {
        var sp = _cpu.GetStackPointer();
        _memory.Write((ushort)(0x100 + sp), value);
        _cpu.SetStackPointer((byte)(sp - 1));
    }

    /// <summary>
    /// Pull byte from stack
    /// </summary>
    public byte PullStack()
    {
        var sp = (byte)(_cpu.GetStackPointer() + 1);
        _cpu.SetStackPointer(sp);
        return _memory.Read((ushort)(0x100 + sp));
    }

    /// <summary>
    /// Push 16-bit address to stack (used by JSR and interrupts)
    /// </summary>
    public void PushAddress(ushort address)
    {
        PushStack((byte)((address >> 8) & 0xFF)); // High byte first
        PushStack((byte)(address & 0xFF));        // Low byte second
    }

    /// <summary>
    /// Pull 16-bit address from stack
    /// </summary>
    public ushort PullAddress()
    {
        var lowByte = PullStack();  // Low byte first
        var highByte = PullStack(); // High byte second
        return (ushort)((highByte << 8) | lowByte);
    }
}