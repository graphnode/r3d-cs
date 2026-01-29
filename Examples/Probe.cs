using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using CubemapLayout = R3D_cs.CubemapLayout;

namespace Examples;

public static class Probe
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Probe example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        var cubemap = R3D.LoadCubemap("resources/panorama/indoor.hdr", CubemapLayout.AutoDetect);

        var ambientMap = R3D.GenAmbientMap(cubemap, AmbientFlags.Illumination | AmbientFlags.Reflection);

        R3D.SetEnvironmentEx((ref env) =>
        {
            // Setup environment sky
            env.Background.SkyBlur = 0.3f;
            env.Background.Energy = 0.6f;
            env.Background.Sky = cubemap;

            // Setup environment ambient
            env.Ambient.Map = ambientMap;
            env.Ambient.Energy = 0.25f;

            // Setup tonemapping
            env.Tonemap.Mode = Tonemap.Filmic;
        });

        // Create meshes
        var plane = R3D.GenMeshPlane(30, 30, 1, 1);
        var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
        var material = R3D.GetDefaultMaterial();

        // Create light
        var light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D.SetLightActive(light, true);
        R3D.EnableShadow(light);

        // Create probe
        var probe = R3D.CreateProbe(ProbeFlags.Illumination | ProbeFlags.Reflection);
        R3D.SetProbePosition(probe, new Vector3(0, 1, 0));
        R3D.SetProbeShadows(probe, true);
        R3D.SetProbeFalloff(probe, 0.5f);
        R3D.SetProbeActive(probe, true);

        // Setup camera
        var camera = new Camera3D
        {
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

                R3D.Begin(camera);

                    material.Orm.Roughness = 0.5f;
                    material.Orm.Metalness = 0.0f;
                    R3D.DrawMesh(plane, material, Vector3.Zero, 1.0f);

                    for (int i = -1; i <= 1; i++) {
                        material.Orm.Roughness = MathF.Abs(i) * 0.4f;
                        material.Orm.Metalness = 1.0f - MathF.Abs(i);   
                        R3D.DrawMesh(sphere, material, new Vector3(i * 3.0f, 1.0f, 0), 2.0f);
                    }

                R3D.End();
                
                DrawFPS(10, 10);

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadAmbientMap(ambientMap);
        R3D.UnloadCubemap(cubemap);
        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
