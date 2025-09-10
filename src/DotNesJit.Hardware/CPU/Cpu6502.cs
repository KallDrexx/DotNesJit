using DotNesJit.Core.Interfaces;
using DotNesJit.Core.Models;
using DotNesJit.Core.Enums;

namespace DotNesJit.Hardware.CPU;

public class Cpu6502 : ICPU
{
    private byte _accumulator;
    private byte _indexX;
    private byte _indexY;
    private byte _stackPointer = 0xFF;
    private ushort _programCounter = 0x8000;
    private byte _processorStatus = 0x20; // Interrupt disable flag set by default
    
    private readonly IMemory _memory;
    private readonly Dictionary<ushort, Func<bool>> _jitFunctions = new();
    private readonly Dictionary<ushort, string> _functionNames = new();

    public Cpu6502(IMemory memory)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    public CpuState GetState()
    {
        return new CpuState
        {
            AccumulatorA = _accumulator,
            IndexX = _indexX,
            IndexY = _indexY,
            StackPointer = _stackPointer,
            ProgramCounter = _programCounter,
            ProcessorStatus = _processorStatus
        };
    }

    public void SetState(CpuState state)
    {
        _accumulator = state.AccumulatorA;
        _indexX = state.IndexX;
        _indexY = state.IndexY;
        _stackPointer = state.StackPointer;
        _programCounter = state.ProgramCounter;
        _processorStatus = state.ProcessorStatus;
    }

    public ushort GetProgramCounter() => _programCounter;
    public void SetProgramCounter(ushort address) => _programCounter = address;

    public byte GetStackPointer() => _stackPointer;
    public void SetStackPointer(byte value) => _stackPointer = value;

    public byte GetProcessorStatus() => _processorStatus;
    public void SetProcessorStatus(byte value) => _processorStatus = value;

    public bool GetFlag(CpuStatusFlags flag)
    {
        return (_processorStatus & (1 << (int)flag)) != 0;
    }

    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        if (value)
            _processorStatus |= (byte)(1 << (int)flag);
        else
            _processorStatus &= (byte)~(1 << (int)flag);
    }

    public bool ExecuteCycle()
    {
        // Try to execute JIT function first
        if (_jitFunctions.TryGetValue(_programCounter, out var jitFunction))
        {
            return jitFunction();
        }

        // Fallback to interpretation
        return ExecuteInterpretedInstruction();
    }

    private bool ExecuteInterpretedInstruction()
    {
        // Basic interpretation fallback
        byte opcode = _memory.Read(_programCounter);
        _programCounter++;
        
        // For now, just a basic NOP to prevent infinite loops
        return true;
    }

    public void RegisterJITFunction(ushort address, Func<bool> function, string name)
    {
        _jitFunctions[address] = function;
        _functionNames[address] = name;
    }

    public void Reset()
    {
        _accumulator = 0;
        _indexX = 0;
        _indexY = 0;
        _stackPointer = 0xFF;
        _processorStatus = 0x20; // Interrupt disable
        
        // Read reset vector
        _programCounter = _memory.Read16(0xFFFC);
    }

    public void HandleNMI()
    {
        // Push PC and status to stack
        PushToStack((byte)((_programCounter >> 8) & 0xFF));
        PushToStack((byte)(_programCounter & 0xFF));
        PushToStack(_processorStatus);
        
        // Jump to NMI vector
        _programCounter = _memory.Read16(0xFFFA);
        SetFlag(CpuStatusFlags.InterruptDisable, true);
    }

    public void HandleIRQ()
    {
        if (GetFlag(CpuStatusFlags.InterruptDisable))
            return;
            
        // Push PC and status to stack
        PushToStack((byte)((_programCounter >> 8) & 0xFF));
        PushToStack((byte)(_programCounter & 0xFF));
        PushToStack(_processorStatus);
        
        // Jump to IRQ vector
        _programCounter = _memory.Read16(0xFFFE);
        SetFlag(CpuStatusFlags.InterruptDisable, true);
    }

    private void PushToStack(byte value)
    {
        _memory.Write((ushort)(0x100 + _stackPointer), value);
        _stackPointer--;
    }

    private byte PopFromStack()
    {
        _stackPointer++;
        return _memory.Read((ushort)(0x100 + _stackPointer));
    }

    // Methods expected by JIT instruction handlers
    public byte ReadMemory(ushort address) => _memory.Read(address);
    public void WriteMemory(ushort address, byte value) => _memory.Write(address, value);
    public ushort ReadMemory16(ushort address) => _memory.Read16(address);
    public void WriteMemory16(ushort address, ushort value) => _memory.Write16(address, value);

    // Additional methods for compatibility
    public byte GetAccumulator() => _accumulator;
    public void SetAccumulator(byte value) => _accumulator = value;
    public byte GetIndexX() => _indexX;
    public void SetIndexX(byte value) => _indexX = value;
    public byte GetIndexY() => _indexY;
    public void SetIndexY(byte value) => _indexY = value;

    // VBlank and timing methods (placeholders for JIT compatibility)
    public void WaitForVBlank() { /* Implementation would depend on PPU integration */ }
    public void ExecuteCPUCycle() => ExecuteCycle();
}