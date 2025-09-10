using DotNesJit.Core.Interfaces;
using DotNesJit.Core.Models;
using DotNesJit.Core.Enums;
using DotNesJit.Hardware;
using System.Reflection;

namespace DotNesJit.Emulation;

public class NESEmulator : IEmulator
{
    private readonly NESHardware _hardware;
    private readonly Dictionary<ushort, MethodInfo> _jitMethods = new();
    
    private bool _isRunning;
    private bool _isPaused;
    private long _totalCycles;
    private DateTime _startTime;
    private Assembly? _jitAssembly;

    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;

    public event Action<EmulationStats>? StatsUpdated;
    public event Action? Stopped;

    public NESEmulator(byte[] prgRom, byte[] chrRom)
    {
        _hardware = new NESHardware(prgRom, chrRom);
        
        // Set up PPU events
        _hardware.PPU.VBlankStarted += OnVBlankStarted;
        _hardware.PPU.FrameComplete += OnFrameComplete;
    }

    public void LoadJITAssembly(Assembly? assembly)
    {
        _jitAssembly = assembly;
        SetupJITFunctions();
    }

    public EmulationStats GetStats()
    {
        var runtime = _isRunning ? DateTime.UtcNow - _startTime : TimeSpan.Zero;
        var cyclesPerSecond = runtime.TotalSeconds > 0 ? _totalCycles / runtime.TotalSeconds : 0;
        
        return new EmulationStats
        {
            TotalCycles = _totalCycles,
            CyclesPerSecond = cyclesPerSecond,
            CurrentFPS = 60, // Placeholder
            CurrentScanline = _hardware.PPU.GetState().Scanline,
            VBlankDetections = 0, // Placeholder
            IsRunning = _isRunning,
            Runtime = runtime
        };
    }

    public void Start()
    {
        if (_isRunning) return;
        
        _isRunning = true;
        _isPaused = false;
        _startTime = DateTime.UtcNow;
        _totalCycles = 0;
    }

    public void Stop()
    {
        _isRunning = false;
        _isPaused = false;
        Stopped?.Invoke();
    }

    public void Pause()
    {
        _isPaused = true;
    }

    public void Resume()
    {
        _isPaused = false;
    }

    public void Reset()
    {
        _hardware.Reset();
        _totalCycles = 0;
    }

    public void SetControllerState(int controller, NESController state)
    {
        _hardware.SetControllerState(controller, state);
    }

    public NESController GetControllerState(int controller)
    {
        return _hardware.GetControllerState(controller);
    }

    public void ExecuteFrame()
    {
        if (!_isRunning || _isPaused) return;

        // Execute one frame worth of cycles
        // NES runs at ~1.79MHz CPU, ~5.37MHz PPU
        const int cyclesPerFrame = 29781; // Approximate NTSC frame cycles
        
        for (int i = 0; i < cyclesPerFrame && _isRunning && !_isPaused; i++)
        {
            // Execute CPU cycle
            if (_hardware.ExecuteCPUCycle())
            {
                _totalCycles++;
            }
            
            // Execute 3 PPU cycles for every CPU cycle
            for (int j = 0; j < 3; j++)
            {
                _hardware.PPU.ExecuteCycle();
            }
        }
        
        // Update stats periodically
        if (_totalCycles % 10000 == 0)
        {
            StatsUpdated?.Invoke(GetStats());
        }
    }

    private void SetupJITFunctions()
    {
        if (_jitAssembly == null) return;

        try
        {
            var gameType = _jitAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == "Game" || t.Name.EndsWith(".Game"));

            if (gameType != null)
            {
                var hardwareField = gameType.GetField("Hardware", BindingFlags.Public | BindingFlags.Static);
                hardwareField?.SetValue(null, _hardware);

                var methods = gameType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                
                foreach (var method in methods)
                {
                    if (ushort.TryParse(method.Name.Replace("Function_", ""), 
                        System.Globalization.NumberStyles.HexNumber, null, out ushort address))
                    {
                        _jitMethods[address] = method;
                        
                        _hardware.RegisterJITFunction(address, () =>
                        {
                            try
                            {
                                method.Invoke(null, null);
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        }, method.Name);
                    }
                }
            }
        }
        catch
        {
            // JIT setup failed, continue with interpretation
        }
    }

    private void OnVBlankStarted()
    {
        // Handle VBlank interrupt
        _hardware.HandleNMI();
    }

    private void OnFrameComplete()
    {
        // Frame complete processing
    }
}