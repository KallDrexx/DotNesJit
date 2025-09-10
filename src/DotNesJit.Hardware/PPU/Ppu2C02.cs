using DotNesJit.Core.Interfaces;
using DotNesJit.Core.Models;

namespace DotNesJit.Hardware.PPU;

public class Ppu2C02 : IPPU
{
    private byte _control;
    private byte _mask;
    private byte _status;
    private byte _oamAddress;
    private ushort _vramAddress;
    private byte _scrollX;
    private byte _scrollY;
    private int _scanline;
    private int _cycle;
    private bool _inVBlank;
    private bool _frameComplete;

    public event Action? VBlankStarted;
    public event Action? VBlankEnded;
    public event Action? FrameComplete;

    public PpuState GetState()
    {
        return new PpuState
        {
            Control = _control,
            Mask = _mask,
            Status = _status,
            OamAddress = _oamAddress,
            VramAddress = _vramAddress,
            ScrollX = _scrollX,
            ScrollY = _scrollY,
            Scanline = _scanline,
            Cycle = _cycle,
            InVBlank = _inVBlank
        };
    }

    public void SetState(PpuState state)
    {
        _control = state.Control;
        _mask = state.Mask;
        _status = state.Status;
        _oamAddress = state.OamAddress;
        _vramAddress = state.VramAddress;
        _scrollX = state.ScrollX;
        _scrollY = state.ScrollY;
        _scanline = state.Scanline;
        _cycle = state.Cycle;
        _inVBlank = state.InVBlank;
    }

    public byte ReadRegister(ushort address)
    {
        return (address & 0x7) switch
        {
            0x2 => ReadStatus(),
            0x4 => _oamAddress,
            0x7 => ReadVRAM(),
            _ => 0
        };
    }

    public void WriteRegister(ushort address, byte value)
    {
        switch (address & 0x7)
        {
            case 0x0:
                _control = value;
                break;
            case 0x1:
                _mask = value;
                break;
            case 0x3:
                _oamAddress = value;
                break;
            case 0x5:
                WriteScroll(value);
                break;
            case 0x6:
                WriteVRAMAddress(value);
                break;
            case 0x7:
                WriteVRAM(value);
                break;
        }
    }

    public void ExecuteCycle()
    {
        _cycle++;
        
        if (_cycle >= 341) // PPU cycles per scanline
        {
            _cycle = 0;
            _scanline++;
            
            if (_scanline == 241) // Start of VBlank
            {
                _inVBlank = true;
                _status |= 0x80; // Set VBlank flag
                VBlankStarted?.Invoke();
            }
            else if (_scanline >= 262) // End of frame
            {
                _scanline = 0;
                _inVBlank = false;
                _status &= 0x7F; // Clear VBlank flag
                _frameComplete = true;
                
                VBlankEnded?.Invoke();
                FrameComplete?.Invoke();
            }
        }
    }

    public void Reset()
    {
        _control = 0;
        _mask = 0;
        _status = 0;
        _oamAddress = 0;
        _vramAddress = 0;
        _scrollX = 0;
        _scrollY = 0;
        _scanline = 0;
        _cycle = 0;
        _inVBlank = false;
        _frameComplete = false;
    }

    public bool IsInVBlank() => _inVBlank;

    public bool IsFrameComplete()
    {
        bool complete = _frameComplete;
        _frameComplete = false; // Clear flag after reading
        return complete;
    }

    private byte ReadStatus()
    {
        byte status = _status;
        _status &= 0x7F; // Clear VBlank flag on read
        return status;
    }

    private byte ReadVRAM()
    {
        // VRAM reading logic would go here
        return 0;
    }

    private void WriteScroll(byte value)
    {
        // Scroll register write logic
        _scrollX = value; // Simplified
    }

    private void WriteVRAMAddress(byte value)
    {
        // VRAM address register write logic
        _vramAddress = value; // Simplified
    }

    private void WriteVRAM(byte value)
    {
        // VRAM writing logic would go here
    }
}