using DotNesJit.Core.Models;

namespace DotNesJit.Core.Interfaces;

public interface IPPU
{
    PpuState GetState();
    void SetState(PpuState state);
    
    byte ReadRegister(ushort address);
    void WriteRegister(ushort address, byte value);
    
    void ExecuteCycle();
    void Reset();
    
    bool IsInVBlank();
    bool IsFrameComplete();
    
    event Action? VBlankStarted;
    event Action? VBlankEnded;
    event Action? FrameComplete;
}