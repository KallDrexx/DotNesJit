using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Decompilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;

namespace Dotnet6502.ComprehensiveTestRunner;

public class TestJitCompiler : IJitCompiler
{
    public Dictionary<ushort, ExecutableMethod> Methods { get; } = new();
    public Dictionary<Type, MsilGenerator.CustomIlGenerator>? CustomGenerators { get; set; }

    public TestMemoryMap MemoryMap { get; }
    public Base6502Hal TestHal { get; }

    public TestJitCompiler()
    {
        MemoryMap = new TestMemoryMap();
        TestHal = new Base6502Hal(MemoryMap);
    }

    public void RunMethod(ushort address)
    {
        if (!Methods.TryGetValue(address, out var method))
        {
            var message = $"Method at address {address:X4} called but no method prepared for that";
            throw new InvalidOperationException(message);
        }

        method(this, TestHal);
    }

    public void AddMethod(ushort address, IReadOnlyList<Ir6502.Instruction> instructions)
    {
        var nop = new RawInstruction(address, "NOP", AddressingMode.Implied, null, null, null, false, 2);
        var convertedInstructions = new ConvertedInstruction(nop, instructions);
        var method = ExecutableMethodGenerator.Generate($"test_0x{address:X4}", [convertedInstructions], CustomGenerators);
        Methods.Add(address, method);
    }
}
