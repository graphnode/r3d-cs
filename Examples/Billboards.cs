using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Billboards
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Billboards example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetTextureFilter(TextureFilter.Point);

        // Set background/ambient color
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = new Color(102, 191, 255, 255);
            env.Ambient.Color = new Color(10, 19, 25, 255);
            env.Tonemap.Mode = Tonemap.Filmic;
        });

        // Create ground mesh and material
        var meshGround = R3D.GenMeshPlane(200, 200, 1, 1);
        var matGround = R3D.GetDefaultMaterial();
        matGround.Albedo.Color = Color.Green;

        // Create billboard mesh and material
        var meshBillboard = R3D.GenMeshQuad(1.0f, 1.0f, 1, 1, new Vector3(0.0f, 0.0f, 1.0f));
        meshBillboard.ShadowCastMode = ShadowCastMode.OnDoubleSided;

        var matBillboard = R3D.GetDefaultMaterial();
        matBillboard.Albedo = R3D.LoadAlbedoMap("resources/images/tree.png", Color.White);
        matBillboard.BillboardMode = BillboardMode.YAxis;

        // Create transforms for instanced billboards
        var instances = R3D.LoadInstanceBuffer(64, InstanceFlags.Position | InstanceFlags.Scale);
        var positions = R3D.MapInstances<Vector3>(instances, InstanceFlags.Position);
        var scales = R3D.MapInstances<Vector3>(instances, InstanceFlags.Scale);
        for (var i = 0; i < 64; i++)
        {
            float scaleFactor = GetRandomValue(25, 50) / 10.0f;
            scales[i] = new Vector3(scaleFactor, scaleFactor, 1.0f);
            positions[i] = new Vector3(
                GetRandomValue(-100, 100),
                scaleFactor * 0.5f,
                GetRandomValue(-100, 100)
            );
        }

        R3D.UnmapInstances(instances, InstanceFlags.Position | InstanceFlags.Scale);

        // Setup directional light with shadows
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(-1, -1, -1));
        R3D.SetShadowDepthBias(light, 0.01f);
        R3D.EnableShadow(light);
        R3D.SetLightActive(light, true);
        R3D.SetLightRange(light, 32.0f);

        // Setup camera
        var camera = new Camera3D
        {
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

                R3D.Begin(camera);
                    R3D.DrawMesh(meshGround, matGround, Vector3.Zero, 1.0f);
                    R3D.DrawMeshInstanced(meshBillboard, matBillboard, instances, 64);
                R3D.End();

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMaterial(matBillboard);
        R3D.UnloadMesh(meshBillboard);
        R3D.UnloadMesh(meshGround);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
