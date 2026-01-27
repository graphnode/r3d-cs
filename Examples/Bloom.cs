using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Bloom
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Bloom example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());
        
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            // Setup bloom and tonemapping
            env.tonemap.mode = R3D_Tonemap.R3D_TONEMAP_ACES;
            env.bloom.mode = R3D_Bloom.R3D_BLOOM_MIX;
            env.bloom.levels = 1.0f;
            
            // Set background
            env.background.color = Color.Black;
        });
        
        // Create cube mesh and material
        R3D_Mesh cube = R3D_GenMeshCube(1.0f, 1.0f, 1.0f);
        R3D_Material material = R3D_GetDefaultMaterial();
        float hueCube = 0.0f;
        material.emission.color = ColorFromHSV(hueCube, 1.0f, 1.0f);
        material.emission.energy = 1.0f;
        material.albedo.color = Color.Black;

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
                material.emission.color = ColorFromHSV(hueCube, 1.0f, 1.0f);
            }

            // Adjust bloom parameters
            float intensity = R3D_ENVIRONMENT_GET.bloom.intensity;
            int intensityDir = IsKeyDownDelay(KeyboardKey.Right) - IsKeyDownDelay(KeyboardKey.Left);
            AdjustBloomParam(ref intensity, intensityDir, 0.01f, 0.0f, float.PositiveInfinity);
            R3D_ENVIRONMENT_SET((ref env) => env.bloom.intensity = intensity);

            float radius = R3D_ENVIRONMENT_GET.bloom.filterRadius;
            int radiusDir = IsKeyDownDelay(KeyboardKey.Up) - IsKeyDownDelay(KeyboardKey.Down);
            AdjustBloomParam(ref radius, radiusDir, 0.1f, 0.0f, float.PositiveInfinity);
            R3D_ENVIRONMENT_SET((ref env) => env.bloom.filterRadius = radius);

            int levelDir = IsMouseButtonDown(MouseButton.Right) - IsMouseButtonDown(MouseButton.Left);
            float levels = R3D_ENVIRONMENT_GET.bloom.levels;
            AdjustBloomParam(ref levels, levelDir, 0.01f, 0.0f, 1.0f);
            R3D_ENVIRONMENT_SET((ref env) => env.bloom.levels = levels);

            // Draw scene
            if (IsKeyPressed(KeyboardKey.Space)) {
                R3D_ENVIRONMENT_SET((ref env) => env.bloom.mode = (R3D_Bloom)(((int)R3D_ENVIRONMENT_GET.bloom.mode + 1) % ((int)R3D_Bloom.R3D_BLOOM_SCREEN + 1)));
            }

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                R3D_Begin(camera);
                    R3D_DrawMesh(cube, material, Vector3.Zero, 1.0f);
                R3D_End();

                // Draw bloom info
                DrawTextRight($"Mode: {GetBloomModeName()}", 10, 20, Color.Lime);
                DrawTextRight($"Intensity: {R3D_ENVIRONMENT_GET.bloom.intensity:.00}", 40, 20, Color.Lime);
                DrawTextRight($"Filter Radius: {R3D_ENVIRONMENT_GET.bloom.filterRadius:.00}", 70, 20, Color.Lime);
                DrawTextRight($"Levels: {R3D_ENVIRONMENT_GET.bloom.levels:.00}", 100, 20, Color.Lime);

            EndDrawing();
        }

        R3D_UnloadMesh(cube);
        R3D_Close();

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
        var mode = (int)R3D_ENVIRONMENT_GET.bloom.mode;
        return mode is >= 0 and <= (int)R3D_Bloom.R3D_BLOOM_SCREEN ? modes[mode] : "Unknown";
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