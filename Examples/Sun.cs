using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Sun
{
    private const int X_INSTANCES = 50;
    private const int Y_INSTANCES = 50;
    private const int INSTANCE_COUNT = X_INSTANCES * Y_INSTANCES;

    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Sun example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetAntiAliasing(AntiAliasing.Fxaa);

        // Create meshes and material
        var plane = R3D.GenMeshPlane(1000, 1000, 1, 1);
        var sphere = R3D.GenMeshSphere(0.35f, 16, 32);
        var material = R3D.GetDefaultMaterial();

        // Create transforms for instanced spheres
        var instances = R3D.LoadInstanceBuffer(INSTANCE_COUNT, InstanceFlags.Position);
        var positions = R3D.MapInstances<Vector3>(instances, InstanceFlags.Position);
        var spacing = 1.5f;
        float offsetX = X_INSTANCES * spacing / 2.0f;
        float offsetZ = Y_INSTANCES * spacing / 2.0f;
        var idx = 0;
        for (var x = 0; x < X_INSTANCES; x++)
        for (var y = 0; y < Y_INSTANCES; y++)
        {
            positions[idx] = new Vector3(x * spacing - offsetX, 0, y * spacing - offsetZ);
            idx++;
        }

        R3D.UnmapInstances(instances, InstanceFlags.Position);

        // Setup environment
        var skybox = R3D.GenCubemapSky(1024, R3D.CUBEMAP_SKY_BASE);
        var ambientMap = R3D.GenAmbientMap(skybox, AmbientFlags.Illumination | AmbientFlags.Reflection);
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Sky = skybox;
            env.Ambient.Map = ambientMap;
        });

        // Create directional light with shadows
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(-1, -1, -1));
        R3D.SetLightActive(light, true);
        R3D.SetLightRange(light, 16.0f);
        R3D.SetShadowSoftness(light, 2.0f);
        R3D.SetShadowDepthBias(light, 0.01f);
        R3D.EnableShadow(light);

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 1, 0),
            Target = new Vector3(1, 1.25f, 1),
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
                    R3D.DrawMesh(plane, material, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D.DrawMeshInstanced(sphere, material, instances, INSTANCE_COUNT);
                R3D.End();
            EndDrawing();
        }

        // Cleanup
        R3D.UnloadInstanceBuffer(instances);
        R3D.UnloadMaterial(material);
        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
