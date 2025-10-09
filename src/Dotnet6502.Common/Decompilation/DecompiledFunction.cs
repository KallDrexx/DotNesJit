using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Common.Decompilation;

public record DecompiledFunction(
    ushort Address,
    IReadOnlyList<RawInstruction> Instructions,
    IReadOnlySet<ushort> InternalJumpTargets)
{
    /// <summary>
    /// Returns the instructions in the correct order, so that the first instruction
    /// is always the instruction at the function's starting address.
    /// </summary>
    public IEnumerable<RawInstruction> GetOrderedInstructions()
    {
        var initialInstructions = Instructions
            .Where(x => x.Address >= Address)
            .OrderBy(x => x.Address);

        var trailingInstructions = Instructions
            .Where(x => x.Address < Address)
            .OrderBy(x => x.Address);

        return initialInstructions.Concat(trailingInstructions);
    }
}
