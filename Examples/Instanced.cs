using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Instanced
{
    private const int INSTANCE_COUNT = 1000;

    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Instanced rendering example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Set ambient light
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = Color.DarkGray);

        // Create cube mesh and default material
        var mesh = R3D.GenMeshCube(1, 1, 1);
        var material = R3D.GetDefaultMaterial();

        // Generate random transforms and colors for instances
        var instances = R3D.LoadInstanceBuffer(INSTANCE_COUNT, InstanceFlags.Position | InstanceFlags.Rotation |
                                                              InstanceFlags.Scale | InstanceFlags.Color);
        var positions = R3D.MapInstances<Vector3>(instances, InstanceFlags.Position);
        var rotations = R3D.MapInstances<Quaternion>(instances, InstanceFlags.Rotation);
        var scales = R3D.MapInstances<Vector3>(instances, InstanceFlags.Scale);
        var colors = R3D.MapInstances<Color>(instances, InstanceFlags.Color);

        for (int i = 0; i < INSTANCE_COUNT; i++)
        {
            positions[i] = new Vector3(
                (float)GetRandomValue(-50000, 50000) / 1000,
                (float)GetRandomValue(-50000, 50000) / 1000,
                (float)GetRandomValue(-50000, 50000) / 1000
            );
            rotations[i] = Quaternion.CreateFromYawPitchRoll(
                (float)GetRandomValue(-314000, 314000) / 100000,
                (float)GetRandomValue(-314000, 314000) / 100000,
                (float)GetRandomValue(-314000, 314000) / 100000
            );
            scales[i] = new Vector3(
                (float)GetRandomValue(100, 2000) / 1000,
                (float)GetRandomValue(100, 2000) / 1000,
                (float)GetRandomValue(100, 2000) / 1000
            );
            colors[i] = Color.FromHSV(
                (float)GetRandomValue(0, 360000) / 1000, 1.0f, 1.0f
            );
        }

        R3D.UnmapInstances(instances, InstanceFlags.Position | InstanceFlags.Rotation |
                                      InstanceFlags.Scale | InstanceFlags.Color);

        // Setup directional light
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(0, -1, 0));
        R3D.SetLightActive(light, true);

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 2, 2),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Capture mouse
        DisableCursor();

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Free);

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                R3D.Begin(camera);
                    R3D.DrawMeshInstanced(mesh, material, instances, INSTANCE_COUNT);
                R3D.End();

                DrawFPS(10, 10);
            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMaterial(material);
        R3D.UnloadMesh(mesh);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
