using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Basic
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Basic example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());

        // Create meshes
        R3D_Mesh plane = R3D_GenMeshPlane(1000, 1000, 1, 1);
        R3D_Mesh sphere = R3D_GenMeshSphere(0.5f, 64, 64);
        R3D_Material material = R3D_GetDefaultMaterial();

        // Setup environment
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            env.ambient.color = new Color(10, 10, 10, 255);
        });

        // Create light
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_SPOT);
        R3D_LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D_EnableShadow(light);
        R3D_SetLightActive(light, true);

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

                R3D_Begin(camera);
                    R3D_DrawMesh(plane, material, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D_DrawMesh(sphere, material, Vector3.Zero, 1.0f);
                R3D_End();

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadMesh(sphere);
        R3D_UnloadMesh(plane);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}