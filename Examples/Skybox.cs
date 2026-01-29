using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using CubemapLayout = R3D_cs.CubemapLayout;

namespace Examples;

public static class Skybox
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Skybox example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create sphere mesh
        var sphere = R3D.GenMeshSphere(0.5f, 32, 64);

        // Define procedural skybox parameters
        var skyParams = R3D.CUBEMAP_SKY_BASE;
        skyParams.GroundEnergy = 2.0f;
        skyParams.SkyEnergy = 2.0f;
        skyParams.SunEnergy = 2.0f;

        // Load and generate skyboxes
        var skyProcedural = R3D.GenCubemapSky(512, skyParams);
        var skyPanorama = R3D.LoadCubemap("resources/panorama/sky.hdr", CubemapLayout.AutoDetect);

        // Generate ambient maps
        var ambientProcedural = R3D.GenAmbientMap(skyProcedural, AmbientFlags.Illumination | AmbientFlags.Reflection);
        var ambientPanorama = R3D.GenAmbientMap(skyPanorama, AmbientFlags.Illumination | AmbientFlags.Reflection);

        R3D.SetEnvironmentEx((ref env) =>
        {
            // Set default sky/ambient maps
            env.Background.Sky = skyPanorama;
            env.Ambient.Map = ambientPanorama;

            // Set tonemapping
            env.Tonemap.Mode = Tonemap.Agx;
        });

        // Setup camera
        var camera = new Camera3D
        {
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

            if (IsMouseButtonPressed(MouseButton.Left))
                R3D.SetEnvironmentEx((ref env) =>
                {
                    if (env.Background.Sky.Texture == skyPanorama.Texture)
                    {
                        env.Background.Sky = skyProcedural;
                        env.Ambient.Map = ambientProcedural;
                    }
                    else
                    {
                        env.Background.Sky = skyPanorama;
                        env.Ambient.Map = ambientPanorama;
                    }
                });

            // Draw sphere grid
            R3D.Begin(camera);
                for (int x = 0; x <= 8; x++) {
                    for (int y = 0; y <= 8; y++) {
                        var material = R3D.MATERIAL_BASE;
                        material.Orm.Roughness = Raymath.Remap(y, 0.0f, 8.0f, 0.0f, 1.0f);
                        material.Orm.Metalness = Raymath.Remap(x, 0.0f, 8.0f, 0.0f, 1.0f);
                        R3D.DrawMesh(sphere, material, new Vector3((x - 4) * 1.25f, (y - 4f) * 1.25f, 0.0f), 1.0f);
                    }
                }
            R3D.End();

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadAmbientMap(ambientProcedural);
        R3D.UnloadAmbientMap(ambientPanorama);
        R3D.UnloadCubemap(skyProcedural);
        R3D.UnloadCubemap(skyPanorama);
        R3D.UnloadMesh(sphere);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
