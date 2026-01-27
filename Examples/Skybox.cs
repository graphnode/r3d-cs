using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Skybox
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Skybox example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());

        // Create sphere mesh
        R3D_Mesh sphere = R3D_GenMeshSphere(0.5f, 32, 64);

        // Define procedural skybox parameters
        R3D_CubemapSky skyParams = R3D_CUBEMAP_SKY_BASE;
        skyParams.groundEnergy = 2.0f;
        skyParams.skyEnergy = 2.0f;
        skyParams.sunEnergy = 2.0f;

        // Load and generate skyboxes
        R3D_Cubemap skyProcedural = R3D_GenCubemapSky(512, skyParams);
        R3D_Cubemap skyPanorama = R3D_LoadCubemap("resources/panorama/sky.hdr", R3D_CubemapLayout.R3D_CUBEMAP_LAYOUT_AUTO_DETECT);

        // Generate ambient maps
        R3D_AmbientMap ambientProcedural = R3D_GenAmbientMap(skyProcedural, R3D_AmbientFlags.R3D_AMBIENT_ILLUMINATION | R3D_AmbientFlags.R3D_AMBIENT_REFLECTION);
        R3D_AmbientMap ambientPanorama = R3D_GenAmbientMap(skyPanorama, R3D_AmbientFlags.R3D_AMBIENT_ILLUMINATION | R3D_AmbientFlags.R3D_AMBIENT_REFLECTION);
        
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            // Set default sky/ambient maps
            env.background.sky = skyPanorama;
            env.ambient.map = ambientPanorama;
            
            // Set tonemapping
            env.tonemap.mode = R3D_Tonemap.R3D_TONEMAP_AGX;
        });

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 0, 10),
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

            if (IsMouseButtonPressed(MouseButton.Left)) {
                if (R3D_ENVIRONMENT_GET.background.sky.texture == skyPanorama.texture) {
                    R3D_ENVIRONMENT_SET((ref env) =>
                    {
                        env.background.sky = skyProcedural;
                        env.ambient.map = ambientProcedural;
                    });
                }
                else {
                    R3D_ENVIRONMENT_SET((ref env) =>
                    {
                        env.background.sky = skyPanorama;
                        env.ambient.map = ambientPanorama;
                    });
                }
            }

            // Draw sphere grid
            R3D_Begin(camera);
                for (int x = 0; x <= 8; x++) {
                    for (int y = 0; y <= 8; y++) {
                        R3D_Material material = R3D_MATERIAL_BASE;
                        material.orm.roughness = Raymath.Remap(y, 0.0f, 8.0f, 0.0f, 1.0f);
                        material.orm.metalness = Raymath.Remap(x, 0.0f, 8.0f, 0.0f, 1.0f);
                        R3D_DrawMesh(sphere, material, new Vector3((x - 4) * 1.25f, (y - 4f) * 1.25f, 0.0f), 1.0f);
                    }
                }
            R3D_End();

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadAmbientMap(ambientProcedural);
        R3D_UnloadAmbientMap(ambientPanorama);
        R3D_UnloadCubemap(skyProcedural);
        R3D_UnloadCubemap(skyPanorama);
        R3D_UnloadMesh(sphere);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}