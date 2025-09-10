using DotNesJit.Core.Enums;
using DotNesJit.Core.Models;

namespace DotNesJit.Core.Interfaces;

public interface ICPU
{
    CpuState GetState();
    void SetState(CpuState state);
    
    ushort GetProgramCounter();
    void SetProgramCounter(ushort address);
    
    byte GetStackPointer();
    void SetStackPointer(byte value);
    
    byte GetProcessorStatus();
    void SetProcessorStatus(byte value);
    
    bool GetFlag(CpuStatusFlags flag);
    void SetFlag(CpuStatusFlags flag, bool value);
    
    bool ExecuteCycle();
    void Reset();
    
    void HandleNMI();
    void HandleIRQ();
}