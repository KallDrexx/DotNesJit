using System.Reflection.Emit;
using Dotnet6502.Common.Decompilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Compiles 6502 assembly functions based on a specified method entry point
/// on an as-needed basis.
/// </summary>
public class JitCompiler : IJitCompiler
{
    public static readonly OpCode LoadJitCompilerArg = OpCodes.Ldarg_0;
    public static readonly OpCode LoadHalArg = OpCodes.Ldarg_1;

    private readonly Base6502Hal _hal;
    private readonly IJitCustomizer? _jitCustomizer;
    private readonly IMemoryMap _memoryMap;
    private readonly Dictionary<ushort, ExecutableMethod> _compiledMethods = new();

    public JitCompiler(Base6502Hal hal, IJitCustomizer? jitCustomizer, IMemoryMap memoryMap)
    {
        _hal = hal;
        _jitCustomizer = jitCustomizer;
        _memoryMap = memoryMap;
    }

    /// <summary>
    /// Executes the method starting at the specified address
    /// </summary>
    public void RunMethod(ushort address)
    {
        if (!_compiledMethods.TryGetValue(address, out var method))
        {
            var instructions = GetIrInstructions(address);
            if (instructions.Count == 0)
            {
                var message = $"Function at address 0x{address:X4} has no instructions";
                throw new InvalidOperationException(message);
            }

            var customGenerators = _jitCustomizer?.GetCustomIlGenerators();
            method = ExecutableMethodGenerator.Generate($"func_{address:X4}", instructions, customGenerators);
            _compiledMethods.Add(address, method);
        }

        _hal.DebugHook($"Entering function 0x{address:X4}");
        method(this, _hal);
        _hal.DebugHook($"Exiting function 0x{address:X4}");
    }

    private IReadOnlyList<ConvertedInstruction> GetIrInstructions(ushort address)
    {
        var codeRegions = _memoryMap.GetCodeRegions();
        var function = FunctionDecompiler.Decompile(address, codeRegions);

        if (function.Instructions.Count == 0)
        {
            var message = $"Function at address 0x{address:X4} contained no instructions";
            throw new InvalidOperationException(message);
        }

        var orderedInstructions = function.GetOrderedInstructions();

        // Convert each 6502 instruction into one or more IR instructions
        IReadOnlyList<ConvertedInstruction> convertedInstructions = orderedInstructions
            .Select(x => new ConvertedInstruction(x, InstructionConverter.Convert(x, function.InternalJumpTargets)))
            .ToArray();

        // Mutate the instructions based on the JIT customizations being requested
        if (_jitCustomizer != null)
        {
            convertedInstructions = _jitCustomizer.MutateInstructions(convertedInstructions);
        }

        return convertedInstructions;
    }
}