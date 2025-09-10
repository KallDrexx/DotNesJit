using DotNesJit.Core.Interfaces;

namespace DotNesJit.Emulation.Modes;

public class EventDrivenMode
{
    private readonly IEmulator _emulator;
    private readonly Timer _frameTimer;
    private readonly object _lockObject = new();

    public EventDrivenMode(IEmulator emulator)
    {
        _emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
        _frameTimer = new Timer(ExecuteFrame, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        // Target 60 FPS (16.67ms per frame)
        _frameTimer.Change(0, 16);
    }

    public void Stop()
    {
        _frameTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void ExecuteFrame(object? state)
    {
        lock (_lockObject)
        {
            if (_emulator.IsRunning && !_emulator.IsPaused)
            {
                if (_emulator is NESEmulator nesEmulator)
                {
                    nesEmulator.ExecuteFrame();
                }
            }
        }
    }

    public void Dispose()
    {
        _frameTimer?.Dispose();
    }
}