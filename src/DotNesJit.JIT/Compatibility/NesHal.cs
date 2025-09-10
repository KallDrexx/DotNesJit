using DotNesJit.Hardware;

namespace DotNesJit.JIT.Compatibility;

/// <summary>
/// Compatibility alias for the old NesHal class
/// This allows existing JIT instruction handlers to work with the new architecture
/// </summary>
public class NesHal : NESHardware
{
    public NesHal(byte[] prgRom, byte[] chrRom) : base(prgRom, chrRom)
    {
    }
}