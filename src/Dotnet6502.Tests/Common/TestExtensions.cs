using Dotnet6502.Common.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Tests.Common;

public static class TestExtensions
{
    public static RawInstruction ToRawInstruction(this DisassembledInstruction instruction)
    {
        return new RawInstruction(
            instruction.CPUAddress,
            instruction.Info.Mnemonic,
            instruction.Info.AddressingMode,
            instruction.Info.Size > 1 ? instruction.Bytes[0] : null,
            instruction.Info.Size > 2 ? instruction.Bytes[1] : null,
            instruction.TargetAddress,
            instruction.IsBranch,
            instruction.Info.Cycles);
    }
}