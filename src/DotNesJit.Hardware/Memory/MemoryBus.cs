using DotNesJit.Core.Interfaces;

namespace DotNesJit.Hardware.Memory;

public class MemoryBus : IMemory
{
    private readonly byte[] _ram = new byte[0x10000]; // 64KB address space
    private readonly byte[] _prgRom;
    private readonly byte[] _chrRom;
    
    // Memory map constants
    private const ushort RAM_START = 0x0000;
    private const ushort RAM_END = 0x1FFF;
    private const ushort PPU_START = 0x2000;
    private const ushort PPU_END = 0x3FFF;
    private const ushort APU_START = 0x4000;
    private const ushort APU_END = 0x401F;
    private const ushort CARTRIDGE_START = 0x8000;
    private const ushort CARTRIDGE_END = 0xFFFF;

    public MemoryBus(byte[] prgRom, byte[] chrRom)
    {
        _prgRom = prgRom ?? Array.Empty<byte>();
        _chrRom = chrRom ?? Array.Empty<byte>();
        
        // Initialize RAM
        Array.Clear(_ram);
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            >= RAM_START and <= RAM_END => _ram[address & 0x7FF], // 2KB RAM, mirrored
            >= PPU_START and <= PPU_END => ReadPPURegister(address),
            >= APU_START and <= APU_END => ReadAPURegister(address),
            >= CARTRIDGE_START and <= CARTRIDGE_END => ReadCartridge(address),
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case >= RAM_START and <= RAM_END:
                _ram[address & 0x7FF] = value; // 2KB RAM, mirrored
                break;
            case >= PPU_START and <= PPU_END:
                WritePPURegister(address, value);
                break;
            case >= APU_START and <= APU_END:
                WriteAPURegister(address, value);
                break;
            case >= CARTRIDGE_START and <= CARTRIDGE_END:
                WriteCartridge(address, value);
                break;
        }
    }

    public ushort Read16(ushort address)
    {
        byte low = Read(address);
        byte high = Read((ushort)(address + 1));
        return (ushort)(low | (high << 8));
    }

    public void Write16(ushort address, ushort value)
    {
        Write(address, (byte)(value & 0xFF));
        Write((ushort)(address + 1), (byte)((value >> 8) & 0xFF));
    }

    public ReadOnlySpan<byte> ReadRange(ushort address, int length)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = Read((ushort)(address + i));
        }
        return result;
    }

    public void WriteRange(ushort address, ReadOnlySpan<byte> data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            Write((ushort)(address + i), data[i]);
        }
    }

    public void Reset()
    {
        Array.Clear(_ram);
    }

    private byte ReadPPURegister(ushort address)
    {
        // PPU register reading will be handled by PPU component
        return 0;
    }

    private void WritePPURegister(ushort address, byte value)
    {
        // PPU register writing will be handled by PPU component
    }

    private byte ReadAPURegister(ushort address)
    {
        // APU register reading
        return 0;
    }

    private void WriteAPURegister(ushort address, byte value)
    {
        // APU register writing
    }

    private byte ReadCartridge(ushort address)
    {
        // Map cartridge space to PRG ROM
        if (_prgRom.Length == 0) return 0;
        
        int offset = address - CARTRIDGE_START;
        if (_prgRom.Length == 0x4000) // 16KB ROM
        {
            // Mirror in both halves of address space
            offset %= 0x4000;
        }
        else if (_prgRom.Length == 0x8000) // 32KB ROM
        {
            offset %= 0x8000;
        }
        
        return offset < _prgRom.Length ? _prgRom[offset] : (byte)0;
    }

    private void WriteCartridge(ushort address, byte value)
    {
        // Most cartridges are read-only, but some have battery-backed RAM
        // This would be handled by mapper logic
    }
}