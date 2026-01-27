using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Pbr
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - PBR example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());
        R3D_SetAntiAliasing(R3D_AntiAliasing.R3D_ANTI_ALIASING_FXAA);
        
        var cubemap = R3D_LoadCubemap("resources/panorama/indoor.hdr", R3D_CubemapLayout.R3D_CUBEMAP_LAYOUT_AUTO_DETECT);
        var ambientMap = R3D_GenAmbientMap(cubemap, R3D_AmbientFlags.R3D_AMBIENT_ILLUMINATION | R3D_AmbientFlags.R3D_AMBIENT_REFLECTION);
        
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            // Setup environment sky
            env.background.skyBlur = 0.775f;
            env.background.sky = cubemap;
            
            // Setup environment ambient
            env.ambient.map = ambientMap;

            // Setup bloom
            env.bloom.mode = R3D_Bloom.R3D_BLOOM_MIX;
            env.bloom.intensity = 0.02f;

            // Setup tonemapping
            env.tonemap.mode = R3D_Tonemap.R3D_TONEMAP_FILMIC;
            env.tonemap.exposure = 0.5f;
            env.tonemap.white = 4.0f;
        });
        
        // Load model
        R3D_SetTextureFilter(TextureFilter.Anisotropic4X);
        var model = R3D_LoadModel("resources/models/DamagedHelmet.glb");
        Matrix4x4 modelMatrix = Matrix4x4.Identity;
        float modelScale = 1.0f;

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
                float pitch = (GetMouseDelta().Y * 0.005f) / modelScale;
                float yaw   = (GetMouseDelta().X * 0.005f) / modelScale;
                var rotate  = Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, 0.0f);
                modelMatrix *= rotate;
            }

            BeginDrawing();
                ClearBackground(Color.RayWhite);
                R3D_Begin(camera);
                    var scale = Matrix4x4.CreateScale(modelScale);
                    var transform = modelMatrix * scale;
                    R3D_DrawModelPro(model, Matrix4x4.Transpose(transform));
                R3D_End();
            EndDrawing();
        }

        // Cleanup
        R3D_UnloadModel(model, true);
        R3D_UnloadAmbientMap(ambientMap);
        R3D_UnloadCubemap(cubemap);
        R3D_Close();
        CloseWindow();
        return 0;
    }
}
