using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class CustomMesh
{
    public static int Main()
    {
        InitWindow(800, 450, "[r3d-cs] - Custom Mesh example");
        SetTargetFPS(60);

        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create mesh data for a colored triangle (3 vertices, 3 indices)
        var meshData = R3D.CreateMeshData(3, 3);

        // Set vertices directly via Span<Vertex>
        var verts = meshData.Vertices;
        verts[0] = new Vertex
        {
            Position = new Vector3(-0.5f, 0, 0),
            Normal = new Vector3(0, 1, 0),
            Texcoord = new Vector2(0, 0),
            Color = Color.Red
        };
        verts[1] = new Vertex
        {
            Position = new Vector3(0.5f, 0, 0),
            Normal = new Vector3(0, 1, 0),
            Texcoord = new Vector2(1, 0),
            Color = Color.Green
        };
        verts[2] = new Vertex
        {
            Position = new Vector3(0, 1, 0),
            Normal = new Vector3(0, 1, 0),
            Texcoord = new Vector2(0.5f, 1),
            Color = Color.Blue
        };

        // Set indices directly via Span<uint>
        var indices = meshData.Indices;
        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;

        // Generate tangents for the mesh data
        R3D.GenMeshDataTangents(ref meshData);

        // Upload to GPU
        var mesh = R3D.LoadMesh(PrimitiveType.Triangles, meshData, MeshUsage.StaticMesh);

        // Double-sided material so the triangle renders from both sides
        var material = R3D.GetDefaultMaterial();
        material.CullMode = CullMode.None;

        // Setup environment
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(50, 50, 50, 255));

        // Create light
        var light = R3D.CreateLight(LightType.Omni);
        R3D.SetLightPosition(light, new Vector3(0, 3, 3));
        R3D.SetLightActive(light, true);

        // Setup camera
        var camera = new Camera3D
        {
            Position = new Vector3(0, 1, 2),
            Target = new Vector3(0, 0.4f, 0),
            Up = new Vector3(0, 1, 0),
            FovY = 60
        };

        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);

            BeginDrawing();
                ClearBackground(Color.RayWhite);

                R3D.Begin(camera);
                    R3D.DrawMesh(mesh, material, Vector3.Zero, 1.0f);
                R3D.End();

                DrawText("Custom triangle built with Span<Vertex>", 10, 10, 20, Color.DarkGray);

            EndDrawing();
        }

        R3D.UnloadMesh(mesh);
        R3D.UnloadMeshData(meshData);
        R3D.Close();
        CloseWindow();

        return 0;
    }
}
