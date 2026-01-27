using System;
using System.Numerics;
using System.Runtime.InteropServices;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Particles
{
    private const int MAX_PARTICLES = 4096;

    [StructLayout(LayoutKind.Sequential)]
    private struct Particle
    {
        public Vector3 pos;
        public Vector3 vel;
        public float life;
    }
    
    public static unsafe int Main()
    {
        // Initialize window
    InitWindow(800, 450, "[r3d] - Particles example");
    SetTargetFPS(60);

    // Initialize R3D
    R3D_Init(GetScreenWidth(), GetScreenHeight());

    // Set environment
    R3D_ENVIRONMENT_SET((ref env) =>
    {
        env.background.color = new Color(4, 4, 4);
        env.bloom.mode = R3D_Bloom.R3D_BLOOM_ADDITIVE;
    });

    // Generate a gradient as emission texture for our particles
    Image image = GenImageGradientRadial(64, 64, 0.0f, Color.White, Color.Black);
    Texture2D texture = LoadTextureFromImage(image);
    UnloadImage(image);

    // Generate a quad mesh for our particles
    R3D_Mesh mesh = R3D_GenMeshQuad(0.25f, 0.25f, 1, 1, Vector3.UnitZ);

    // Setup particle material
    R3D_Material material = R3D_GetDefaultMaterial();
    material.billboardMode =R3D_BillboardMode.R3D_BILLBOARD_FRONT;
    material.blendMode = R3D_BlendMode.R3D_BLEND_ADDITIVE;
    material.albedo.texture = R3D_GetBlackTexture();
    material.emission.color = new Color(255, 0, 0, 255);
    material.emission.texture = texture;
    material.emission.energy = 1.0f;

    // Create particle instance buffer
    R3D_InstanceBuffer instances = R3D_LoadInstanceBuffer(MAX_PARTICLES, R3D_InstanceFlags.R3D_INSTANCE_POSITION);

    // Setup camera
    Camera3D camera = new Camera3D {
        Position = new Vector3(-7, 7, -7),
        Target = new Vector3(0, 1, 0),
        Up = Vector3.UnitY,
        FovY = 60.0f,
        Projection = CameraProjection.Perspective
    };

    // CPU buffer for storing particles
    Particle[] particles = new Particle[MAX_PARTICLES];
    var positions = stackalloc Vector3[MAX_PARTICLES];
    int particleCount = 0;

    while (!WindowShouldClose())
    {
        float dt = GetFrameTime();
        UpdateCamera(ref camera, CameraMode.Orbital);

        // Spawn particles
        for (int i = 0; i < 10; i++) {
            if (particleCount < MAX_PARTICLES) {
                float angle = GetRandomValue(0, 360) * DEG2RAD;
                particles[particleCount].pos = Vector3.Zero;
                particles[particleCount].vel = new Vector3(
                    MathF.Cos(angle) * GetRandomValue(20, 40) / 10.0f,
                    GetRandomValue(60, 80) / 10.0f,
                    MathF.Sin(angle) * GetRandomValue(20, 40) / 10.0f
                );
                particles[particleCount].life = 1.0f;
                particleCount++;
            }
        }

        // Update particles
        int alive = 0;
        for (int i = 0; i < particleCount; i++) {
            particles[i].vel.Y -= 9.81f * dt;
            particles[i].pos.X += particles[i].vel.X * dt;
            particles[i].pos.Y += particles[i].vel.Y * dt;
            particles[i].pos.Z += particles[i].vel.Z * dt;
            particles[i].life -= dt * 0.5f;
            if (particles[i].life > 0) {
                positions[alive] = particles[i].pos;
                particles[alive] = particles[i];
                alive++;
            }
        }
        particleCount = alive;

        R3D_UploadInstances(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION, 0, particleCount, positions);

        BeginDrawing();
            R3D_Begin(camera);
                R3D_DrawMeshInstanced(mesh, material, instances, particleCount);
            R3D_End();
            DrawFPS(10, 10);
        EndDrawing();
    }

    R3D_UnloadInstanceBuffer(instances);
    R3D_UnloadMaterial(material);
    R3D_UnloadMesh(mesh);
    R3D_Close();

    CloseWindow();

    return 0;
    }
}