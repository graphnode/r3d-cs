using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using CppAst;

namespace R3D_cs.GenerateBindings;

internal static class Program
{
    private static int Main(string[] args)
    {
        Option<string> pathOption = new("--path", "-p")
        {
            Description = "Path to local r3d repository with the header files to parse",
            Required = true,
        };

        Option<string> versionOption = new("--version", "-v")
        {
            Description = "Version for generated bindings (e.g., 0.8.0, 0.8.0-dev)",
            Required = true,
        };

        Option<string> outputOption = new("--output", "-o")
        {
            Description = "Output directory for generated files (default: <solution>/R3D-cs)",
            DefaultValueFactory = _ =>
            {
                // Find solution root by looking for .sln file
                string currentDir = Directory.GetCurrentDirectory();
                string? solutionRoot = currentDir;

                // Walk up directory tree to find .sln file
                while (solutionRoot != null && !Directory.GetFiles(solutionRoot, "*.sln").Any())
                {
                    solutionRoot = Directory.GetParent(solutionRoot)?.FullName;
                }

                if (solutionRoot == null)
                    throw new DirectoryNotFoundException("Could not find solution root (no .sln file found in parent directories)");

                return Path.Combine(solutionRoot, "R3D-cs");
            },
        };

        RootCommand rootCommand = new("Sample app for System.CommandLine")
        {
            Description = "Generates C# bindings for the R3D C API",
            Options = { pathOption, versionOption, outputOption },
        };
        rootCommand.SetAction(parseResult =>
        {
            try
            {
                string repoPath = Path.GetFullPath(parseResult.GetValue(pathOption) ?? string.Empty);
                string version = parseResult.GetValue(versionOption) ?? string.Empty;
                string outputDir = parseResult.GetValue(outputOption) ?? string.Empty;

                Console.WriteLine($"Repository path: {repoPath}");
                Console.WriteLine($"Version: {version}");
                Console.WriteLine();

                if (!Directory.Exists(repoPath))
                    throw new DirectoryNotFoundException($"Repository path not found: {repoPath}");

                // Parse C header
                Console.WriteLine("Parsing C header...");
                string headerPath = Path.Combine(repoPath, "include", "r3d", "r3d.h");
                string includePath = Path.Combine(repoPath, "include");
                string raylibInclude = Path.Combine(repoPath, "external", "raylib", "src");

                if (!File.Exists(headerPath))
                    throw new FileNotFoundException($"Header file not found: {headerPath}");

                var parserOptions = new CppParserOptions
                {
                    IncludeFolders = { includePath, raylibInclude },
                    ParseMacros = false,
                    ParserKind = CppParserKind.C,
                    ParseCommentAttribute = true,
                    ParseComments = true
                };

                var compilation = CppParser.ParseFile(headerPath, parserOptions);

                if (compilation.HasErrors)
                {
                    Console.WriteLine("Parser errors:");
                    foreach (var message in compilation.Diagnostics.Messages)
                        Console.WriteLine($"  {message.Text}");
                    throw new Exception("Failed to parse header file");
                }

                Console.WriteLine($"  Parsed successfully: {compilation.Functions.Count} functions, {compilation.Classes.Count} structs, {compilation.Enums.Count} enums");
                Console.WriteLine();

                // Generate bindings
                Console.WriteLine("Generating C# bindings...");
                Console.WriteLine($"  Output: {outputDir}");
                Console.WriteLine();

                var generator = new CodeGenerator(outputDir);
                generator.Generate(compilation, version);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }

            return 0;
        });

        var parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }
}
