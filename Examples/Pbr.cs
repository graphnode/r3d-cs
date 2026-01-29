using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using CubemapLayout = R3D_cs.CubemapLayout;

namespace Examples;

public static class Pbr
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - PBR example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetAntiAliasing(AntiAliasing.Fxaa);

        var cubemap = R3D.LoadCubemap("resources/panorama/indoor.hdr", CubemapLayout.AutoDetect);
        var ambientMap = R3D.GenAmbientMap(cubemap, AmbientFlags.Illumination | AmbientFlags.Reflection);

        R3D.SetEnvironmentEx((ref env) =>
        {
            // Setup environment sky
            env.Background.SkyBlur = 0.775f;
            env.Background.Sky = cubemap;

            // Setup environment ambient
            env.Ambient.Map = ambientMap;

            // Setup bloom
            env.Bloom.Mode = R3D_cs.Bloom.Mix;
            env.Bloom.Intensity = 0.02f;

            // Setup tonemapping
            env.Tonemap.Mode = Tonemap.Filmic;
            env.Tonemap.Exposure = 0.5f;
            env.Tonemap.White = 4.0f;
        });

        // Load model
        R3D.SetTextureFilter(TextureFilter.Anisotropic4X);
        var model = R3D.LoadModel("resources/models/DamagedHelmet.glb");
        var modelMatrix = Matrix4x4.Identity;
        var modelScale = 1.0f;

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 0, 2.5f),
            Target = Vector3.Zero,
            Up = new Vector3(0, 1, 0),
            FovY = 60
        };

        // Main loop
        while (!WindowShouldClose())
        {
            // Update model scale with mouse wheel
            modelScale = Math.Clamp(modelScale + GetMouseWheelMove() * 0.1f, 0.25f, 2.5f);

            // Rotate model with left mouse button
            if (IsMouseButtonDown(MouseButton.Left))
            {
                float pitch = GetMouseDelta().Y * 0.005f / modelScale;
                float yaw = GetMouseDelta().X * 0.005f / modelScale;
                var rotate = Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, 0.0f);
                modelMatrix *= rotate;
            }

            BeginDrawing();
                ClearBackground(Color.RayWhite);
                R3D.Begin(camera);
                    var scale = Matrix4x4.CreateScale(modelScale);
                    var transform = modelMatrix * scale;
                    R3D.DrawModelPro(model, Matrix4x4.Transpose(transform));
                R3D.End();
            EndDrawing();
        }

        // Cleanup
        R3D.UnloadModel(model, true);
        R3D.UnloadAmbientMap(ambientMap);
        R3D.UnloadCubemap(cubemap);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
