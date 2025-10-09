using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;

namespace Dotnet6502.Common.Decompilation;

/// <summary>
/// Decompiles a function starting at the specified address
/// </summary>
public static class FunctionDecompiler
{
    public static DecompiledFunction Decompile(ushort functionAddress, IReadOnlyList<CodeRegion> codeRegions)
    {
        var instructions = new List<RawInstruction>();
        var jumpAddresses = new HashSet<ushort>();
        var seenInstructions = new HashSet<ushort>();
        var addressQueue = new Queue<ushort>([functionAddress]);

        while (addressQueue.TryDequeue(out var nextAddress))
        {
            if (!seenInstructions.Add(nextAddress))
            {
                continue;
            }

            var instruction = GetNextInstruction(nextAddress, codeRegions);
            instructions.Add(instruction);

            if (instruction.TargetAddress != null)
            {
                jumpAddresses.Add(instruction.TargetAddress.Value);
                addressQueue.Enqueue(instruction.TargetAddress.Value);
            }

            if (IsEndOfFunction(instruction))
            {
                continue;
            }

            var size = instruction.Operand2 != null
                ? 3
                : instruction.Operand1 != null
                    ? 2
                    : 1;

            addressQueue.Enqueue((ushort)(nextAddress + size));
        }

        return new DecompiledFunction(functionAddress, instructions, jumpAddresses);
    }

    private static RawInstruction GetNextInstruction(ushort address, IReadOnlyList<CodeRegion> codeRegions)
    {
        var relevantRegion = codeRegions
            .Where(x => x.BaseAddress < address)
            .Where(x => x.BaseAddress + x.Bytes.Length > address)
            .FirstOrDefault();

        if (relevantRegion == null)
        {
            var message = $"No code region contained the address 0x{address:X4}";
            throw new InvalidOperationException(message);
        }

        var offset = address - relevantRegion.BaseAddress;
        var bytes = relevantRegion.Bytes.Span[offset..];
        var info = InstructionSet.GetInstruction(bytes[0]);
        if (!info.IsValid)
        {
            var message = $"Attempted to get instruction at address 0x{address:X4}, but byte 0x{bytes[0]:X4} " +
                          $"is not a valid/known opcode";

            throw new InvalidOperationException(message);
        }

        if (bytes.Length < info.Size)
        {
            var message = $"Opcode {info.Mnemonic} at address 0x{address:X4} requires {info.Size} bytes, but only " +
                          $"{bytes.Length} are available";

            throw new InvalidOperationException(message);
        }

        return new RawInstruction(
            address,
            info.Mnemonic,
            info.AddressingMode,
            info.Size > 1 ? bytes[1] : null,
            info.Size > 2 ? bytes[2] : null,
            GetTargetAddress(info, address, bytes),
            info.Type == InstructionType.Branch,
            info.Cycles);
    }

    private static ushort? GetTargetAddress(
        InstructionInfo instructionInfo,
        ushort instructionAddress,
        ReadOnlySpan<byte> bytes)
    {
        if (instructionInfo.AddressingMode == AddressingMode.Relative)
        {
            // Offset is relative to the instruction after the current one
            var offset = (sbyte)bytes[1];
            return (ushort)(instructionAddress + instructionInfo.Size + offset);
        }

        if (instructionInfo.Type == InstructionType.Jump &&
            instructionInfo.AddressingMode is AddressingMode.Absolute or AddressingMode.Indirect)
        {
            return (ushort)((bytes[1] << 8) | bytes[0]);
        }

        return null;
    }

    private static bool IsEndOfFunction(RawInstruction instruction)
    {
        // RTI and RTS are obviously the end of a function. We consider BRK and JSR
        // to be the end of a function as well because an RTI or RTS will do a function
        // call into the next instruction. This is required because RTI/RTS could be
        // returning based on a modified stack, and therefore we are not guaranteed to
        // be returning to the expected spot.
        if (instruction.Mnemonic is "JSR" or "BRK" or "RTI" or "RTS")
        {
            return true;
        }

        // Since we don't know where we are jumping at compile time, this will be treated
        // as a function call, thus we consider it the end of the function.
        if (instruction.AddressingMode == AddressingMode.Indirect)
        {
            return true;
        }

        return false;
    }
}