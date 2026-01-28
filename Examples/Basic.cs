using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Basic
{
    public static unsafe int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Basic example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create meshes
        var plane = R3D.GenMeshPlane(1000, 1000, 1, 1);
        var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
        var material = R3D.GetDefaultMaterial();

        // Setup environment
        var env = R3D.GetEnvironment();
        env->Ambient.Color = new Color(10, 10, 10, 255);
        R3D.SetEnvironment(env);

        // Create light
        Light light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D.EnableShadow(light);
        R3D.SetLightActive(light, true);

        // Setup camera
        var camera = new Camera3D() {
            Position = new Vector3(0, 2, 2),
            Target = Vector3.Zero,
            Up = new Vector3(0, 1, 0),
            FovY = 60
        };

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                R3D.Begin(camera);
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