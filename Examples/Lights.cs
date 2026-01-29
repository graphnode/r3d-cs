using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Lights
{
    private const int NUM_LIGHTS = 128;
    private const int GRID_SIZE = 100;

    private static float Randf(float min, float max)
    {
        return min + (max - min) * Random.Shared.NextSingle();
    }

    public static unsafe int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Many lights example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Set ambient light
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = Color.Black;
            env.Ambient.Color = new Color(10, 10, 10, 255);
        });

        // Create plane and cube meshes
        var plane = R3D.GenMeshPlane(GRID_SIZE, GRID_SIZE, 1, 1);
        var cube = R3D.GenMeshCube(0.5f, 0.5f, 0.5f);
        var material = R3D.GetDefaultMaterial();

        // Allocate transforms for all spheres
        var instances = R3D.LoadInstanceBuffer(GRID_SIZE * GRID_SIZE, InstanceFlags.Position);
        var positions = R3D.MapInstances<Vector3>(instances, InstanceFlags.Position);
        for (int x = -GRID_SIZE / 2; x < GRID_SIZE / 2; x++)
        for (int z = -GRID_SIZE / 2; z < GRID_SIZE / 2; z++)
            positions[(z + GRID_SIZE / 2) * GRID_SIZE + x + GRID_SIZE / 2] = new Vector3(x + 0.5f, 0, z + 0.5f);

        R3D.UnmapInstances(instances, InstanceFlags.Position);

        // Create lights
        var lights = stackalloc Light[NUM_LIGHTS];
        for (var i = 0; i < NUM_LIGHTS; i++)
        {
            lights[i] = R3D.CreateLight(LightType.Omni);
            R3D.SetLightPosition(lights[i], new Vector3(Randf(-GRID_SIZE / 2f, GRID_SIZE / 2f), Randf(1.0f, 5.0f), Randf(-GRID_SIZE / 2f, GRID_SIZE / 2f)));
            R3D.SetLightColor(lights[i], ColorFromHSV(Randf(0.0f, 360.0f), 1.0f, 1.0f));
            R3D.SetLightRange(lights[i], Randf(8.0f, 16.0f));
            R3D.SetLightActive(lights[i], true);
            //R3D.EnableShadow(lights[i]);
        }

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 10, 10),
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

            // Draw scene
            R3D.Begin(camera);
                R3D.DrawMesh(plane, material, new Vector3(0, -0.25f, 0), 1.0f);
                R3D.DrawMeshInstanced(cube, material, instances, GRID_SIZE*GRID_SIZE);
            R3D.End();

            // Optionally show lights shapes
            if (IsKeyDown(KeyboardKey.F))
            {
                BeginMode3D(camera);
                for (var i = 0; i < NUM_LIGHTS; i++) R3D.DrawLightShape(lights[i]);
                EndMode3D();
            }

            DrawFPS(10, 10);
            DrawText("Press 'F' to show the lights", 10, GetScreenHeight() - 34, 24, Color.Black);

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadInstanceBuffer(instances);
        R3D.UnloadMesh(cube);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
