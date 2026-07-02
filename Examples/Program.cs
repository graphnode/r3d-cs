using System;
using System.Diagnostics;
using System.Linq;
using Raylib_cs;

namespace Examples;

public class ExampleInfo(string name, Func<int> main)
{
    public string Name { get; set; } = name;

    public Func<int> Main { get; set; } = main;
}

public static class ExampleList
{
    public static readonly ExampleInfo[] AllExamples =
    [
        new("Basic", Basic.Main),
        new("Probe", Probe.Main),
        new("Lights", Lights.Main),
        new("Pbr", Pbr.Main),
        new("Transparency", Transparency.Main),
        new("Skybox", Skybox.Main),
        new("Sponza", Sponza.Main),
        new("Sprite", Sprite.Main),
        new("Animation", Animation.Main),
        new("Bloom", Bloom.Main),
        new("Resize", Resize.Main),
        new("Shader", Shader.Main),
        new("Kinematics", Kinematics.Main),
        new("Particles", Particles.Main),
        new("Instanced", Instanced.Main),
        new("Billboards", Billboards.Main),
        new("Sun", Sun.Main),
        new("Dof", Dof.Main),
        new("Decal", Decal.Main),
        new("CustomMesh", CustomMesh.Main),
        new("AnimTree", AnimTree.Main),
        new("Multiview", Multiview.Main),
        new("Stencil", Stencil.Main),
        new("ToTexture", ToTexture.Main)
    ];

    public static ExampleInfo? GetExample(string name)
    {
        var example = Array.Find(AllExamples, x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
        );
        return example;
    }
}

internal static class Program
{
    private static unsafe void Main(string[] args)
    {
        Raylib.SetTraceLogCallback(&Logging.LogConsole);

        var examples = args.Length > 0
            ? args.Select(ExampleList.GetExample).Where(e => e != null).ToArray()
            : ExampleList.AllExamples;

        if (examples.Length <= 1)
        {
            examples.FirstOrDefault()?.Main.Invoke();
            return;
        }

        var exe = Environment.ProcessPath!;
        foreach (var example in examples)
        {
            var process = Process.Start(new ProcessStartInfo(exe, example.Name)
            {
                UseShellExecute = false
            })!;
            process.WaitForExit();
        }
    }
}
