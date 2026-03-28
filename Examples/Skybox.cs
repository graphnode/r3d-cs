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
        var skyParams = R3D.PROCEDURAL_SKY_BASE;
        skyParams.GroundEnergy = 2.0f;
        skyParams.SkyEnergy = 2.0f;
        skyParams.SunEnergy = 2.0f;

        // Load a custom sky shader
        var skyShader = R3D.LoadSkyShader("resources/shaders/sky.glsl");
        var color = new Vector3(0.0f, 0.5f, 0.0f);
        R3D.SetSkyShaderUniform(skyShader, "u_color", ref color);
        var cells = (X: 10, Y: 10);
        R3D.SetSkyShaderUniform(skyShader, "u_cells", ref cells);
        var linePx = 1.0f;
        R3D.SetSkyShaderUniform(skyShader, "u_line_px", ref linePx);

        // Load and generate skyboxes
        var skyPanorama = R3D.LoadCubemap("resources/panorama/sky.hdr", CubemapLayout.AutoDetect);
        var skyProcedural = R3D.GenProceduralSky(1024, skyParams);
        var skyCustom = R3D.GenCustomSky(512, skyShader);

        // Generate ambient maps
        var ambientPanorama = R3D.GenAmbientMap(skyPanorama, AmbientFlags.Illumination | AmbientFlags.Reflection);
        var ambientProcedural = R3D.GenAmbientMap(skyProcedural, AmbientFlags.Illumination | AmbientFlags.Reflection);
        var ambientCustom = R3D.GenAmbientMap(skyCustom, AmbientFlags.Illumination | AmbientFlags.Reflection);

        // Store skies/ambients for cycling
        var skies = new[] { skyPanorama, skyProcedural, skyCustom };
        var ambients = new[] { ambientPanorama, ambientProcedural, ambientCustom };
        var currentSky = 0;

        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Sky = skyPanorama;
            env.Background.Energy = 1.0f;
            env.Ambient.Map = ambientPanorama;
            env.Ambient.Energy = 1.0f;
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

            int dir = (IsMouseButtonPressed(MouseButton.Right) ? 1 : 0) - (IsMouseButtonPressed(MouseButton.Left) ? 1 : 0);
            if (dir != 0)
            {
                currentSky = (currentSky + dir + 3) % 3;
                R3D.SetEnvironmentEx((ref env) =>
                {
                    env.Background.Sky = skies[currentSky];
                    env.Ambient.Map = ambients[currentSky];
                });
            }

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
        R3D.UnloadAmbientMap(ambientCustom);
        R3D.UnloadAmbientMap(ambientProcedural);
        R3D.UnloadAmbientMap(ambientPanorama);
        R3D.UnloadCubemap(skyCustom);
        R3D.UnloadCubemap(skyProcedural);
        R3D.UnloadCubemap(skyPanorama);
        R3D.UnloadSkyShader(skyShader);
        R3D.UnloadMesh(sphere);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
