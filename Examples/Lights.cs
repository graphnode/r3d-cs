using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Lights
{
    private const int NUM_LIGHTS = 128;
    private const int GRID_SIZE = 100;

    private static float Randf(float min, float max) => min + (max - min) * Random.Shared.NextSingle();

    public static unsafe int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Many lights example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());

        // Set ambient light
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            env.background.color = Color.Black;
            env.ambient.color = new Color(10, 10, 10, 255);
        });

        // Create plane and cube meshes
        R3D_Mesh plane = R3D_GenMeshPlane(GRID_SIZE, GRID_SIZE, 1, 1);
        R3D_Mesh cube = R3D_GenMeshCube(0.5f, 0.5f, 0.5f);
        R3D_Material material = R3D_GetDefaultMaterial();

        // Allocate transforms for all spheres
        R3D_InstanceBuffer instances = R3D_LoadInstanceBuffer(GRID_SIZE * GRID_SIZE, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        var positions = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        for (int x = -GRID_SIZE/2; x < GRID_SIZE/2; x++) {
            for (int z = -GRID_SIZE/2; z < GRID_SIZE/2; z++) {
                positions[(z+GRID_SIZE/2)*GRID_SIZE + (x+GRID_SIZE/2)] = new Vector3(x + 0.5f, 0, z + 0.5f);
            }
        }
        R3D_UnmapInstances(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);

        // Create lights
        var lights = stackalloc R3D_Light[NUM_LIGHTS];
        for (int i = 0; i < NUM_LIGHTS; i++) {
            lights[i] = R3D_CreateLight(R3D_LightType.R3D_LIGHT_OMNI);
            R3D_SetLightPosition(lights[i], new Vector3(Randf(-GRID_SIZE/2f, GRID_SIZE/2f), Randf(1.0f, 5.0f), Randf(-GRID_SIZE/2f, GRID_SIZE/2f)));
            R3D_SetLightColor(lights[i], ColorFromHSV(Randf(0.0f, 360.0f), 1.0f, 1.0f));
            R3D_SetLightRange(lights[i], Randf(8.0f, 16.0f));
            R3D_SetLightActive(lights[i], true);
            //R3D_EnableShadow(lights[i]);
        }

        // Setup camera
        Camera3D camera = new Camera3D() {
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
            R3D_Begin(camera);
                R3D_DrawMesh(plane, material, new Vector3(0, -0.25f, 0), 1.0f);
                R3D_DrawMeshInstanced(cube, material, instances, GRID_SIZE*GRID_SIZE);
            R3D_End();

            // Optionally show lights shapes
            if (IsKeyDown(KeyboardKey.F)) {
                BeginMode3D(camera);
                for (int i = 0; i < NUM_LIGHTS; i++) {
                    R3D_DrawLightShape(lights[i]);
                }
                EndMode3D();
            }

            DrawFPS(10, 10);
            DrawText("Press 'F' to show the lights", 10, GetScreenHeight()-34, 24, Color.Black);

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadInstanceBuffer(instances);
        R3D_UnloadMesh(cube);
        R3D_UnloadMesh(plane);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}