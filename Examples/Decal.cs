using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Decal
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Decal example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());

        // Create meshes
        R3D_Mesh plane = R3D_GenMeshPlane(5.0f, 5.0f, 1, 1);
        R3D_Mesh sphere = R3D_GenMeshSphere(0.5f, 64, 64);
        R3D_Mesh cylinder = R3D_GenMeshCylinder(0.5f, 0.5f, 1, 64);
        R3D_Material material = R3D_GetDefaultMaterial();
        material.albedo.color = Color.Gray;

        // Create decal
        R3D_Decal decal = R3D_DECAL_BASE;
        R3D_SetTextureFilter(TextureFilter.Bilinear);
        decal.albedo = R3D_LoadAlbedoMap("resources/images/decal.png", Color.White);
        decal.normal = R3D_LoadNormalMap("resources/images/decal_normal.png", 1.0f);
        decal.normalThreshold = 45.0f;
        decal.fadeWidth = 20.0f;

        // Create data for instanced drawing
        R3D_InstanceBuffer instances = R3D_LoadInstanceBuffer(3, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        var positions = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        positions[0] = new Vector3(-1.25f, 0, 1);
        positions[1] = new Vector3(0, 0, 1);
        positions[2] = new Vector3(1.25f, 0, 1);
        R3D_UnmapInstances(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);

        // Setup environment
        R3D_ENVIRONMENT_SET((ref env) => env.ambient.color = new Color(10, 10, 10, 255));

        // Create light
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_DIR);
        R3D_SetLightDirection(light, new Vector3(0.5f, -1, -0.5f));
        R3D_SetShadowDepthBias(light, 0.005f);
        R3D_EnableShadow(light);
        R3D_SetLightActive(light, true);

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 3, 3),
            Target = new Vector3(0, 0, 0),
            Up = new Vector3(0, 1, 0),
            FovY = 60,
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
                    R3D_DrawMesh(plane, material, Vector3.Zero, 1.0f);
                    R3D_DrawMesh(sphere, material, new Vector3(-1, 0.5f, -1), 1.0f);
                    R3D_DrawMeshEx(cylinder, material, new Vector3(1, 0.5f, -1), Quaternion.CreateFromYawPitchRoll(0, 0,MathF.PI/2), Vector3.One);
                 
                    R3D_DrawDecal(decal, new Vector3(-1, 1, -1), 1.0f);
                    R3D_DrawDecalEx(decal, new Vector3(1, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(0, MathF.PI/2, 0), new Vector3(1.25f, 1.25f, 1.25f));
                    R3D_DrawDecalInstanced(decal, instances, 3);
                R3D_End();

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadMesh(plane);
        R3D_UnloadMesh(sphere);
        R3D_UnloadMesh(cylinder);
        R3D_UnloadMaterial(material);
        R3D_UnloadDecalMaps(decal);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}