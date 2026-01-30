using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace R3D_cs.GenerateBindings;

/// <summary>
///     Manages cloning the R3D repository using git commands.
/// </summary>
public static class RepositoryManager
{
    /// <summary>
    ///     Retrieves the file system path where the repository is cached or stored.
    /// </summary>
    /// <returns>The full path to the repository cache directory.</returns>
    public static string GetRepositoryPath()
    {
        return Path.Combine(Path.GetTempPath(), "r3d-repo");
    }

    /// <summary>
    ///     Ensures the repository is available. Uses existing if present, otherwise clones.
    ///     When using auto-detected latest tag, checks if cached repo is outdated and updates if needed.
    /// </summary>
    /// <param name="repoUrl">The repository URL to clone from.</param>
    /// <param name="gitRef">The branch or tag to clone.</param>
    /// <param name="isAutoDetectedLatest">True if gitRef was auto-detected as the latest tag (not user-specified).</param>
    /// <returns>The path to the repository.</returns>
    public static string EnsureRepository(string repoUrl, string gitRef, bool isAutoDetectedLatest = false)
    {
        string repoPath = GetRepositoryPath();
        Console.WriteLine($"Cache location: {repoPath}");

        if (Directory.Exists(repoPath) && Directory.Exists(Path.Combine(repoPath, ".git")))
        {
            // Check if we need to update the cached repo
            if (isAutoDetectedLatest)
            {
                (_, string currentRef) = GetRepositoryInfo(repoPath);
                if (currentRef != gitRef)
                {
                    Console.WriteLine($"Cached repository is on '{currentRef}', but latest is '{gitRef}'");
                    Console.WriteLine("  Updating to latest version...");
                    DeleteDirectoryRecursive(repoPath);
                    CloneRepository(repoUrl, repoPath, gitRef);
                    return repoPath;
                }
            }

            Console.WriteLine($"Using existing repository at: {repoPath}");
            Console.WriteLine("  (Use --force-clone to re-download)");
            return repoPath;
        }

        CloneRepository(repoUrl, repoPath, gitRef);
        return repoPath;
    }

    /// <summary>
    ///     Deletes the cached repository if it exists.
    /// </summary>
    public static void DeleteRepository()
    {
        string repoPath = GetRepositoryPath();
        if (Directory.Exists(repoPath))
        {
            Console.WriteLine($"Removing cached repository at {repoPath}...");
            DeleteDirectoryRecursive(repoPath);
            Console.WriteLine("Done.");
        }
        else
            Console.WriteLine("No cached repository found.");
    }

    /// <summary>
    ///     Recursively deletes a directory, clearing read-only attributes first.
    ///     This is needed on Windows because git marks pack files as read-only.
    /// </summary>
    private static void DeleteDirectoryRecursive(string path)
    {
        var dir = new DirectoryInfo(path);

        foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories)) file.Attributes = FileAttributes.Normal;

        dir.Delete(true);
    }

    private static void CloneRepository(string repoUrl, string repoPath, string gitRef)
    {
        Console.WriteLine($"Cloning repository from {repoUrl}...");
        Console.WriteLine($"  Target ref: {gitRef}");

        // Ensure parent directory exists
        string? parentDir = Path.GetDirectoryName(repoPath);
        if (parentDir != null && !Directory.Exists(parentDir))
            Directory.CreateDirectory(parentDir);

        // Remove any partial/invalid clone
        if (Directory.Exists(repoPath))
        {
            Console.WriteLine("  Removing incomplete repository...");
            DeleteDirectoryRecursive(repoPath);
        }

        // Clone with the specific branch/tag and recurse submodules
        Console.WriteLine("  Cloning (this may take a moment)...");
        RunGit($"clone --branch {gitRef} --recurse-submodules --depth 1 {repoUrl} \"{repoPath}\"");

        Console.WriteLine($"  Repository cloned to: {repoPath}");
    }

    /// <summary>
    ///     Gets information about the current repository state.
    /// </summary>
    public static (string commitSha, string branch) GetRepositoryInfo(string repoPath)
    {
        string sha = RunGit($"-C \"{repoPath}\" rev-parse --short HEAD").Trim();
        string branch;

        // Try to get tag name first, fall back to branch
        try
        {
            branch = RunGit($"-C \"{repoPath}\" describe --tags --exact-match").Trim();
        }
        catch
        {
            branch = RunGit($"-C \"{repoPath}\" rev-parse --abbrev-ref HEAD").Trim();
        }

        return (sha, branch);
    }

    /// <summary>
    ///     Gets the latest version tag at or before the current commit.
    /// </summary>
    /// <param name="repoPath">The path to the repository.</param>
    /// <returns>The version if found, null otherwise.</returns>
    public static Version? GetLatestVersionFromTags(string repoPath)
    {
        try
        {
            // Get the most recent tag reachable from HEAD
            string tag = RunGit($"-C \"{repoPath}\" describe --tags --abbrev=0").Trim();
            string versionString = tag.TrimStart('v', 'V');

            if (Version.TryParse(versionString, out var version))
                return version;
        }
        catch
        {
            // No tags found or git command failed
        }

        return null;
    }

    /// <summary>
    ///     Fetches the latest semver tag from a remote repository.
    /// </summary>
    /// <param name="repoUrl">The repository URL to query.</param>
    /// <returns>The latest tag name, or null if no tags found.</returns>
    public static string? GetLatestTag(string repoUrl)
    {
        Console.WriteLine($"Fetching tags from {repoUrl}...");

        string output = RunGit($"ls-remote --tags {repoUrl}");
        var tags = new List<(string name, Version version)>();

        // Pattern to match semver-like tags (with optional 'v' prefix)
        var versionPattern = new Regex(@"^v?(\d+(?:\.\d+)*)$", RegexOptions.IgnoreCase);

        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Format: <sha>\trefs/tags/<tagname>
            string[] parts = line.Split('\t');
            if (parts.Length < 2)
                continue;

            string refName = parts[1];
            if (!refName.StartsWith("refs/tags/"))
                continue;

            string tagName = refName["refs/tags/".Length..];

            // Skip annotated tag dereferenced refs (e.g., refs/tags/v1.0^{})
            if (tagName.Contains('^'))
                continue;

            var match = versionPattern.Match(tagName);
            if (match.Success)
            {
                // Parse version, padding with zeros for comparison
                string versionStr = match.Groups[1].Value;
                int[] versionParts = versionStr.Split('.').Select(int.Parse).ToArray();

                // Pad to 4 parts for consistent comparison
                while (versionParts.Length < 4)
                    versionParts = [.. versionParts, 0];

                var version = new Version(versionParts[0], versionParts[1],
                    versionParts.Length > 2 ? versionParts[2] : 0,
                    versionParts.Length > 3 ? versionParts[3] : 0);
                tags.Add((tagName, version));
            }
        }

        if (tags.Count == 0)
        {
            Console.WriteLine("  No version tags found.");
            return null;
        }

        // Sort by version descending and get the latest
        var latest = tags.OrderByDescending(t => t.version).First();
        Console.WriteLine($"  Found {tags.Count} version tags. Latest: {latest.name}");

        return latest.name;
    }

    private static string RunGit(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process;
        try
        {
            process = Process.Start(psi) ?? throw new Exception("Failed to start git process");
        }
        catch (Win32Exception)
        {
            throw new Exception(
                "Git is not installed or not found in PATH. " +
                "Please install Git from https://git-scm.com/ and ensure it's available in your system PATH.");
        }

        using (process)
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"git {arguments} failed: {error}");

            return output;
        }
    }
}
