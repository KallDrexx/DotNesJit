using System.Runtime.Loader;
using DotNesJit.JIT.Builder;
using DotNesJit.Emulation;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;

namespace DotNesJit.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("DotNesJit - A .NET NES emulator with JIT compilation v1.0");
        
        if (args.Length == 0)
        {
            ShowHelp();
            return 1;
        }

        var command = args[0].ToLower();
        var romFile = args.Length > 1 ? args[1] : null;
        
        if (string.IsNullOrEmpty(romFile) || !File.Exists(romFile))
        {
            Console.WriteLine("Error: ROM file not found or not specified");
            ShowHelp();
            return 1;
        }

        try
        {
            switch (command)
            {
                case "compile":
                    return await CompileRom(romFile, args);
                case "run":
                    return await RunRom(romFile, args);
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run compile <rom-file> [--save-dll] [--verbose]");
        Console.WriteLine("  dotnet run run <rom-file> [--mode <mode>] [--verbose]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  compile    Compile a NES ROM to JIT assembly");
        Console.WriteLine("  run        Compile and run a NES ROM");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --save-dll     Save compiled assembly to disk");
        Console.WriteLine("  --mode         Emulation mode (event-driven, cycle-accurate)");
        Console.WriteLine("  --verbose      Enable verbose output");
    }

    private static async Task<int> CompileRom(string romFile, string[] args)
    {
        var saveDll = args.Contains("--save-dll");
        var verbose = args.Contains("--verbose");

        Console.WriteLine($"Loading ROM: '{romFile}'");
        
        // Load ROM
        var loader = new ROMLoader();
        var romInfo = loader.LoadFromFile(romFile);
        var programRomData = loader.GetPRGROMData();
        var chrRomData = loader.GetCHRROMData();

        Console.WriteLine($"ROM Information:");
        Console.WriteLine($"  PRG ROM: {programRomData.Length} bytes ({programRomData.Length / 1024}KB)");
        Console.WriteLine($"  CHR ROM: {chrRomData.Length} bytes ({chrRomData.Length / 1024}KB)");
        Console.WriteLine($"  Mapper: {romInfo.MapperNumber}");

        // Disassemble
        Console.WriteLine("Phase 1: Disassembling ROM...");
        var disassembler = new Disassembler(romInfo, programRomData);
        disassembler.Disassemble();
        Console.WriteLine($"  ✓ Found {disassembler.Instructions.Count} instructions");

        // Decompile
        Console.WriteLine("Phase 2: Analyzing control flow...");
        var decompiler = new Decompiler(romInfo, disassembler);
        decompiler.Decompile();
        Console.WriteLine($"  ✓ Identified {decompiler.Functions.Count} functions");

        // JIT Compile
        Console.WriteLine("Phase 3: JIT Compilation...");
        var builder = new NesAssemblyBuilder(Path.GetFileNameWithoutExtension(romFile), decompiler);
        
        if (saveDll)
        {
            var outputPath = Path.GetDirectoryName(romFile) ?? Environment.CurrentDirectory;
            var dllFileName = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(romFile) + ".dll");
            
            using var dllFile = File.Create(dllFileName);
            builder.Save(dllFile);
            Console.WriteLine($"  ✓ Generated: {dllFileName}");
        }

        Console.WriteLine("Compilation complete!");
        return 0;
    }

    private static async Task<int> RunRom(string romFile, string[] args)
    {
        var verbose = args.Contains("--verbose");
        var mode = "event-driven";
        
        var modeIndex = Array.IndexOf(args, "--mode");
        if (modeIndex >= 0 && modeIndex + 1 < args.Length)
        {
            mode = args[modeIndex + 1];
        }

        Console.WriteLine($"Loading ROM: '{romFile}'");

        // Compile ROM (same as compile command)
        var loader = new ROMLoader();
        var romInfo = loader.LoadFromFile(romFile);
        var programRomData = loader.GetPRGROMData();
        var chrRomData = loader.GetCHRROMData();

        Console.WriteLine("Analyzing ROM...");
        var disassembler = new Disassembler(romInfo, programRomData);
        disassembler.Disassemble();
        
        var decompiler = new Decompiler(romInfo, disassembler);
        decompiler.Decompile();

        Console.WriteLine("Compiling JIT assembly...");
        var builder = new NesAssemblyBuilder(Path.GetFileNameWithoutExtension(romFile), decompiler);

        using var stream = new MemoryStream();
        builder.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);
        var assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
        
        // Create emulator
        var emulator = new NESEmulator(programRomData, chrRomData);
        emulator.LoadJITAssembly(assembly);

        Console.WriteLine($"✓ Setup complete! Mode: {mode}");

        emulator.Start();
        emulator.ExecuteFrame();

        return 0;
    }
}