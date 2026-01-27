using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

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
        R3D_Init(GetScreenWidth(), GetScreenHeight());

        // Set ambient light
        R3D_ENVIRONMENT_SET((ref env) => env.ambient.color = Color.DarkGray);

        // Create cube mesh and default material
        R3D_Mesh mesh = R3D_GenMeshCube(1, 1, 1);
        R3D_Material material = R3D_GetDefaultMaterial();

        // Generate random transforms and colors for instances
        R3D_InstanceBuffer instances = R3D_LoadInstanceBuffer(INSTANCE_COUNT, R3D_InstanceFlags.R3D_INSTANCE_POSITION | R3D_InstanceFlags.R3D_INSTANCE_ROTATION | 
                                                                                      R3D_InstanceFlags.R3D_INSTANCE_SCALE | R3D_InstanceFlags.R3D_INSTANCE_COLOR);
        var positions = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        var rotations = R3D_MapInstances<Quaternion>(instances, R3D_InstanceFlags.R3D_INSTANCE_ROTATION);
        var scales = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_SCALE);
        var colors = R3D_MapInstances<Color>(instances, R3D_InstanceFlags.R3D_INSTANCE_COLOR);

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

        R3D_UnmapInstances(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION | R3D_InstanceFlags.R3D_INSTANCE_ROTATION | 
                                      R3D_InstanceFlags.R3D_INSTANCE_SCALE | R3D_InstanceFlags.R3D_INSTANCE_COLOR);

        // Setup directional light
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_DIR);
        R3D_SetLightDirection(light, new Vector3(0, -1, 0));
        R3D_SetLightActive(light, true);

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

                R3D_Begin(camera);
                    R3D_DrawMeshInstanced(mesh, material, instances, INSTANCE_COUNT);
                R3D_End();

                DrawFPS(10, 10);
            EndDrawing();
        }

        // Cleanup
        R3D_UnloadMaterial(material);
        R3D_UnloadMesh(mesh);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}