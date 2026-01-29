using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Sprite
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Sprite example");
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

        // Create sprite mesh and material
        var meshSprite = R3D.GenMeshQuad(1.0f, 1.0f, 1, 1, Vector3.UnitZ);
        meshSprite.ShadowCastMode = ShadowCastMode.OnDoubleSided;

        var matSprite = R3D.GetDefaultMaterial();
        matSprite.Albedo = R3D.LoadAlbedoMap("resources/images/spritesheet.png", Color.White);
        matSprite.BillboardMode = BillboardMode.YAxis;

        // Setup spotlight
        var light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(0, 10, 10), Vector3.Zero);
        R3D.SetLightRange(light, 64.0f);
        R3D.EnableShadow(light);
        R3D.SetLightActive(light, true);

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 2, 5),
            Target = new Vector3(0, 0.5f, 0),
            Up = Vector3.UnitY,
            FovY = 45
        };

        // Bird data
        var birdPos = new Vector3(0, 0.5f, 0);

        // Main loop
        while (!WindowShouldClose())
        {
            // Update bird position
            var birdPrev = birdPos;
            birdPos.X = 2.0f * MathF.Sin((float)GetTime());
            birdPos.Y = 1.0f + MathF.Cos((float)GetTime() * 4.0f) * 0.5f;
            float birdDirX = birdPos.X - birdPrev.X >= 0.0f ? 1.0f : -1.0f;

            // Update sprite UVs
            // We multiply by the sign of the X direction to invert the uvScale.x
            float currentFrame = 10.0f * (float)GetTime();
            GetTexCoordScaleOffset(ref matSprite.UvScale, ref matSprite.UvOffset, (int)(4 * birdDirX), 1, currentFrame);

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                // Draw scene
                R3D.Begin(camera);
                    R3D.DrawMesh(meshGround, matGround, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D.DrawMesh(meshSprite, matSprite, birdPos with { Z = 0 }, 1.0f);
                R3D.End();

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadMaterial(matSprite);
        R3D.UnloadMesh(meshSprite);
        R3D.UnloadMesh(meshGround);
        R3D.Close();

        CloseWindow();

        return 0;
    }

    private static void GetTexCoordScaleOffset(ref Vector2 uvScale, ref Vector2 uvOffset, int xFrameCount, int yFrameCount, float currentFrame)
    {
        uvScale.X = 1.0f / xFrameCount;
        uvScale.Y = 1.0f / yFrameCount;

        int frameIndex = (int)(currentFrame + 0.5f) % (xFrameCount * yFrameCount);
        int frameX = frameIndex % xFrameCount;
        int frameY = frameIndex / xFrameCount;

        uvOffset.X = frameX * uvScale.X;
        uvOffset.Y = frameY * uvScale.Y;
    }
}
