using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Sprite
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - Sprite example");
        SetTargetFPS(60);

        // Initialize R3D
        R3D_Init(GetScreenWidth(), GetScreenHeight());
        R3D_SetTextureFilter(TextureFilter.Point);

        // Set background/ambient color
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            env.background.color = new Color(102, 191, 255, 255);
            env.ambient.color = new Color(10, 19, 25, 255);
            env.tonemap.mode = R3D_Tonemap.R3D_TONEMAP_FILMIC;
        });
        
        // Create ground mesh and material
        R3D_Mesh meshGround = R3D_GenMeshPlane(200, 200, 1, 1);
        R3D_Material matGround = R3D_GetDefaultMaterial();
        matGround.albedo.color = Color.Green;

        // Create sprite mesh and material
        R3D_Mesh meshSprite = R3D_GenMeshQuad(1.0f, 1.0f, 1, 1, Vector3.UnitZ);
        meshSprite.shadowCastMode = R3D_ShadowCastMode.R3D_SHADOW_CAST_ON_DOUBLE_SIDED;

        R3D_Material matSprite = R3D_GetDefaultMaterial();
        matSprite.albedo = R3D_LoadAlbedoMap("resources/images/spritesheet.png", Color.White);
        matSprite.billboardMode = R3D_BillboardMode.R3D_BILLBOARD_Y_AXIS;

        // Setup spotlight
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_SPOT);
        R3D_LightLookAt(light, new Vector3(0,10,10), Vector3.Zero);
        R3D_SetLightRange(light, 64.0f);
        R3D_EnableShadow(light);
        R3D_SetLightActive(light, true);

        // Setup camera
        Camera3D camera = new Camera3D() {
            Position = new Vector3(0, 2, 5),
            Target = new Vector3(0, 0.5f, 0), 
            Up = Vector3.UnitY,
            FovY = 45
        };

        // Bird data
        Vector3 birdPos = new Vector3(0, 0.5f, 0);

        // Main loop
        while (!WindowShouldClose())
        {
            // Update bird position
            Vector3 birdPrev = birdPos;
            birdPos.X = 2.0f * MathF.Sin((float)GetTime());
            birdPos.Y = 1.0f + MathF.Cos((float)GetTime() * 4.0f) * 0.5f;
            float birdDirX = (birdPos.X - birdPrev.X >= 0.0f) ? 1.0f : -1.0f;

            // Update sprite UVs
            // We multiply by the sign of the X direction to invert the uvScale.x
            float currentFrame = 10.0f * (float)GetTime();
            GetTexCoordScaleOffset(ref matSprite.uvScale, ref matSprite.uvOffset, (int)(4 * birdDirX), 1, currentFrame);

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                // Draw scene
                R3D_Begin(camera);
                    R3D_DrawMesh(meshGround, matGround, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D_DrawMesh(meshSprite, matSprite, birdPos with { Z = 0 }, 1.0f);
                R3D_End();

            EndDrawing();
        }

        // Cleanup
        R3D_UnloadMaterial(matSprite);
        R3D_UnloadMesh(meshSprite);
        R3D_UnloadMesh(meshGround);
        R3D_Close();

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