using System.Reflection.Emit;

namespace DotNesJit.Core.Interfaces;

public interface IInstructionHandler
{
    IEnumerable<string> Mnemonics { get; }
    void EmitInstruction(ILGenerator il, string mnemonic, byte[] operands);
}