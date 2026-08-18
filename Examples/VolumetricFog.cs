using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class VolumetricFog
{
    private const int NUM_COLUMNS = 6;

    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Volumetric fog example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetAntiAliasingMode(AntiAliasingMode.Fxaa);

        // Setup environment with volumetric fog in a dark scene
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = Color.Black;
            env.Ambient.Color = new Color(8, 8, 12, 255);

            env.VolumetricFog.Enabled = true;
            env.VolumetricFog.ScatteringDensity = 0.08f;
            env.VolumetricFog.AbsortionDensity = 0.03f;
            env.VolumetricFog.Anisotropy = 0.7f;
        });

        // Create meshes and material
        var plane = R3D.GenMeshPlane(50, 50, 1, 1);
        var column = R3D.GenMeshCylinder(0.4f, 6.0f, 16);
        var sphere = R3D.GenMeshSphere(0.5f, 16, 32);
        var material = R3D.GetDefaultMaterial();

        // Create a spot light shining through the columns to produce light shafts
        var light = R3D.CreateLight(LightType.Spot);
        R3D.SetLightTarget(light, new Vector3(-8, 10, 0), Vector3.Zero);
        R3D.SetLightAngle(light, 15.0f, 30.0f);
        R3D.SetLightRange(light, 32.0f);
        R3D.SetLightEnergy(light, 6.0f);
        R3D.EnableShadow(light);
        R3D.EnableLight(light);

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(8, 2, 3),
            Target = new Vector3(-6, 5, -1),
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Capture mouse
        DisableCursor();

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Free);

            // Toggle volumetric fog
            if (IsKeyPressed(KeyboardKey.F))
                R3D.SetEnvironmentEx((ref env) => env.VolumetricFog.Enabled = !env.VolumetricFog.Enabled);

            // Adjust scattering density and anisotropy
            R3D.SetEnvironmentEx((ref env) =>
            {
                float dt = GetFrameTime();

                if (IsKeyDown(KeyboardKey.Up)) env.VolumetricFog.ScatteringDensity += 0.05f * dt;
                if (IsKeyDown(KeyboardKey.Down)) env.VolumetricFog.ScatteringDensity -= 0.05f * dt;
                env.VolumetricFog.ScatteringDensity = Math.Clamp(env.VolumetricFog.ScatteringDensity, 0.0f, 0.5f);

                if (IsKeyDown(KeyboardKey.Right)) env.VolumetricFog.Anisotropy += 0.5f * dt;
                if (IsKeyDown(KeyboardKey.Left)) env.VolumetricFog.Anisotropy -= 0.5f * dt;
                env.VolumetricFog.Anisotropy = Math.Clamp(env.VolumetricFog.Anisotropy, -0.9f, 0.9f);
            });

            BeginDrawing();
                ClearBackground(Color.Black);

                R3D.Begin(camera);
                    R3D.DrawMesh(plane, material, Vector3.Zero, 1.0f);
                    for (var i = 0; i < NUM_COLUMNS; i++)
                        R3D.DrawMesh(column, material, new Vector3(0, 0, (i - (NUM_COLUMNS - 1) * 0.5f) * 2.5f), 1.0f);
                    R3D.DrawMesh(sphere, material, new Vector3(3, 0.5f, 0), 1.0f);
                R3D.End();

                ref var env = ref R3D.GetEnvironmentEx();
                DrawText("[F] Toggle fog", 10, 34, 20, Color.Lime);
                DrawText($"[Up/Down] Scattering density: {env.VolumetricFog.ScatteringDensity:F3}", 10, 58, 20, Color.Lime);
                DrawText($"[Left/Right] Anisotropy: {env.VolumetricFog.Anisotropy:F2}", 10, 82, 20, Color.Lime);

                DrawFPS(10, 10);
            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMaterial(material);
        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(column);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
