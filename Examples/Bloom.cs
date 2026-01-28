using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Bloom
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Bloom example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        R3D.SetEnvironmentEx((ref env) =>
        {
            // Setup bloom and tonemapping
            env.Tonemap.Mode = Tonemap.Aces;
            env.Bloom.Mode = R3D_cs.Bloom.Mix;
            env.Bloom.Levels = 1.0f;

            // Set background
            env.Background.Color = Color.Black;
        });

        // Create cube mesh and material
        var cube = R3D.GenMeshCube(1.0f, 1.0f, 1.0f);
        var material = R3D.GetDefaultMaterial();
        float hueCube = 0.0f;
        material.Emission.Color = ColorFromHSV(hueCube, 1.0f, 1.0f);
        material.Emission.Energy = 1.0f;
        material.Albedo.Color = Color.Black;

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 3.5f, 5),
            Target   = Vector3.Zero,
            Up       = Vector3.UnitY,
            FovY     = 60
        };

        // Main loop
        while (!WindowShouldClose())
        {
            float delta = GetFrameTime();
            UpdateCamera(ref camera, CameraMode.Orbital);

            // Change cube color
            if (IsKeyDown(KeyboardKey.C)) {
                hueCube = Raymath.Wrap(hueCube + 45.0f * delta, 0, 360);
                material.Emission.Color = ColorFromHSV(hueCube, 1.0f, 1.0f);
            }

            // Adjust bloom parameters
            float intensity = R3D.GetEnvironmentEx().Bloom.Intensity;
            int intensityDir = IsKeyDownDelay(KeyboardKey.Right) - IsKeyDownDelay(KeyboardKey.Left);
            AdjustBloomParam(ref intensity, intensityDir, 0.01f, 0.0f, float.PositiveInfinity);
            R3D.SetEnvironmentEx((ref env) => env.Bloom.Intensity = intensity);

            float radius = R3D.GetEnvironmentEx().Bloom.FilterRadius;
            int radiusDir = IsKeyDownDelay(KeyboardKey.Up) - IsKeyDownDelay(KeyboardKey.Down);
            AdjustBloomParam(ref radius, radiusDir, 0.1f, 0.0f, float.PositiveInfinity);
            R3D.SetEnvironmentEx((ref env) => env.Bloom.FilterRadius = radius);

            int levelDir = IsMouseButtonDown(MouseButton.Right) - IsMouseButtonDown(MouseButton.Left);
            float levels = R3D.GetEnvironmentEx().Bloom.Levels;
            AdjustBloomParam(ref levels, levelDir, 0.01f, 0.0f, 1.0f);
            R3D.SetEnvironmentEx((ref env) => env.Bloom.Levels = levels);

            // Draw scene
            if (IsKeyPressed(KeyboardKey.Space)) {
                R3D.SetEnvironmentEx((ref env) => env.Bloom.Mode = (R3D_cs.Bloom)(((int)R3D.GetEnvironmentEx().Bloom.Mode + 1) % ((int)R3D_cs.Bloom.Screen + 1)));
            }

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                R3D.Begin(camera);
                    R3D.DrawMesh(cube, material, Vector3.Zero, 1.0f);
                R3D.End();

                var env = R3D.GetEnvironmentEx();
                
                // Draw bloom info
                DrawTextRight($"Mode: {GetBloomModeName()}", 10, 20, Color.Lime);
                DrawTextRight($"Intensity: {env.Bloom.Intensity:.00}", 40, 20, Color.Lime);
                DrawTextRight($"Filter Radius: {env.Bloom.FilterRadius:.00}", 70, 20, Color.Lime);
                DrawTextRight($"Levels: {env.Bloom.Levels:.00}", 100, 20, Color.Lime);

            EndDrawing();
        }

        R3D.UnloadMesh(cube);
        R3D.Close();

        CloseWindow();

        return 0;
    }

    private static CBool IsKeyDownDelay(KeyboardKey key)
    {
        return IsKeyPressedRepeat(key) || IsKeyPressed(key);
    }

    private static string GetBloomModeName()
    {
        string[] modes = ["Disabled", "Mix", "Additive", "Screen"];
        var mode = (int)R3D.GetEnvironmentEx().Bloom.Mode;
        return mode is >= 0 and <= (int)R3D_cs.Bloom.Screen ? modes[mode] : "Unknown";
    }

    private static void DrawTextRight(string text, int y, int fontSize, Color color)
    {
        int width = MeasureText(text, fontSize);
        DrawText(text, GetScreenWidth() - width - 10, y, fontSize, color);
    }

    private static void AdjustBloomParam(ref float param, int direction, float step, float min, float max)
    {
        if (direction != 0)
            param = Math.Clamp(param + direction * step, min, max);
    }
}
