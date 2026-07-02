using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Multiview
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d-cs] - Multiview");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create meshes
        var plane = R3D.GenMeshPlane(1000, 1000, 1, 1);
        var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
        var material = R3D.GetDefaultMaterial();

        // Setup environment
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(10, 10, 10, 255));

        // Create light
        var light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D.EnableShadow(light);
        R3D.SetLightActive(light, true);

        // Setup two R3D cameras orbiting in opposite directions
        var cam0 = R3D.CAMERA_BASE;
        var cam1 = R3D.CAMERA_BASE;

        // Main loop
        while (!WindowShouldClose())
        {
            float time = (float)GetTime();

            cam0.Position = new Vector3(4.0f * MathF.Cos(time), 4.0f, 4.0f * MathF.Sin(time));
            cam1.Position = new Vector3(4.0f * MathF.Cos(-time), 4.0f, 4.0f * MathF.Sin(-time));

            R3D.CameraLookAt(ref cam0, Vector3.Zero, new Vector3(0, 1, 0));
            R3D.CameraLookAt(ref cam1, Vector3.Zero, new Vector3(0, 1, 0));

            float hw = GetScreenWidth() / 2.0f;
            float h = GetScreenHeight();

            // Each view renders the same scene into one half of the screen
            var view0 = new View { Camera = cam0, Viewport = new Rectangle(0, 0, hw, h) };
            var view1 = new View { Camera = cam1, Viewport = new Rectangle(hw, 0, hw, h) };

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                R3D.BeginPro(view0);
                    R3D.DrawMesh(plane, material, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D.DrawMesh(sphere, material, Vector3.Zero, 1.0f);
                R3D.End();

                R3D.BeginPro(view1);
                    R3D.DrawMesh(plane, material, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D.DrawMesh(sphere, material, Vector3.Zero, 1.0f);
                R3D.End();

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
