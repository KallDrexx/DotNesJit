namespace DotNesJit.Core.Models;

public readonly record struct PpuState
{
    public byte Control { get; init; }
    public byte Mask { get; init; }
    public byte Status { get; init; }
    public byte OamAddress { get; init; }
    public ushort VramAddress { get; init; }
    public byte ScrollX { get; init; }
    public byte ScrollY { get; init; }
    public int Scanline { get; init; }
    public int Cycle { get; init; }
    public bool InVBlank { get; init; }
}