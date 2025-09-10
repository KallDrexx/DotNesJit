using DotNesJit.Core.Enums;

namespace DotNesJit.Core.Models;

public readonly record struct CpuState
{
    public byte AccumulatorA { get; init; }
    public byte IndexX { get; init; }
    public byte IndexY { get; init; }
    public byte StackPointer { get; init; }
    public ushort ProgramCounter { get; init; }
    public byte ProcessorStatus { get; init; }
    
    public bool GetFlag(CpuStatusFlags flag) => (ProcessorStatus & (1 << (int)flag)) != 0;
}