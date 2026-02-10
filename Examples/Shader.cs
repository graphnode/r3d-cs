using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Shader
{
    public static unsafe int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Shader example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Setup environment
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Ambient.Color = new Color(10, 10, 10, 255);
            env.Bloom.Mode = R3D_cs.Bloom.Additive;
        });

        // Create meshes
        var plane = R3D.GenMeshPlane(1000, 1000, 1, 1);
        var torus = R3D.GenMeshTorus(0.5f, 0.1f, 32, 16);

        // Create material
        var material = R3D.GetDefaultMaterial();
        material.Shader = R3D.LoadSurfaceShader("resources/shaders/material.glsl");

        // Generate a texture for custom sampler
        var image = GenImageChecked(512, 512, 16, 32, Color.White, Color.Black);
        var texture = LoadTextureFromImage(image);
        UnloadImage(image);

        // Set custom sampler
        R3D.SetSurfaceShaderSampler(ref *material.Shader, "u_texture", texture);

        // Load a screen shader
        ScreenShader* screenShader = R3D.LoadScreenShader("resources/shaders/screen.glsl");
        R3D.SetScreenShaderChain(&screenShader, 1);

        // Create light
        var light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D.EnableShadow(light);
        R3D.SetLightActive(light, true);

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 2, 2),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                float time = 2.0f * (float)GetTime();
                R3D.SetScreenShaderUniform(ref *screenShader, "u_time", &time);
                R3D.SetSurfaceShaderUniform(ref *material.Shader, "u_time", &time);

                R3D.Begin(camera);
                    var planeMaterial = R3D.GetDefaultMaterial();
                    R3D.DrawMesh(plane, planeMaterial, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D.DrawMesh(torus, material, Vector3.Zero, 1.0f);
                R3D.End();

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadSurfaceShader(ref *material.Shader);
        R3D.UnloadScreenShader(ref *screenShader);
        R3D.UnloadMesh(torus);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
