using System;
using System.IO;
using System.Runtime.CompilerServices;
using CppAst;

namespace R3D_cs.GenerateBindings;

/// <summary>
/// Entry point for the R3D C# bindings generator.
/// </summary>
internal static class Program
{
    private const string DefaultRepoUrl = "https://github.com/Bigfoot71/r3d/";
    private const string DefaultGitRef = "master";

    private static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║     R3D C# Bindings Generator            ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.WriteLine();

        // Parse arguments
        var options = ParseArguments(args);
        if (options == null)
            return;

        try
        {
            Run(options);
            Console.WriteLine();
            Console.WriteLine("Generation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"  Inner: {ex.InnerException.Message}");
            Environment.Exit(1);
        }
    }

    private static void Run(Options options)
    {
        string repoPath;

        // Step 1: Ensure repository is available
        if (options.LocalRepo != null)
        {
            // Use local repository
            repoPath = Path.GetFullPath(options.LocalRepo);
            Console.WriteLine($"Using local repository: {repoPath}");

            if (!Directory.Exists(repoPath))
                throw new DirectoryNotFoundException($"Local repository not found: {repoPath}");
        }
        else
        {
            // Clone or use cached repository
            Console.WriteLine($"Repository: {options.RepoUrl}");
            Console.WriteLine($"Git ref: {options.GitRef}");
            Console.WriteLine();

            // Delete existing repo if force clone requested
            if (options.ForceClone)
            {
                RepositoryManager.DeleteRepository();
                Console.WriteLine();
            }

            repoPath = RepositoryManager.EnsureRepository(options.RepoUrl, options.GitRef);
        }

        (string commitSha, string branch) = RepositoryManager.GetRepositoryInfo(repoPath);
        Console.WriteLine($"  Commit: {commitSha} ({branch})");
        Console.WriteLine();

        // Step 2: Parse C header
        Console.WriteLine("Parsing C header...");
        string headerPath = Path.Combine(repoPath, "include", "r3d", "r3d.h");
        string raylibInclude = Path.Combine(repoPath, "external", "raylib", "src");

        if (!File.Exists(headerPath))
        {
            throw new FileNotFoundException($"Header file not found: {headerPath}");
        }

        var parserOptions = new CppParserOptions
        {
            IncludeFolders = { raylibInclude },
            ParseMacros = false,
            ParserKind = CppParserKind.C,
            ParseCommentAttribute = true,
            ParseComments = true,
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

        // Step 3: Generate bindings
        Console.WriteLine("Generating C# bindings...");
        string outputDir = Path.GetFullPath(options.OutputDir);
        Console.WriteLine($"  Output: {outputDir}");
        Console.WriteLine();

        var generator = new CodeGenerator(outputDir);
        generator.Generate(compilation);
    }

    private static Options? ParseArguments(string[] args)
    {
        var options = new Options
        {
            RepoUrl = DefaultRepoUrl,
            GitRef = DefaultGitRef,
            OutputDir = Path.GetFullPath("../R3D-cs", GetProjectDirectory())
        };

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    PrintHelp();
                    return null;

                case "-b":
                case "--branch":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --branch requires a value");
                        return null;
                    }
                    options.GitRef = args[++i];
                    break;

                case "-t":
                case "--tag":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --tag requires a value");
                        return null;
                    }
                    options.GitRef = args[++i];
                    break;

                case "-r":
                case "--repo":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --repo requires a value");
                        return null;
                    }
                    options.RepoUrl = args[++i];
                    break;

                case "-o":
                case "--output":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --output requires a value");
                        return null;
                    }
                    options.OutputDir = args[++i];
                    break;

                case "--clean":
                    // Delete the cached repository and exit
                    RepositoryManager.DeleteRepository();
                    return null;

                case "--force-clone":
                    options.ForceClone = true;
                    break;

                case "-l":
                case "--local":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --local requires a path");
                        return null;
                    }
                    options.LocalRepo = args[++i];
                    break;

                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    Console.WriteLine("Use --help for usage information.");
                    return null;
            }
        }

        return options;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: R3D-cs.GenerateBindings [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help           Show this help message");
        Console.WriteLine("  -l, --local <path>   Use existing local repository (skips cloning)");
        Console.WriteLine("  -b, --branch <name>  Git branch to use when cloning (default: master)");
        Console.WriteLine("  -t, --tag <name>     Git tag to use when cloning");
        Console.WriteLine("  -r, --repo <url>     Repository URL (default: https://github.com/Bigfoot71/r3d/)");
        Console.WriteLine("  -o, --output <dir>   Output directory for generated files");
        Console.WriteLine("  --force-clone        Delete cached repo and clone fresh");
        Console.WriteLine("  --clean              Remove cached repository and exit");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  R3D-cs.GenerateBindings                    # Use cached repo or clone master");
        Console.WriteLine("  R3D-cs.GenerateBindings -l ../r3d          # Use local r3d repository");
        Console.WriteLine("  R3D-cs.GenerateBindings --force-clone      # Force fresh clone");
        Console.WriteLine("  R3D-cs.GenerateBindings -b develop --force-clone  # Clone develop branch");
        Console.WriteLine("  R3D-cs.GenerateBindings --clean            # Clear cached repository");
        Console.WriteLine();
        Console.WriteLine($"Repository is cached at: {RepositoryManager.GetRepositoryPath()}");
    }

    private static string GetProjectDirectory([CallerFilePath] string? path = null) => Path.GetDirectoryName(path)!;

    private class Options
    {
        public required string RepoUrl { get; set; }
        public required string GitRef { get; set; }
        public required string OutputDir { get; set; }
        public string? LocalRepo { get; set; }
        public bool ForceClone { get; set; }
    }
}
