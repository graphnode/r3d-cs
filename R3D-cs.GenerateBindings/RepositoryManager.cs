using System;
using System.IO;
using System.Runtime.CompilerServices;
using LibGit2Sharp;

namespace R3D_cs.GenerateBindings;

/// <summary>
/// Manages cloning the R3D repository.
/// </summary>
public static class RepositoryManager
{
    /// <summary>
    /// Constructs the path to the local repository based on the caller's file path or a default temporary directory.
    /// </summary>
    public static string GetRepositoryPath([CallerFilePath] string? path = null) 
        => Path.Combine(Path.GetDirectoryName(path) ?? Path.GetTempPath(), "r3d-repo");
    
    /// <summary>
    /// Ensures the repository is available. Uses existing if present, otherwise clones.
    /// </summary>
    /// <param name="repoUrl">The repository URL to clone from.</param>
    /// <param name="gitRef">The branch or tag to clone.</param>
    /// <returns>The path to the repository.</returns>
    public static string EnsureRepository(string repoUrl, string gitRef)
    {
        string repoPath = GetRepositoryPath();

        if (Directory.Exists(repoPath) && Repository.IsValid(repoPath))
        {
            Console.WriteLine($"Using existing repository at: {repoPath}");
            Console.WriteLine("  (Use --force-clone to re-download)");
            return repoPath;
        }

        CloneRepository(repoUrl, repoPath, gitRef);
        return repoPath;
    }

    /// <summary>
    /// Deletes the cached repository if it exists.
    /// </summary>
    public static void DeleteRepository()
    {
        string repoPath = GetRepositoryPath();
        if (Directory.Exists(repoPath))
        {
            Console.WriteLine($"Removing cached repository at {repoPath}...");
            Directory.Delete(repoPath, true);
            Console.WriteLine("Done.");
        }
        else
        {
            Console.WriteLine("No cached repository found.");
        }
    }

    private static void CloneRepository(string repoUrl, string repoPath, string gitRef)
    {
        Console.WriteLine($"Cloning repository from {repoUrl}...");
        Console.WriteLine($"  Branch/tag: {gitRef}");

        // Ensure parent directory exists
        string? parentDir = Path.GetDirectoryName(repoPath);
        if (parentDir != null && !Directory.Exists(parentDir))
            Directory.CreateDirectory(parentDir);

        // Remove any partial/invalid clone
        if (Directory.Exists(repoPath))
        {
            Console.WriteLine("  Removing incomplete repository...");
            Directory.Delete(repoPath, true);
        }

        var cloneOptions = new CloneOptions
        {
            RecurseSubmodules = true,
            Checkout = true,
            BranchName = gitRef,
            FetchOptions = { Depth = 1 },
            OnCheckoutProgress = (_, completed, total) =>
            {
                if (completed == total && total > 0)
                    Console.WriteLine($"  Checkout complete: {total} files");
            }
        };

        try
        {
            Console.WriteLine("  Cloning (this may take a moment)...");
            Repository.Clone(repoUrl, repoPath, cloneOptions);
            Console.WriteLine($"  Repository cloned to: {repoPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Clone failed: {ex.Message}");
            Console.WriteLine("  Trying without branch specification...");

            // Remove partial clone if exists
            if (Directory.Exists(repoPath))
                Directory.Delete(repoPath, true);

            // Try cloning default branch
            cloneOptions.BranchName = null;
            Repository.Clone(repoUrl, repoPath, cloneOptions);
            Console.WriteLine($"  Repository cloned to: {repoPath}");
        }
    }

    /// <summary>
    /// Gets information about the current repository state.
    /// </summary>
    public static (string commitSha, string branch) GetRepositoryInfo(string repoPath)
    {
        using var repo = new Repository(repoPath);
        string sha = repo.Head.Tip?.Sha[..8] ?? "unknown";
        string branch = repo.Head.FriendlyName;
        return (sha, branch);
    }
}
