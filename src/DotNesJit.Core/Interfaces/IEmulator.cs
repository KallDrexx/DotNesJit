using DotNesJit.Core.Enums;
using DotNesJit.Core.Models;

namespace DotNesJit.Core.Interfaces;

public interface IEmulator
{
    EmulationStats GetStats();
    
    void Start();
    void Stop();
    void Pause();
    void Resume();
    void Reset();
    
    bool IsRunning { get; }
    bool IsPaused { get; }
    
    void SetControllerState(int controller, NESController state);
    NESController GetControllerState(int controller);
    
    event Action<EmulationStats>? StatsUpdated;
    event Action? Stopped;
}