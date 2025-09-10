using DotNesJit.Core.Enums;

namespace DotNesJit.Hardware.Input;

public class Controller
{
    private NESController _currentState;
    private NESController _latchedState;
    private int _shiftRegister;
    private bool _strobe;

    public NESController State
    {
        get => _currentState;
        set => _currentState = value;
    }

    public void SetStrobe(bool strobe)
    {
        _strobe = strobe;
        if (strobe)
        {
            _latchedState = _currentState;
            _shiftRegister = 0;
        }
    }

    public byte Read()
    {
        if (_strobe)
        {
            // When strobe is high, always return A button state
            return (byte)((_currentState & NESController.A) != 0 ? 1 : 0);
        }

        // Return current bit and shift
        byte result = (byte)((_latchedState & (NESController)(1 << _shiftRegister)) != 0 ? 1 : 0);
        _shiftRegister++;
        
        // After 8 reads, keep returning 1
        if (_shiftRegister >= 8)
            return 1;
            
        return result;
    }

    public void Reset()
    {
        _currentState = NESController.None;
        _latchedState = NESController.None;
        _shiftRegister = 0;
        _strobe = false;
    }
}