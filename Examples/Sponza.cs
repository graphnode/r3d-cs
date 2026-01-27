using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Sponza
{
    public static unsafe int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Sponza example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());
        
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            // Post-processing setup
            env.bloom.mode = R3D_Bloom.R3D_BLOOM_MIX;
            env.ssao.enabled = true;
            
            // Background and ambient
            env.background.color = Color.SkyBlue;
            env.ambient.color = Color.Gray;
        });

        // Load Sponza model
        R3D_SetTextureFilter(TextureFilter.Anisotropic8X);
        R3D_Model sponza = R3D_LoadModel("resources/models/Sponza.glb");

        // Setup lights
        var lights = stackalloc R3D_Light[2];
        for (int i = 0; i < 2; i++) {
            lights[i] = R3D_CreateLight(R3D_LightType.R3D_LIGHT_OMNI);
            R3D_SetLightPosition(lights[i], new Vector3(i == 0 ? -10.0f : 10.0f, 20.0f, 0.0f));
            R3D_SetLightActive(lights[i], true);
            R3D_SetLightEnergy(lights[i], 4.0f);
            R3D_SetShadowUpdateMode(lights[i], R3D_ShadowUpdateMode.R3D_SHADOW_UPDATE_MANUAL);
            R3D_EnableShadow(lights[i]);
        }

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(8.0f, 1.0f, 0.5f),
            Target = new Vector3(0.0f, 2.0f, -2.0f),
            Up = Vector3.UnitY,
            FovY = 60.0f
        };

        // Capture mouse
        DisableCursor();

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Free);

            // Toggle SSAO
            if (IsKeyPressed(KeyboardKey.One)) {
                R3D_ENVIRONMENT_SET((ref env) => env.ssao.enabled = !R3D_ENVIRONMENT_GET.ssao.enabled);
            }

            // Toggle SSIL
            if (IsKeyPressed(KeyboardKey.Two)) {
                R3D_ENVIRONMENT_SET((ref env) => env.ssil.enabled = !R3D_ENVIRONMENT_GET.ssil.enabled);
            }

            // Toggle SSR
            if (IsKeyPressed(KeyboardKey.Three)) {
                R3D_ENVIRONMENT_SET((ref env) => env.ssr.enabled = !R3D_ENVIRONMENT_GET.ssr.enabled);
            }

            // Toggle fog
            if (IsKeyPressed(KeyboardKey.Four)) {
                R3D_ENVIRONMENT_SET((ref env) => env.fog.mode = R3D_ENVIRONMENT_GET.fog.mode == R3D_Fog.R3D_FOG_DISABLED ? R3D_Fog.R3D_FOG_EXP : R3D_Fog.R3D_FOG_DISABLED);
            }

            // Toggle FXAA
            if (IsKeyPressed(KeyboardKey.Five)) {
                R3D_SetAntiAliasing(R3D_GetAntiAliasing() == R3D_AntiAliasing.R3D_ANTI_ALIASING_DISABLED ? R3D_AntiAliasing.R3D_ANTI_ALIASING_FXAA : R3D_AntiAliasing.R3D_ANTI_ALIASING_DISABLED);
            }

            // Cycle tonemapping
            if (IsMouseButtonPressed(MouseButton.Left)) {
                R3D_Tonemap mode = R3D_ENVIRONMENT_GET.tonemap.mode;
                R3D_ENVIRONMENT_SET((ref env) => env.tonemap.mode = (R3D_Tonemap)(((int)mode + (int)R3D_Tonemap.R3D_TONEMAP_COUNT - 1) % (int)R3D_Tonemap.R3D_TONEMAP_COUNT));
            }
            if (IsMouseButtonPressed(MouseButton.Right)) {
                R3D_Tonemap mode = R3D_ENVIRONMENT_GET.tonemap.mode;
                R3D_ENVIRONMENT_SET((ref env) => env.tonemap.mode = (R3D_Tonemap)(((int)mode + 1) % (int)R3D_Tonemap.R3D_TONEMAP_COUNT));
            }

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                // Draw Sponza model
                R3D_Begin(camera);
                    R3D_DrawModel(sponza, Vector3.Zero, 1.0f);
                R3D_End();

                // Draw lights
                BeginMode3D(camera);
                    DrawSphere(R3D_GetLightPosition(lights[0]), 0.5f, Color.White);
                    DrawSphere(R3D_GetLightPosition(lights[1]), 0.5f, Color.White);
                EndMode3D();

                // Display tonemapping
                R3D_Tonemap tonemap = R3D_ENVIRONMENT_GET.tonemap.mode;
                string tonemapText = tonemap switch
                {
                    R3D_Tonemap.R3D_TONEMAP_LINEAR => "< TONEMAP LINEAR >",
                    R3D_Tonemap.R3D_TONEMAP_REINHARD => "< TONEMAP REINHARD >",
                    R3D_Tonemap.R3D_TONEMAP_FILMIC => "< TONEMAP FILMIC >",
                    R3D_Tonemap.R3D_TONEMAP_ACES => "< TONEMAP ACES >",
                    R3D_Tonemap.R3D_TONEMAP_AGX => "< TONEMAP AGX >",
                    _ => ""
                };
                DrawText(tonemapText, GetScreenWidth() - MeasureText(tonemapText, 20) - 10, 10, 20, Color.Lime);

                DrawFPS(10, 10);
            EndDrawing();
        }

        // Cleanup
        R3D_UnloadModel(sponza, true);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}