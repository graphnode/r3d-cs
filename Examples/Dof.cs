using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Dof
{
    private const int X_INSTANCES = 10;
    private const int Y_INSTANCES = 10;
    private const int INSTANCE_COUNT = X_INSTANCES * Y_INSTANCES;

    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - DoF example");
        SetTargetFPS(60);

        // Initialize R3D with FXAA
        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetAntiAliasing(AntiAliasing.Fxaa);

        // Configure depth of field and background
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = Color.Black;
            env.Dof.Mode = DoF.Enabled;
            env.Dof.FocusPoint = 2.0f;
            env.Dof.FocusScale = 3.0f;
            env.Dof.MaxBlurSize = 20.0f;
            env.Dof.DebugMode = false;
        });

        // Create directional light
        var light = R3D.CreateLight(LightType.Dir);
        R3D.SetLightDirection(light, new Vector3(0, -1, 0));
        R3D.SetLightActive(light, true);

        // Create sphere mesh and default material
        var meshSphere = R3D.GenMeshSphere(0.2f, 64, 64);
        var matDefault = R3D.GetDefaultMaterial();

        // Generate instance matrices and colors
        var spacing = 0.5f;
        float offsetX = X_INSTANCES * spacing / 2.0f;
        float offsetZ = Y_INSTANCES * spacing / 2.0f;
        var idx = 0;
        var instances = R3D.LoadInstanceBuffer(INSTANCE_COUNT, InstanceFlags.Position | InstanceFlags.Color);
        var positions = R3D.MapInstances<Vector3>(instances, InstanceFlags.Position);
        var colors = R3D.MapInstances<Color>(instances, InstanceFlags.Color);
        for (var x = 0; x < X_INSTANCES; x++)
        for (var y = 0; y < Y_INSTANCES; y++)
        {
            positions[idx] = new Vector3(x * spacing - offsetX, 0, y * spacing - offsetZ);
            colors[idx] = new Color(Random.Shared.Next(0, 256), Random.Shared.Next(0, 256), Random.Shared.Next(0, 256), 255);
            idx++;
        }

        R3D.UnmapInstances(instances, InstanceFlags.Position | InstanceFlags.Color);

        // Setup camera
        var camDefault = new Camera3D
        {
            Position = new Vector3(0, 2, 2),
            Target = new Vector3(0, 0, 0),
            Up = new Vector3(0, 1, 0),
            FovY = 60
        };

        // Main loop
        while (!WindowShouldClose())
        {
            float delta = GetFrameTime();

            // Rotate camera
            var rotation = Matrix4x4.CreateFromAxisAngle(camDefault.Up, 0.1f * delta);
            var view = camDefault.Position - camDefault.Target;
            view = Vector3.Transform(view, rotation);
            camDefault.Position = camDefault.Target + view;

            // Adjust DoF based on mouse
            var mousePos = GetMousePosition();
            float focusPoint = 0.5f + (5.0f - mousePos.Y / GetScreenHeight() * 5.0f);
            float focusScale = 0.5f + (5.0f - mousePos.X / GetScreenWidth() * 5.0f);
            R3D.SetEnvironmentEx((ref env) => env.Dof.FocusPoint = focusPoint);
            R3D.SetEnvironmentEx((ref env) => env.Dof.FocusScale = focusScale);

            float mouseWheel = GetMouseWheelMove();
            if (mouseWheel != 0.0f)
            {
                float maxBlur = R3D.GetEnvironmentEx().Dof.MaxBlurSize;
                R3D.SetEnvironmentEx((ref env) => env.Dof.MaxBlurSize = maxBlur + mouseWheel * 0.1f);
            }

            if (IsKeyPressed(KeyboardKey.F1)) R3D.SetEnvironmentEx((ref env) => env.Dof.DebugMode = !R3D.GetEnvironmentEx().Dof.DebugMode);

            BeginDrawing();
                ClearBackground(Color.Black);

                // Render scene
                R3D.Begin(camDefault);
                    R3D.DrawMeshInstanced(meshSphere, matDefault, instances, INSTANCE_COUNT);
                R3D.End();

                // Display DoF values
                var env = R3D.GetEnvironmentEx();
                string dofText = $"Focus Point: {env.Dof.FocusPoint:F2}\nFocus Scale: {env.Dof.FocusScale:F2}\n" +
                                 $"Max Blur Size: {env.Dof.MaxBlurSize:F2}\nDebug Mode: {env.Dof.DebugMode}";
                DrawText(dofText, 10, 30, 20, new Color(255, 255, 255, 127));

                // Display instructions
                DrawText("F1: Toggle Debug Mode\nScroll: Adjust Max Blur Size\nMouse Left/Right: Shallow/Deep DoF\nMouse Up/Down: Adjust Focus Point Depth",
                    300, 10, 20, new Color(255, 255, 255, 127));

                // Display FPS
                string fpsText = $"FPS: {GetFPS()}";
                DrawText(fpsText, 10, 10, 20, new Color(255, 255, 255, 127));

            EndDrawing();
        }

        // Cleanup
        R3D.UnloadInstanceBuffer(instances);
        R3D.UnloadMesh(meshSphere);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
