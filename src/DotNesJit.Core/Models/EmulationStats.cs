namespace DotNesJit.Core.Models;

public readonly record struct EmulationStats
{
    public long TotalCycles { get; init; }
    public double CyclesPerSecond { get; init; }
    public double CurrentFPS { get; init; }
    public int CurrentScanline { get; init; }
    public int VBlankDetections { get; init; }
    public bool IsRunning { get; init; }
    public TimeSpan Runtime { get; init; }
}