using System.Reflection;

namespace DotNesJit.Core.Interfaces;

public interface IJITCompiler
{
    Assembly? CompileToAssembly(string assemblyName);
    bool AddFunction(ushort address, string name, IEnumerable<byte> instructions);
    void SetHardwareReference(object hardware);
    
    int CompiledFunctionCount { get; }
    IReadOnlyDictionary<ushort, string> CompiledFunctions { get; }
}