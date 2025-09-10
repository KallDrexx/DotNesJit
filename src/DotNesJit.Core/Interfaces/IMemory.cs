namespace DotNesJit.Core.Interfaces;

public interface IMemory
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
    
    ushort Read16(ushort address);
    void Write16(ushort address, ushort value);
    
    ReadOnlySpan<byte> ReadRange(ushort address, int length);
    void WriteRange(ushort address, ReadOnlySpan<byte> data);
    
    void Reset();
}