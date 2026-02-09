using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Kinematics
{
    private const float GRAVITY = -15.0f;
    private const float MOVE_SPEED = 5.0f;
    private const float JUMP_FORCE = 8.0f;

    private static Vector3 CapsuleCenter(Capsule caps) => (caps.Start + caps.End) * 0.5f;

    public static unsafe int Main()
    {
        InitWindow(800, 450, "[r3d] - Kinematics Example");
        SetTargetFPS(60);

        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetTextureFilter(TextureFilter.Anisotropic8X);

        var sky = R3D.GenCubemapSky(4096, R3D.CUBEMAP_SKY_BASE);
        var ambient = R3D.GenAmbientMap(sky, AmbientFlags.Illumination | AmbientFlags.Reflection);
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Sky = sky;
            env.Ambient.Map = ambient;
        });

        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(-1, -1, -1));
        R3D.SetLightRange(light, 16.0f);
        R3D.SetLightActive(light, true);
        R3D.EnableShadow(light);
        R3D.SetShadowDepthBias(light, 0.005f);

        // Load materials
        var baseAlbedo = R3D.LoadAlbedoMap("resources/images/placeholder.png", Color.White);

        var groundMat = R3D.GetDefaultMaterial();
        groundMat.UvScale = new Vector2(250.0f, 250.0f);
        groundMat.Albedo = baseAlbedo;

        var slopeMat = R3D.GetDefaultMaterial();
        slopeMat.Albedo.Color = new Color(255, 255, 0, 255);
        slopeMat.Albedo.Texture = baseAlbedo.Texture;

        // Ground
        var groundMesh = R3D.GenMeshPlane(1000, 1000, 1, 1);
        var groundBox = new BoundingBox(new Vector3(-500, -1, -500), new Vector3(500, 0, 500));

        // Slope obstacle
        var slopeMeshData = R3D.GenMeshDataSlope(2, 2, 2, new Vector3(0, 1, -1));
        var slopeMesh = R3D.LoadMesh(PrimitiveType.Triangles, slopeMeshData, null, MeshUsage.StaticMesh);
        var slopeTransform = Matrix4x4.Transpose(Matrix4x4.CreateTranslation(0, 1, 5));

        // Player capsule
        var capsule = new Capsule { Start = new Vector3(0, 0.5f, 0), End = new Vector3(0, 1.5f, 0), Radius = 0.5f };
        var capsMesh = R3D.GenMeshCapsule(0.5f, 1.0f, 64, 32);
        var velocity = Vector3.Zero;

        // Camera
        var cameraAngle = 0.0f;
        var cameraPitch = 30.0f;
        var camera = new Camera3D
        {
            Position = new Vector3(0, 5, 5),
            Target = CapsuleCenter(capsule),
            Up = Vector3.UnitY,
            FovY = 60
        };

        DisableCursor();

        while (!WindowShouldClose())
        {
            float dt = GetFrameTime();

            // Camera rotation
            var mouseDelta = GetMouseDelta();
            cameraAngle -= mouseDelta.X * 0.15f;
            cameraPitch = Math.Clamp(cameraPitch + mouseDelta.Y * 0.15f, -7.5f, 80.0f);

            // Movement input relative to camera
            int dx = (IsKeyDown(KeyboardKey.A) ? 1 : 0) - (IsKeyDown(KeyboardKey.D) ? 1 : 0);
            int dz = (IsKeyDown(KeyboardKey.W) ? 1 : 0) - (IsKeyDown(KeyboardKey.S) ? 1 : 0);

            var moveInput = Vector3.Zero;
            if (dx != 0 || dz != 0)
            {
                float angleRad = cameraAngle * DEG2RAD;
                var right = new Vector3(MathF.Cos(angleRad), 0, -MathF.Sin(angleRad));
                var forward = new Vector3(MathF.Sin(angleRad), 0, MathF.Cos(angleRad));
                moveInput = Vector3.Normalize(right * dx + forward * dz);
            }

            // Check grounded
            var outGround = new RayCollision();
            bool isGrounded = R3D.IsCapsuleGroundedBox(capsule, 0.01f, groundBox, ref outGround) ||
                              R3D.IsCapsuleGroundedMesh(capsule, 0.3f, slopeMeshData, slopeTransform, ref outGround);

            // Jump and apply gravity
            if (isGrounded && IsKeyPressed(KeyboardKey.Space)) velocity.Y = JUMP_FORCE;
            if (!isGrounded) velocity.Y += GRAVITY * dt;
            else if (velocity.Y < 0) velocity.Y = 0;

            // Calculate total movement
            var movement = moveInput * MOVE_SPEED * dt;
            movement.Y = velocity.Y * dt;

            // Apply movement with collision
            var outNormal = Vector3.Zero;
            movement = R3D.SlideCapsuleMesh(capsule, movement, slopeMeshData, slopeTransform, ref outNormal);
            capsule.Start += movement;
            capsule.End += movement;

            // Ground clamp
            if (capsule.Start.Y < 0.5f)
            {
                float correction = 0.5f - capsule.Start.Y;
                capsule.Start = capsule.Start with { Y = capsule.Start.Y + correction };
                capsule.End = capsule.End with { Y = capsule.End.Y + correction };
                velocity.Y = 0;
            }

            // Update camera position
            var target = CapsuleCenter(capsule);
            float pitchRad = cameraPitch * DEG2RAD;
            float camAngleRad = cameraAngle * DEG2RAD;
            camera.Position = new Vector3(
                target.X - MathF.Sin(camAngleRad) * MathF.Cos(pitchRad) * 5.0f,
                target.Y + MathF.Sin(pitchRad) * 5.0f,
                target.Z - MathF.Cos(camAngleRad) * MathF.Cos(pitchRad) * 5.0f
            );
            camera.Target = target;

            BeginDrawing();
                ClearBackground(Color.Black);
                R3D.Begin(camera);
                    R3D.DrawMeshPro(slopeMesh, slopeMat, slopeTransform);
                    R3D.DrawMesh(groundMesh, groundMat, Vector3.Zero, 1.0f);
                    R3D.DrawMesh(capsMesh, R3D.MATERIAL_BASE, CapsuleCenter(capsule), 1.0f);
                R3D.End();
                DrawFPS(10, 10);
                DrawText(isGrounded ? "GROUNDED" : "AIRBORNE", 10, GetScreenHeight() - 30, 20, isGrounded ? Color.Lime : Color.Yellow);
            EndDrawing();
        }

        R3D.UnloadMeshData(slopeMeshData);
        R3D.UnloadMesh(groundMesh);
        R3D.UnloadMesh(slopeMesh);
        R3D.UnloadMesh(capsMesh);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
