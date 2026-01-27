using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Animation
{
    public static unsafe int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Animation example");
        SetTargetFPS(60);

        // Initialize R3D with FXAA
        R3D_Init(GetScreenWidth(), GetScreenHeight());
        R3D_SetAntiAliasing(R3D_AntiAliasing.R3D_ANTI_ALIASING_FXAA);
        
        R3D_Cubemap cubemap = R3D_LoadCubemap("resources/panorama/indoor.hdr", R3D_CubemapLayout.R3D_CUBEMAP_LAYOUT_AUTO_DETECT);
        R3D_AmbientMap ambientMap = R3D_GenAmbientMap(cubemap, R3D_AmbientFlags.R3D_AMBIENT_ILLUMINATION);
        
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            // Setup environment sky
            env.background.skyBlur = 0.3f;
            env.background.sky = cubemap;
            
            // Setup environment ambient
            env.ambient.map = ambientMap;
            env.ambient.energy = 0.25f;       

            // Setup tonemapping
            env.tonemap.mode = R3D_Tonemap.R3D_TONEMAP_FILMIC;
            env.tonemap.exposure = 0.75f;
        });

        // Generate a ground plane and load the animated model
        R3D_Mesh plane = R3D_GenMeshPlane(10, 10, 1, 1);
        R3D_Model model = R3D_LoadModel("resources/models/CesiumMan.glb");

        // Load animations
        R3D_AnimationLib modelAnims = R3D_LoadAnimationLib("resources/models/CesiumMan.glb");
        R3D_AnimationPlayer modelPlayer = R3D_LoadAnimationPlayer(model.skeleton, modelAnims);

        // Setup animation playing
        R3D_SetAnimationWeight(ref modelPlayer, 0, 1.0f);
        R3D_SetAnimationLoop(ref modelPlayer, 0, true);
        R3D_PlayAnimation(ref modelPlayer, 0);

        // Create model instances
        R3D_InstanceBuffer instances = R3D_LoadInstanceBuffer(4, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        var positions = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        for (int z = 0; z < 2; z++) {
            for (int x = 0; x < 2; x++) {
                positions[z*2 + x] = new Vector3(x - 0.5f, 0, z - 0.5f);
            }
        }
        R3D_UnmapInstances(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);

        // Setup lights with shadows
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_DIR);
        R3D_SetLightDirection(light, new Vector3(-1.0f, -1.0f, -1.0f));
        R3D_SetLightActive(light, true);
        R3D_SetLightRange(light, 10.0f);
        R3D_EnableShadow(light);

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
            R3D_UpdateAnimationPlayer(ref modelPlayer, delta);

            BeginDrawing();
                ClearBackground(Color.RayWhite);
                R3D_Begin(camera);
                    R3D_DrawMesh(plane, R3D_MATERIAL_BASE, Vector3.Zero, 1.0f);
                    R3D_DrawAnimatedModel(model, modelPlayer, Vector3.Zero, 1.25f);
                    R3D_DrawAnimatedModelInstanced(model, modelPlayer, instances, 4);
                R3D_End();
            EndDrawing();
        }

        // Cleanup
        R3D_UnloadAnimationPlayer(modelPlayer);
        R3D_UnloadAnimationLib(modelAnims);
        R3D_UnloadModel(model, true);
        R3D_UnloadMesh(plane);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}