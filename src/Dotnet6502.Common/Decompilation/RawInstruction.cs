using NESDecompiler.Core.CPU;

namespace Dotnet6502.Common.Decompilation;

public record RawInstruction(
    ushort Address,
    string Mnemonic,
    AddressingMode AddressingMode,
    byte? Operand1,
    byte? Operand2,
    ushort? TargetAddress,
    bool IsBranch,
    byte CycleCount);