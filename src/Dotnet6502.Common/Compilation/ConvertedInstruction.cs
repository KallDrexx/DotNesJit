using Dotnet6502.Common.Decompilation;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Represent a single 6502 assembly instruction that has been converted to the
/// Ir6502 intermediary representation instructions
/// </summary>
public record ConvertedInstruction(
    RawInstruction OriginalInstruction,
    IReadOnlyList<Ir6502.Instruction> Ir6502Instructions);