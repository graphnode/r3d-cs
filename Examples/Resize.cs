using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Material = R3D_cs.Material;

namespace Examples;

public static class Resize
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Resize example");
        SetWindowState(ConfigFlags.ResizableWindow);
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create sphere mesh and materials
        var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
        var materials = new Material[5];
        for (int i = 0; i < 5; i++)
        {
            materials[i] = R3D.GetDefaultMaterial();
            materials[i].Albedo.Color = Color.FromHSV((float)i / 5 * 330, 1.0f, 1.0f);
        }

        // Set up directional light
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(0, 0, -1));
        R3D.SetLightActive(light, true);

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 2, 2),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Current blit state
        AspectMode aspect = AspectMode.Expand;
        UpscaleMode upscale = UpscaleMode.Nearest;

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);

            // Toggle aspect keep
            if (IsKeyPressed(KeyboardKey.R)) {
                aspect = (AspectMode)(((int)aspect + 1) % 2);
                R3D.SetAspectMode(aspect);
            }

            // Toggle linear filtering
            if (IsKeyPressed(KeyboardKey.F)) {
                upscale = (UpscaleMode)(((int)upscale + 1) % 4);
                R3D.SetUpscaleMode(upscale);
            }

            BeginDrawing();
                ClearBackground(Color.Black);

                // Draw spheres
                R3D.Begin(camera);
                    for (int i = 0; i < 5; i++) {
                        R3D.DrawMesh(sphere, materials[i], new Vector3((float)i - 2, 0, 0), 1.0f);
                    }
                R3D.End();

                // Draw info
                DrawText($"Resize mode: {GetAspectModeName(aspect)}", 10, 10, 20, Color.RayWhite);
                DrawText($"Filter mode: {GetUpscaleModeName(upscale)}", 10, 40, 20, Color.RayWhite);

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMesh(sphere);
        R3D.Close();

        CloseWindow();

        return 0;
    }

    private static string GetAspectModeName(AspectMode mode)
    {
        switch (mode) {
            case AspectMode.Expand: return "EXPAND";
            case AspectMode.Keep: return "KEEP";
        }
        return "UNKNOWN";
    }

    private static string GetUpscaleModeName(UpscaleMode mode)
    {
        switch (mode) {
            case UpscaleMode.Nearest: return "NEAREST";
            case UpscaleMode.Linear: return "LINEAR";
            case UpscaleMode.Bicubic: return "BICUBIC";
            case UpscaleMode.Lanczos: return "LANCZOS";
        }
        return "UNKNOWN";
    }
}
