using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using CubemapLayout = R3D_cs.CubemapLayout;

namespace Examples;

public static class AnimTree
{
    public static int Main()
    {
        // Initialize the window
        InitWindow(800, 450, "[r3d] - Animation tree example");
        SetTargetFPS(60);

        // Initialize R3D with FXAA
        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetAntiAliasingMode(AntiAliasingMode.Fxaa);

        var cubemap = R3D.LoadCubemap("resources/panorama/indoor.hdr", CubemapLayout.AutoDetect);
        var ambientMap = R3D.GenAmbientMap(cubemap, AmbientFlags.Illumination);

        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.SkyBlur = 0.3f;
            env.Background.Energy = 0.6f;
            env.Background.Sky = cubemap;
            env.Ambient.Map = ambientMap;
            env.Ambient.Energy = 0.25f;
            env.Tonemap.Mode = Tonemap.Filmic;
            env.Tonemap.Exposure = 0.75f;
        });

        // Generate a ground plane and load the animated model
        var plane = R3D.GenMeshPlane(10, 10, 1, 1);
        var model = R3D.LoadModel("resources/models/YBot.glb");

        // Load animations
        var modelAnims = R3D.LoadAnimationLib("resources/models/YBot.glb");
        var modelPlayer = R3D.LoadAnimationPlayer(model.Skeleton, modelAnims);

        // Create & define animation tree structure
        var animTree = R3D.LoadAnimationTreeEx(modelPlayer, 12, 0);

        var animState = new AnimationState
        {
            Speed = 0.8f,
            Play = true,
            Loop = true
        };
        var edgeParams = new StmEdgeParams
        {
            Mode = StmEdgeMode.Ondone,
            Status = StmEdgeStatus.Auto,
            XFadeTime = 0.0f
        };
        var fadedEdgeParams = new StmEdgeParams
        {
            Mode = StmEdgeMode.Ondone,
            Status = StmEdgeStatus.Auto,
            XFadeTime = 0.3f
        };
        // Left-Right state machine
        var leftRightStmNode = R3D.CreateStmNode(ref animTree, 4, 4);
        {
            var walkLeft = new AnimationNodeParams { Name = "walk left", State = animState, Looper = true };
            var animNode0 = R3D.CreateAnimationNode(ref animTree, walkLeft);
            var animNode1 = R3D.CreateAnimationNode(ref animTree, walkLeft);

            var walkRight = new AnimationNodeParams { Name = "walk right", State = animState, Looper = true };
            var animNode2 = R3D.CreateAnimationNode(ref animTree, walkRight);
            var animNode3 = R3D.CreateAnimationNode(ref animTree, walkRight);

            var s0 = R3D.CreateStmNodeState(leftRightStmNode, animNode0, 1);
            var s1 = R3D.CreateStmNodeState(leftRightStmNode, animNode1, 1);
            var s2 = R3D.CreateStmNodeState(leftRightStmNode, animNode2, 1);
            var s3 = R3D.CreateStmNodeState(leftRightStmNode, animNode3, 1);
            R3D.CreateStmNodeEdge(leftRightStmNode, s0, s1, edgeParams);
            R3D.CreateStmNodeEdge(leftRightStmNode, s1, s2, fadedEdgeParams);
            R3D.CreateStmNodeEdge(leftRightStmNode, s2, s3, edgeParams);
            R3D.CreateStmNodeEdge(leftRightStmNode, s3, s0, fadedEdgeParams);
        }

        // Forward-Backward state machine
        var forwBackStmNode = R3D.CreateStmNode(ref animTree, 4, 4);
        {
            var walkFwd = new AnimationNodeParams { Name = "walk forward", State = animState, Looper = true };
            var animNode0 = R3D.CreateAnimationNode(ref animTree, walkFwd);
            var animNode1 = R3D.CreateAnimationNode(ref animTree, walkFwd);

            var walkBack = new AnimationNodeParams { Name = "walk backward", State = animState, Looper = true };
            var animNode2 = R3D.CreateAnimationNode(ref animTree, walkBack);
            var animNode3 = R3D.CreateAnimationNode(ref animTree, walkBack);

            var s0 = R3D.CreateStmNodeState(forwBackStmNode, animNode0, 1);
            var s1 = R3D.CreateStmNodeState(forwBackStmNode, animNode1, 1);
            var s2 = R3D.CreateStmNodeState(forwBackStmNode, animNode2, 1);
            var s3 = R3D.CreateStmNodeState(forwBackStmNode, animNode3, 1);
            R3D.CreateStmNodeEdge(forwBackStmNode, s0, s1, edgeParams);
            R3D.CreateStmNodeEdge(forwBackStmNode, s1, s2, fadedEdgeParams);
            R3D.CreateStmNodeEdge(forwBackStmNode, s2, s3, edgeParams);
            R3D.CreateStmNodeEdge(forwBackStmNode, s3, s0, fadedEdgeParams);
        }

        // Switch node: idle / left-right / forward-backward
        var switchParams = new SwitchNodeParams
        {
            Synced = false,
            ActiveInput = 0,
            XFadeTime = 0.4f
        };
        var switchNode = R3D.CreateSwitchNode(ref animTree, 3, switchParams);

        var idleNode = R3D.CreateAnimationNode(ref animTree, new AnimationNodeParams { Name = "idle", State = animState });

        R3D.AddAnimationNode(switchNode, idleNode, 0);
        R3D.AddAnimationNode(switchNode, leftRightStmNode, 1);
        R3D.AddAnimationNode(switchNode, forwBackStmNode, 2);
        R3D.AddRootAnimationNode(ref animTree, switchNode);

        // Setup lights with shadows
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(-1.0f, -1.0f, -1.0f));
        R3D.SetLightActive(light, true);
        R3D.SetLightRange(light, 10.0f);
        R3D.EnableShadow(light);

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 1.5f, 3.0f),
            Target = new Vector3(0, 0.75f, 0.0f),
            Up = Vector3.UnitY,
            FovY = 60
        };

        // Main loop
        while (!WindowShouldClose())
        {
            float delta = GetFrameTime();

            if (IsKeyDown(KeyboardKey.One))   switchParams.ActiveInput = 0;
            if (IsKeyDown(KeyboardKey.Two))   switchParams.ActiveInput = 1;
            if (IsKeyDown(KeyboardKey.Three)) switchParams.ActiveInput = 2;
            R3D.SetSwitchNodeParams(switchNode, switchParams);

            UpdateCamera(ref camera, CameraMode.Orbital);
            R3D.UpdateAnimationTree(ref animTree, delta);

            BeginDrawing();
                ClearBackground(Color.RayWhite);
                R3D.Begin(camera);
                    R3D.DrawMesh(plane, R3D.MATERIAL_BASE, Vector3.Zero, 1.0f);
                    R3D.DrawAnimatedModel(model, modelPlayer, Vector3.Zero, 1.0f);
                R3D.End();
                DrawText("Press '1' to idle",                      10, GetScreenHeight() - 74, 20, Color.Black);
                DrawText("Press '2' to walk left and right",       10, GetScreenHeight() - 54, 20, Color.Black);
                DrawText("Press '3' to walk forward and backward", 10, GetScreenHeight() - 34, 20, Color.Black);
            EndDrawing();
        }

        // Cleanup
        R3D.UnloadAnimationTree(animTree);
        R3D.UnloadAnimationPlayer(modelPlayer);
        R3D.UnloadAnimationLib(modelAnims);
        R3D.UnloadModel(model, true);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
