using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class ToTexture
{
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d-cs] - Render to texture");
        SetTargetFPS(60);

        // Initialize R3D
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        // Create meshes
        var plane = R3D.GenMeshPlane(1000, 1000, 1, 1);
        var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
        var material = R3D.GetDefaultMaterial();

        // Setup environment
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(10, 10, 10, 255));

        // Create light
        var light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
        R3D.EnableShadow(light);
        R3D.SetLightActive(light, true);

        // Render texture that the R3D scene is drawn into
        var target = LoadRenderTexture(1024, 512);

        // The R3D camera renders the scene; the raylib camera views the resulting texture
        var r3dCamera = new Camera3D
        {
            Position = new Vector3(0, 2, 2),
            Target = Vector3.Zero,
            Up = new Vector3(0, 1, 0),
            FovY = 60
        };
        var rlCamera = new Camera3D
        {
            Position = new Vector3(0, 1, 4),
            Target = new Vector3(0, 1, -1),
            Up = new Vector3(0, 1, 0),
            FovY = 60
        };

        DisableCursor();

        // Main loop
        while (!WindowShouldClose())
        {
            UpdateCamera(ref r3dCamera, CameraMode.Orbital);
            UpdateCamera(ref rlCamera, CameraMode.Free);

            BeginDrawing();
                ClearBackground(Color.DarkGray);

                // Render the R3D scene into the render texture
                var view = new View
                {
                    Camera = R3D.CameraFromRL(r3dCamera),
                    Target = target
                };

                R3D.BeginPro(view);
                    R3D.DrawMesh(plane, material, new Vector3(0, -0.5f, 0), 1.0f);
                    R3D.DrawMesh(sphere, material, Vector3.Zero, 1.0f);
                R3D.End();

                // Display the render texture as a billboard in a plain raylib scene
                BeginMode3D(rlCamera);
                    DrawBillboard(rlCamera, target.Texture, new Vector3(0, 1, 0), -2, Color.White);
                    DrawGrid(10, 1);
                EndMode3D();

            EndDrawing();
        }

        // Cleanup
        UnloadRenderTexture(target);
        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
