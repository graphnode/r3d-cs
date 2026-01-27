using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

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
        R3D_Init(GetScreenWidth(), GetScreenHeight());

        // Create sphere mesh and materials
        R3D_Mesh sphere = R3D_GenMeshSphere(0.5f, 64, 64);
        R3D_Material[] materials = new R3D_Material[5];
        for (int i = 0; i < 5; i++)
        {
            materials[i] = R3D_GetDefaultMaterial();
            materials[i].albedo.color = Color.FromHSV((float)i / 5 * 330, 1.0f, 1.0f);
        }

        // Set up directional light
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_DIR);
        R3D_SetLightDirection(light, new Vector3(0, 0, -1));
        R3D_SetLightActive(light, true);

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 2, 2),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Current blit state
        R3D_AspectMode aspect = R3D_AspectMode.R3D_ASPECT_EXPAND;
        R3D_UpscaleMode upscale = R3D_UpscaleMode.R3D_UPSCALE_NEAREST;

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);

            // Toggle aspect keep
            if (IsKeyPressed(KeyboardKey.R)) {
                aspect = (R3D_AspectMode)(((int)aspect + 1) % 2);
                R3D_SetAspectMode(aspect);
            }

            // Toggle linear filtering
            if (IsKeyPressed(KeyboardKey.F)) {
                upscale = (R3D_UpscaleMode)(((int)upscale + 1) % 4);
                R3D_SetUpscaleMode(upscale);
            }

            BeginDrawing();
                ClearBackground(Color.Black);

                // Draw spheres
                R3D_Begin(camera);
                    for (int i = 0; i < 5; i++) {
                        R3D_DrawMesh(sphere, materials[i], new Vector3((float)i - 2, 0, 0), 1.0f);
                    }
                R3D_End();

                // Draw info
                DrawText($"Resize mode: {GetAspectModeName(aspect)}", 10, 10, 20, Color.RayWhite);
                DrawText($"Filter mode: {GetUpscaleModeName(upscale)}", 10, 40, 20, Color.RayWhite);

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadMesh(sphere);
        R3D_Close();

        CloseWindow();

        return 0;
    }
    
    private static string GetAspectModeName(R3D_AspectMode mode)
    {
        switch (mode) {
            case R3D_AspectMode.R3D_ASPECT_EXPAND: return "EXPAND";
            case R3D_AspectMode.R3D_ASPECT_KEEP: return "KEEP";
        }
        return "UNKNOWN";
    }

    private static string GetUpscaleModeName(R3D_UpscaleMode mode)
    {
        switch (mode) {
            case R3D_UpscaleMode.R3D_UPSCALE_NEAREST: return "NEAREST";
            case R3D_UpscaleMode.R3D_UPSCALE_LINEAR: return "LINEAR";
            case R3D_UpscaleMode.R3D_UPSCALE_BICUBIC: return "BICUBIC";
            case R3D_UpscaleMode.R3D_UPSCALE_LANCZOS: return "LANCZOS";
        }
        return "UNKNOWN";
    }
}