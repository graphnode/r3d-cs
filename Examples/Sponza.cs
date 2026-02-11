using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Sponza
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Sponza example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        R3D.SetEnvironmentEx((ref env) =>
        {
            // Post-processing setup
            env.Bloom.Mode = R3D_cs.Bloom.Mix;
            env.Ssao.Enabled = true;

            // Background and ambient
            env.Background.Color = Color.SkyBlue;
            env.Ambient.Color = Color.Gray;
        });

        // Load Sponza model
        R3D.SetTextureFilter(TextureFilter.Anisotropic8X);
        var sponza = R3D.LoadModel("resources/models/Sponza.glb");

        // Setup lights
        var lights = new Light[2];
        for (var i = 0; i < 2; i++)
        {
            lights[i] = R3D.CreateLight(LightType.Omni);
            R3D.SetLightPosition(lights[i], new Vector3(i == 0 ? -10.0f : 10.0f, 20.0f, 0.0f));
            R3D.SetLightActive(lights[i], true);
            R3D.SetLightEnergy(lights[i], 4.0f);
            R3D.SetShadowUpdateMode(lights[i], ShadowUpdateMode.Manual);
            R3D.EnableShadow(lights[i]);
        }

        // Setup camera
        var camera = new Camera3D
        {
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
            if (IsKeyPressed(KeyboardKey.One))
                R3D.SetEnvironmentEx((ref env) => env.Ssao.Enabled = !R3D.GetEnvironmentEx().Ssao.Enabled);

            // Toggle SSIL
            if (IsKeyPressed(KeyboardKey.Two))
                R3D.SetEnvironmentEx((ref env) => env.Ssil.Enabled = !R3D.GetEnvironmentEx().Ssil.Enabled);

            // Toggle SSR
            if (IsKeyPressed(KeyboardKey.Three))
                R3D.SetEnvironmentEx((ref env) => env.Ssr.Enabled = !R3D.GetEnvironmentEx().Ssr.Enabled);

            // Toggle fog
            if (IsKeyPressed(KeyboardKey.Four))
                R3D.SetEnvironmentEx((ref env) => env.Fog.Mode = R3D.GetEnvironmentEx().Fog.Mode == Fog.Disabled ? Fog.Exp : Fog.Disabled);

            // Toggle FXAA
            if (IsKeyPressed(KeyboardKey.Five))
                R3D.SetAntiAliasing(R3D.GetAntiAliasing() == AntiAliasing.Disabled ? AntiAliasing.Fxaa : AntiAliasing.Disabled);

            // Cycle tonemapping
            if (IsMouseButtonPressed(MouseButton.Left))
            {
                var mode = R3D.GetEnvironmentEx().Tonemap.Mode;
                R3D.SetEnvironmentEx((ref env) => env.Tonemap.Mode = (Tonemap)(((int)mode + (int)Tonemap.Count - 1) % (int)Tonemap.Count));
            }

            if (IsMouseButtonPressed(MouseButton.Right))
            {
                var mode = R3D.GetEnvironmentEx().Tonemap.Mode;
                R3D.SetEnvironmentEx((ref env) => env.Tonemap.Mode = (Tonemap)(((int)mode + 1) % (int)Tonemap.Count));
            }

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                // Draw Sponza model
                R3D.Begin(camera);
                    R3D.DrawModel(sponza, Vector3.Zero, 1.0f);
                R3D.End();

                // Draw lights
                BeginMode3D(camera);
                    DrawSphere(R3D.GetLightPosition(lights[0]), 0.5f, Color.White);
                    DrawSphere(R3D.GetLightPosition(lights[1]), 0.5f, Color.White);
                EndMode3D();

                // Display tonemapping
                Tonemap tonemap = R3D.GetEnvironmentEx().Tonemap.Mode;
                string tonemapText = tonemap switch
                {
                    Tonemap.Linear => "< TONEMAP LINEAR >",
                    Tonemap.Reinhard => "< TONEMAP REINHARD >",
                    Tonemap.Filmic => "< TONEMAP FILMIC >",
                    Tonemap.Aces => "< TONEMAP ACES >",
                    Tonemap.Agx => "< TONEMAP AGX >",
                    _ => ""
                };
                DrawText(tonemapText, GetScreenWidth() - MeasureText(tonemapText, 20) - 10, 10, 20, Color.Lime);

                DrawFPS(10, 10);
            EndDrawing();
        }

        // Cleanup
        R3D.UnloadModel(sponza, true);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
