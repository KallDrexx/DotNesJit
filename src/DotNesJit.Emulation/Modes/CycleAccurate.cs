using DotNesJit.Core.Interfaces;

namespace DotNesJit.Emulation.Modes;

public class CycleAccurateMode
{
    private readonly IEmulator _emulator;
    private volatile bool _running;

    public CycleAccurateMode(IEmulator emulator)
    {
        _emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
    }

    public void Start()
    {
        _running = true;
        
        Task.Run(() =>
        {
            // High precision timing for cycle-accurate emulation
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            const double targetCyclesPerSecond = 1789773.0; // NTSC CPU frequency
            const double nanosPerCycle = 1_000_000_000.0 / targetCyclesPerSecond;
            
            long cycleCount = 0;
            
            while (_running && _emulator.IsRunning)
            {
                if (!_emulator.IsPaused)
                {
                    if (_emulator is NESEmulator nesEmulator)
                    {
                        nesEmulator.ExecuteFrame();
                        cycleCount += 29781; // Cycles per frame
                    }
                    
                    // Precise timing control
                    var targetTime = TimeSpan.FromTicks((long)(cycleCount * nanosPerCycle / 100));
                    var actualTime = stopwatch.Elapsed;
                    var sleepTime = targetTime - actualTime;
                    
                    if (sleepTime > TimeSpan.Zero)
                    {
                        Thread.Sleep(sleepTime);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        });
    }

    public void Stop()
    {
        _running = false;
    }
}