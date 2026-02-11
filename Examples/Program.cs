using System;
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
        new("CustomMesh", CustomMesh.Main)
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

        if (args.Length > 0)
        {
            var example = ExampleList.GetExample(args[0]);
            example?.Main.Invoke();
        }
        else
            RunExamples(ExampleList.AllExamples);
    }

    private static void RunExamples(ExampleInfo[] examples)
    {
        var configFlags = Enum.GetValues<ConfigFlags>();
        foreach (var example in examples)
        {
            example.Main.Invoke();
            foreach (var flag in configFlags) Raylib.ClearWindowState(flag);
        }
    }
}
