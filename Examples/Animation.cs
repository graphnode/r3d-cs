using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Animation
{
    public static unsafe int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Animation example");
        SetTargetFPS(60);

        // Initialize R3D with FXAA
        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetAntiAliasing(AntiAliasing.Fxaa);
        
        var cubemap = R3D.LoadCubemap("resources/panorama/indoor.hdr", R3D_cs.CubemapLayout.AutoDetect);
        var ambientMap = R3D.GenAmbientMap(cubemap, AmbientFlags.Illumination);
        
        R3D.SetEnvironmentEx((ref env) =>
        {
            // Setup environment sky
            env.Background.SkyBlur = 0.3f;
            env.Background.Sky = cubemap;
            
            // Setup environment ambient
            env.Ambient.Map = ambientMap;
            env.Ambient.Energy = 0.25f;       

            // Setup tonemapping
            env.Tonemap.Mode = Tonemap.Filmic;
            env.Tonemap.Exposure = 0.75f;
        });

        // Generate a ground plane and load the animated model
        var plane = R3D.GenMeshPlane(10, 10, 1, 1);
        var model = R3D.LoadModel("resources/models/CesiumMan.glb");

        // Load animations
        var modelAnims = R3D.LoadAnimationLib("resources/models/CesiumMan.glb");
        var modelPlayer = R3D.LoadAnimationPlayer(model.Skeleton, modelAnims);

        // Setup animation playing
        R3D.SetAnimationWeight(ref modelPlayer, 0, 1.0f);
        R3D.SetAnimationLoop(ref modelPlayer, 0, true);
        R3D.PlayAnimation(ref modelPlayer, 0);

        // Create model instances
        var instances = R3D.LoadInstanceBuffer(4, InstanceFlags.Position);
        var positions = R3D.MapInstances<Vector3>(instances, InstanceFlags.Position);
        for (int z = 0; z < 2; z++) {
            for (int x = 0; x < 2; x++) {
                positions[z*2 + x] = new Vector3(x - 0.5f, 0, z - 0.5f);
            }
        }
        R3D.UnmapInstances(instances, InstanceFlags.Position);

        // Setup lights with shadows
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(-1.0f, -1.0f, -1.0f));
        R3D.SetLightActive(light, true);
        R3D.SetLightRange(light, 10.0f);
        R3D.EnableShadow(light);

        // Setup camera
        Camera3D camera = new Camera3D {
            Position = new Vector3(0, 1.5f, 3.0f),
            Target = new Vector3(0, 0.75f, 0.0f),
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Main loop
        while (!WindowShouldClose())
        {
            float delta = GetFrameTime();

            UpdateCamera(ref camera, CameraMode.Orbital);
            R3D.UpdateAnimationPlayer(ref modelPlayer, delta);

            BeginDrawing();
                ClearBackground(Color.RayWhite);
                R3D.Begin(camera);
                    R3D.DrawMesh(plane, R3D.MATERIAL_BASE, Vector3.Zero, 1.0f);
                    R3D.DrawAnimatedModel(model, modelPlayer, Vector3.Zero, 1.25f);
                    R3D.DrawAnimatedModelInstanced(model, modelPlayer, instances, 4);
                R3D.End();
            EndDrawing();
        }

        // Cleanup
        R3D.UnloadAnimationPlayer(modelPlayer);
        R3D.UnloadAnimationLib(modelAnims);
        R3D.UnloadModel(model, true);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}