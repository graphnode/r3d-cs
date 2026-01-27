using System;
using System.Numerics;
using R3d_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static R3d_cs.R3D;

namespace Examples;

public static class Dof
{
    private const int X_INSTANCES = 10;
    private const int Y_INSTANCES = 10;
    private const int INSTANCE_COUNT = (X_INSTANCES * Y_INSTANCES);
    
    public static int Main()
    {
        // Initialize window
        InitWindow(800, 450, "[r3d] - DoF example");
        SetTargetFPS(60);

        // Initialize R3D with FXAA
        R3D_Init(GetScreenWidth(), GetScreenHeight());
        R3D_SetAntiAliasing(R3D_AntiAliasing.R3D_ANTI_ALIASING_FXAA);

        // Configure depth of field and background
        R3D_ENVIRONMENT_SET((ref env) =>
        {
            env.background.color = Color.Black;
            env.dof.mode = R3D_DoF.R3D_DOF_ENABLED;
            env.dof.focusPoint = 2.0f;
            env.dof.focusScale = 3.0f;
            env.dof.maxBlurSize = 20.0f;
            env.dof.debugMode = false;
        });

        // Create directional light
        R3D_Light light = R3D_CreateLight(R3D_LightType.R3D_LIGHT_DIR);
        R3D_SetLightDirection(light, new Vector3(0, -1, 0));
        R3D_SetLightActive(light, true);

        // Create sphere mesh and default material
        R3D_Mesh meshSphere = R3D_GenMeshSphere(0.2f, 64, 64);
        R3D_Material matDefault = R3D_GetDefaultMaterial();

        // Generate instance matrices and colors
        float spacing = 0.5f;
        float offsetX = (X_INSTANCES * spacing) / 2.0f;
        float offsetZ = (Y_INSTANCES * spacing) / 2.0f;
        int idx = 0;
        R3D_InstanceBuffer instances = R3D_LoadInstanceBuffer(INSTANCE_COUNT, R3D_InstanceFlags.R3D_INSTANCE_POSITION | R3D_InstanceFlags.R3D_INSTANCE_COLOR);
        var positions = R3D_MapInstances<Vector3>(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION);
        var colors = R3D_MapInstances<Color>(instances, R3D_InstanceFlags.R3D_INSTANCE_COLOR);
        for (int x = 0; x < X_INSTANCES; x++) {
            for (int y = 0; y < Y_INSTANCES; y++) {
                positions[idx] = new Vector3(x * spacing - offsetX, 0, y * spacing - offsetZ);
                colors[idx] = new Color(Random.Shared.Next(0, 256), Random.Shared.Next(0, 256), Random.Shared.Next(0, 256), 255);
                idx++;
            }
        }
        R3D_UnmapInstances(instances, R3D_InstanceFlags.R3D_INSTANCE_POSITION | R3D_InstanceFlags.R3D_INSTANCE_COLOR);

        // Setup camera
        Camera3D camDefault = new Camera3D {
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
            Matrix4x4 rotation = Matrix4x4.CreateFromAxisAngle(camDefault.Up, 0.1f * delta);
            Vector3 view = camDefault.Position - camDefault.Target;
            view = Vector3.Transform(view, rotation);
            camDefault.Position = camDefault.Target + view;

            // Adjust DoF based on mouse
            Vector2 mousePos = GetMousePosition();
            float focusPoint = 0.5f + (5.0f - (mousePos.Y / GetScreenHeight()) * 5.0f);
            float focusScale = 0.5f + (5.0f - (mousePos.X / GetScreenWidth()) * 5.0f);
            R3D_ENVIRONMENT_SET((ref env) => env.dof.focusPoint = focusPoint);
            R3D_ENVIRONMENT_SET((ref env) => env.dof.focusScale = focusScale);

            float mouseWheel = GetMouseWheelMove();
            if (mouseWheel != 0.0f) {
                float maxBlur = R3D_ENVIRONMENT_GET.dof.maxBlurSize;
                R3D_ENVIRONMENT_SET((ref env) => env.dof.maxBlurSize = maxBlur + mouseWheel * 0.1f);
            }

            if (IsKeyPressed(KeyboardKey.F1)) {
                R3D_ENVIRONMENT_SET((ref env) => env.dof.debugMode = !R3D_ENVIRONMENT_GET.dof.debugMode);
            }

            BeginDrawing();
                ClearBackground(Color.Black);

                // Render scene
                R3D_Begin(camDefault);
                    R3D_DrawMeshInstanced(meshSphere, matDefault, instances, INSTANCE_COUNT);
                R3D_End();

                // Display DoF values
                string dofText = $"Focus Point: {R3D_ENVIRONMENT_GET.dof.focusPoint:F2}\nFocus Scale: {R3D_ENVIRONMENT_GET.dof.focusScale:F2}\n" +
                                 $"Max Blur Size: {R3D_ENVIRONMENT_GET.dof.maxBlurSize:F2}\nDebug Mode: {R3D_ENVIRONMENT_GET.dof.debugMode}";
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
        R3D_UnloadInstanceBuffer(instances);
        R3D_UnloadMesh(meshSphere);
        R3D_Close();

        CloseWindow();

        return 0;
    }
}