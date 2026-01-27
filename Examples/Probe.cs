using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Probe
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Probe example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());

        R3D_Cubemap cubemap = R3D_LoadCubemap("resources/panorama/indoor.hdr", R3D_CubemapLayout.R3D_CUBEMAP_LAYOUT_AUTO_DETECT);
        
        R3D_AmbientMap ambientMap = R3D_GenAmbientMap(cubemap, R3D_AmbientFlags.R3D_AMBIENT_ILLUMINATION | R3D_AmbientFlags.R3D_AMBIENT_REFLECTION);
        
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            // Setup environment sky
            env.background.skyBlur = 0.3f;
            env.background.energy = 0.6f;
            env.background.sky = cubemap;
            
            // Setup environment ambient
            env.ambient.map = ambientMap;
            env.ambient.energy = 0.25f;
            
            // Setup tonemapping
            env.tonemap.mode = R3D_Tonemap.R3D_TONEMAP_FILMIC;
        });

        // Create meshes
        R3D_Mesh plane = R3D_GenMeshPlane(30, 30, 1, 1);
        R3D_Mesh sphere = R3D_GenMeshSphere(0.5f, 64, 64);
        R3D_Material material = R3D_GetDefaultMaterial();

        // Create light
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_SPOT);
        R3D_LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D_SetLightActive(light, true);
        R3D_EnableShadow(light);

        // Create probe
        R3D_Probe probe = R3D_CreateProbe(R3D_ProbeFlags.R3D_PROBE_ILLUMINATION | R3D_ProbeFlags.R3D_PROBE_REFLECTION);
        R3D_SetProbePosition(probe, new Vector3(0, 1, 0));
        R3D_SetProbeShadows(probe, true);
        R3D_SetProbeFalloff(probe, 0.5f);
        R3D_SetProbeActive(probe, true);

        // Setup camera
        Camera3D camera = new Camera3D() {
            Position = new Vector3(0, 3.0f, 6.0f),
            Target = new Vector3(0, 0.5f, 0),
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

                    material.orm.roughness = 0.5f;
                    material.orm.metalness = 0.0f;
                    R3D_DrawMesh(plane, material, Vector3.Zero, 1.0f);

                    for (int i = -1; i <= 1; i++) {
                        material.orm.roughness = MathF.Abs(i) * 0.4f;
                        material.orm.metalness = 1.0f - MathF.Abs(i);
                        R3D_DrawMesh(sphere, material, new Vector3(i * 3.0f, 1.0f, 0), 2.0f);
                    }

                R3D_End();
                
                DrawFPS(10, 10);

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadAmbientMap(ambientMap);
        R3D_UnloadCubemap(cubemap);
        R3D_UnloadMesh(sphere);
        R3D_UnloadMesh(plane);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}