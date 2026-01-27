using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Billboards
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Billboards example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());
        R3D_SetTextureFilter(TextureFilter.Point);

        // Set background/ambient color
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            env.background.color = new Color(102, 191, 255, 255);
            env.ambient.color = new Color(10, 19, 25, 255);
            env.tonemap.mode = R3D_Tonemap.R3D_TONEMAP_FILMIC;
        });

        // Create ground mesh and material
        R3D_Mesh meshGround = R3D_GenMeshPlane(200, 200, 1, 1);
        R3D_Material matGround = R3D_GetDefaultMaterial();
        matGround.albedo.color = Color.Green;

        // Create billboard mesh and material
        R3D_Mesh meshBillboard = R3D_GenMeshQuad(1.0f, 1.0f, 1, 1, new Vector3(0.0f, 0.0f, 1.0f));
        meshBillboard.shadowCastMode = R3D_ShadowCastMode.R3D_SHADOW_CAST_ON_DOUBLE_SIDED;

        R3D_Material matBillboard = R3D_GetDefaultMaterial();
        matBillboard.albedo = R3D_LoadAlbedoMap("resources/images/tree.png", Color.White);
        matBillboard.billboardMode = R3D_BillboardMode.R3D_BILLBOARD_Y_AXIS;

        // Create transforms for instanced billboards
        R3D_InstanceBuffer instances = R3D_LoadInstanceBuffer(64, R3D_InstanceFlags.R3D_INSTANCE_POSITION | R3D_InstanceFlags.R3D_INSTANCE_SCALE);
        var positions = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        var scales = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_SCALE);
        for (int i = 0; i < 64; i++) {
            float scaleFactor = GetRandomValue(25, 50) / 10.0f;
            scales[i] = new Vector3(scaleFactor, scaleFactor, 1.0f);
            positions[i] = new Vector3(
                GetRandomValue(-100, 100),
                scaleFactor * 0.5f,
                GetRandomValue(-100, 100)
            );
        }
        R3D_UnmapInstances(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION | R3D_InstanceFlags.R3D_INSTANCE_SCALE);

        // Setup directional light with shadows
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_DIR);
        R3D_SetLightDirection(light, new Vector3(-1, -1, -1));
        R3D_SetShadowDepthBias(light, 0.01f);
        R3D_EnableShadow(light);
        R3D_SetLightActive(light, true);
        R3D_SetLightRange(light, 32.0f);

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 5, 0),
            Target = new Vector3(0, 5, -1),
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
                    R3D_DrawMesh(meshGround, matGround, Vector3.Zero, 1.0f);
                    R3D_DrawMeshInstanced(meshBillboard, matBillboard, instances, 64);
                R3D_End();

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadMaterial(matBillboard);
        R3D_UnloadMesh(meshBillboard);
        R3D_UnloadMesh(meshGround);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}