using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 PHA (Push Accumulator) instruction
///
/// PHA pushes the accumulator onto the stack and:
/// - Does NOT affect any flags
/// - Decrements the stack pointer
/// - Preserves all registers
/// </summary>
public class PhaTests
{
    [Fact]
    public void PHA_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };


        var nesIrInstructions = InstructionConverter.Convert(instruction.ToRawInstruction(), new HashSet<ushort>());
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42); // Accumulator preserved
        jit.TestHal.PopFromStack().ShouldBe((byte)0x42); // Value on stack
    }

    [Fact]
    public void PHA_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };


        var nesIrInstructions = InstructionConverter.Convert(instruction.ToRawInstruction(), new HashSet<ushort>());
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.PopFromStack().ShouldBe((byte)0x00);
    }

    [Fact]
    public void PHA_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };


        var nesIrInstructions = InstructionConverter.Convert(instruction.ToRawInstruction(), new HashSet<ushort>());
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.PopFromStack().ShouldBe((byte)0xFF);
    }

    [Fact]
    public void PHA_Does_Not_Affect_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };


        var nesIrInstructions = InstructionConverter.Convert(instruction.ToRawInstruction(), new HashSet<ushort>());
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x80;

        // Set all flags to test they are preserved
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        // All flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void PHA_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };


        var nesIrInstructions = InstructionConverter.Convert(instruction.ToRawInstruction(), new HashSet<ushort>());
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
        jit.TestHal.PopFromStack().ShouldBe((byte)0x42);
    }

    [Fact]
    public void PHA_Multiple_Values()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };


        var nesIrInstructions = InstructionConverter.Convert(instruction.ToRawInstruction(), new HashSet<ushort>());

        // Test pushing multiple values
        var jit1 = new TestJitCompiler();
            jit1.AddMethod(0x1234, nesIrInstructions);
        jit1.TestHal.ARegister = 0x55;
        jit1.RunMethod(0x1234);
        jit1.TestHal.PopFromStack().ShouldBe((byte)0x55);

        var jit2 = new TestJitCompiler();
            jit2.AddMethod(0x1234, nesIrInstructions);
        jit2.TestHal.ARegister = 0xAA;
        jit2.RunMethod(0x1234);
        jit2.TestHal.PopFromStack().ShouldBe((byte)0xAA);
    }
}