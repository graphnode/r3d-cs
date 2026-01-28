using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Transparency
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Transparency example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create cube model
        var cube = R3D.GenMeshCube(1, 1, 1);
        var matCube = R3D.MATERIAL_BASE;
        matCube.TransparencyMode = TransparencyMode.Alpha;
        matCube.Albedo.Color = new Color(150, 150, 255, 100);
        matCube.Orm.Occlusion = 1.0f;
        matCube.Orm.Roughness = 0.2f;
        matCube.Orm.Metalness = 0.2f;

        // Create plane model
        var plane = R3D.GenMeshPlane(1000, 1000, 1, 1);
        var matPlane = R3D.MATERIAL_BASE;
        matPlane.Orm.Occlusion = 1.0f;
        matPlane.Orm.Roughness = 1.0f;
        matPlane.Orm.Metalness = 0.0f;

        // Create sphere model
        var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
        var matSphere = R3D.MATERIAL_BASE;
        matSphere.Orm.Occlusion = 1.0f;
        matSphere.Orm.Roughness = 0.25f;
        matSphere.Orm.Metalness = 0.75f;

        // Setup camera
        Camera3D camera = new() {
            Position = new Vector3(0, 2, 2),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Setup lighting
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(10, 10, 10, 255));
        var light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D.SetLightActive(light, true);
        R3D.EnableShadow(light);

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                R3D.Begin(camera);
                    R3D.DrawMesh(plane, matPlane, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D.DrawMesh(sphere, matSphere, Vector3.Zero, 1.0f);
                    R3D.DrawMesh(cube, matCube, Vector3.Zero, 1.0f);
                R3D.End();

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(plane);
        R3D.UnloadMesh(cube);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
