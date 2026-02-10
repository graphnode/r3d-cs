using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        Option<string?> versionOption = new("--version", "-v")
        {
            Description = "Override auto-detected version (normally detected from upstream git tags)",
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
                string? versionOverride = parseResult.GetValue(versionOption);
                string outputDir = parseResult.GetValue(outputOption) ?? string.Empty;

                Console.WriteLine($"Repository path: {repoPath}");

                if (!Directory.Exists(repoPath))
                    throw new DirectoryNotFoundException($"Repository path not found: {repoPath}");

                // Detect upstream version from git state
                var (detectedVersion, commitSha, tag) = DetectUpstreamVersion(repoPath);
                string version = versionOverride ?? detectedVersion;

                Console.WriteLine($"Upstream commit: {commitSha}");
                if (tag != null)
                    Console.WriteLine($"Upstream tag:    {tag}");
                Console.WriteLine($"Version:         {version}{(versionOverride != null ? " (override)" : "")}");
                Console.WriteLine();

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

                // Write .r3d-upstream to solution root
                string? solutionRoot = FindSolutionRoot(Directory.GetCurrentDirectory());
                if (solutionRoot != null)
                {
                    string upstreamFile = Path.Combine(solutionRoot, ".r3d-upstream");
                    WriteUpstreamFile(upstreamFile, commitSha, tag);
                    Console.WriteLine();
                    Console.WriteLine($"Updated {upstreamFile}");
                }
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

    private static (string version, string commitSha, string? tag) DetectUpstreamVersion(string repoPath)
    {
        string commitSha = RunGit(repoPath, "rev-parse HEAD");
        string shortHash = commitSha[..7];

        string describe;
        try
        {
            describe = RunGit(repoPath, "describe --tags --long --match v*");
        }
        catch
        {
            // No tags found - use commit count as dev version
            string count = RunGit(repoPath, "rev-list HEAD --count");
            return ($"0.0.0-dev.{count}+{shortHash}", commitSha, null);
        }

        // Parse git describe output: v0.8-3-gabcdef1 or v0.8.0-0-gabcdef1
        var match = Regex.Match(describe, @"^(v(\d+(?:\.\d+(?:\.\d+)?)?))-(\d+)-g[0-9a-f]+$");
        if (!match.Success)
            throw new Exception($"Could not parse git describe output: {describe}");

        string tagName = match.Groups[1].Value;
        string tagVersion = match.Groups[2].Value;
        int commitsAhead = int.Parse(match.Groups[3].Value);

        var parts = tagVersion.Split('.');
        int major = int.Parse(parts[0]);
        int minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        int patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;

        if (commitsAhead == 0)
        {
            return ($"{major}.{minor}.{patch}", commitSha, tagName);
        }
        else
        {
            return ($"{major}.{minor + 1}.0-dev.{commitsAhead}+{shortHash}", commitSha, tagName);
        }
    }

    private static string RunGit(string workingDirectory, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = Process.Start(psi)
            ?? throw new Exception("Failed to start git process");
        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            string error = process.StandardError.ReadToEnd().Trim();
            throw new Exception($"git {arguments} failed: {error}");
        }
        return output;
    }

    private static string? FindSolutionRoot(string startDir)
    {
        string? dir = startDir;
        while (dir != null && !Directory.GetFiles(dir, "*.sln").Any())
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir;
    }

    private static void WriteUpstreamFile(string filePath, string commitSha, string? tag)
    {
        using var writer = new StreamWriter(filePath);
        writer.NewLine = "\n";
        writer.WriteLine($"R3D_UPSTREAM_COMMIT={commitSha}");
        writer.WriteLine($"R3D_UPSTREAM_TAG={tag ?? ""}");
    }
}
