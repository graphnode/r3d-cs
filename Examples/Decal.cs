using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Decal
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Decal example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create meshes
        var plane = R3D.GenMeshPlane(5.0f, 5.0f, 1, 1);
        var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
        var cylinder = R3D.GenMeshCylinder(0.5f, 0.5f, 1, 64);
        var material = R3D.GetDefaultMaterial();
        material.Albedo.Color = Color.Gray;

        // Create decal
        var decal = R3D.DECAL_BASE;
        R3D.SetTextureFilter(TextureFilter.Bilinear);
        decal.Albedo = R3D.LoadAlbedoMap("resources/images/decal.png", Color.White);
        decal.Normal = R3D.LoadNormalMap("resources/images/decal_normal.png", 1.0f);
        decal.NormalThreshold = 45.0f;
        decal.FadeWidth = 20.0f;

        // Create data for instanced drawing
        var instances = R3D.LoadInstanceBuffer(3, InstanceFlags.Position);
        var positions = R3D.MapInstances<Vector3>(instances, InstanceFlags.Position);
        positions[0] = new Vector3(-1.25f, 0, 1);
        positions[1] = new Vector3(0, 0, 1);
        positions[2] = new Vector3(1.25f, 0, 1);
        R3D.UnmapInstances(instances, InstanceFlags.Position);

        // Setup environment
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(10, 10, 10, 255));

        // Create light
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(0.5f, -1, -0.5f));
        R3D.SetShadowDepthBias(light, 0.005f);
        R3D.EnableShadow(light);
        R3D.SetLightActive(light, true);

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 3, 3),
            Target = new Vector3(0, 0, 0),
            Up = new Vector3(0, 1, 0),
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

                R3D.Begin(camera);
                    R3D.DrawMesh(plane, material, Vector3.Zero, 1.0f);
                    R3D.DrawMesh(sphere, material, new Vector3(-1, 0.5f, -1), 1.0f);
                    R3D.DrawMeshEx(cylinder, material, new Vector3(1, 0.5f, -1), Quaternion.CreateFromYawPitchRoll(0, 0, MathF.PI/2), Vector3.One);

                    R3D.DrawDecal(decal, new Vector3(-1, 1, -1), 1.0f);
                    R3D.DrawDecalEx(decal, new Vector3(1, 0.5f, -0.5f), Quaternion.CreateFromYawPitchRoll(0, MathF.PI/2, 0), new Vector3(1.25f, 1.25f, 1.25f));
                    R3D.DrawDecalInstanced(decal, instances, 3);
                R3D.End();

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMesh(plane);
        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(cylinder);
        R3D.UnloadMaterial(material);
        R3D.UnloadDecalMaps(decal);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
