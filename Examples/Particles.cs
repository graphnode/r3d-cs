using System;
using System.Numerics;
using System.Runtime.InteropServices;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using BlendMode = R3D_cs.BlendMode;

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
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Set environment
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = new Color(4, 4, 4);
            env.Bloom.Mode = R3D_cs.Bloom.Additive;
        });

        // Generate a gradient as emission texture for our particles
        Image image = GenImageGradientRadial(64, 64, 0.0f, Color.White, Color.Black);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);

        // Generate a quad mesh for our particles
        var mesh = R3D.GenMeshQuad(0.25f, 0.25f, 1, 1, Vector3.UnitZ);

        // Setup particle material
        var material = R3D.GetDefaultMaterial();
        material.BillboardMode = BillboardMode.Front;
        material.BlendMode = BlendMode.Additive;
        material.Albedo.Texture = R3D.GetBlackTexture();
        material.Emission.Color = new Color(255, 0, 0, 255);
        material.Emission.Texture = texture;
        material.Emission.Energy = 1.0f;

        // Create particle instance buffer
        var instances = R3D.LoadInstanceBuffer(MAX_PARTICLES, InstanceFlags.Position);

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
        Span<Vector3> positions = stackalloc Vector3[MAX_PARTICLES];
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

            R3D.UploadInstances(instances, InstanceFlags.Position, 0, positions, particleCount);

            BeginDrawing();
                R3D.Begin(camera);
                    R3D.DrawMeshInstanced(mesh, material, instances, particleCount);
                R3D.End();
                DrawFPS(10, 10);
            EndDrawing();
        }

        R3D.UnloadInstanceBuffer(instances);
        R3D.UnloadMaterial(material);
        R3D.UnloadMesh(mesh);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
