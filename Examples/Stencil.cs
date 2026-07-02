using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

public static class Stencil
{
    public static int Main()
    {
        InitWindow(800, 450, "[r3d-cs] - Stencil Effects");
        SetTargetFPS(60);

        R3D.Init(GetScreenWidth(), GetScreenHeight());
        R3D.SetAntiAliasingMode(AntiAliasingMode.Smaa);

        // Create the meshes used in the scene
        var plane = R3D.GenMeshPlane(20, 20, 1, 1);
        var box = R3D.GenMeshCube(1.5f, 2.0f, 0.3f);
        var sphere = R3D.GenMeshSphere(0.5f, 32, 32);

        var matGround = R3D.GetDefaultMaterial();
        matGround.Albedo.Color = new Color(160, 160, 160, 255);

        var matWall = R3D.GetDefaultMaterial();
        matWall.Albedo.Color = new Color(120, 80, 60, 255);

        // Main X-Ray sphere material.
        // The first pass draws the sphere normally and marks its visible pixels
        // in the stencil buffer with the value 0x01.
        var matXraySolid = R3D.GetDefaultMaterial();
        matXraySolid.Albedo.Color = new Color(80, 140, 220, 255);
        matXraySolid.Stencil.Mode = CompareMode.Always;
        matXraySolid.Stencil.Ref = 0x01;
        matXraySolid.Stencil.OpPass = StencilOp.StencilReplace;

        // Ghost X-Ray sphere material.
        // The second pass ignores depth so the sphere can be drawn through the wall,
        // but only where the first pass did not already mark the stencil buffer.
        var matXrayGhost = R3D.GetDefaultMaterial();
        matXrayGhost.Albedo.Color = new Color(80, 140, 220, 60);
        matXrayGhost.Depth.Mode = CompareMode.Always;
        matXrayGhost.Stencil.Mode = CompareMode.Notequal;
        matXrayGhost.Stencil.Ref = 0x01;
        matXrayGhost.TransparencyMode = TransparencyMode.Alpha;
        matXrayGhost.Unlit = true;

        // Main outline sphere material.
        // The first pass draws the red sphere and marks its silhouette
        // in the stencil buffer with the value 0x02.
        var matOutlineSolid = R3D.GetDefaultMaterial();
        matOutlineSolid.Albedo.Color = new Color(220, 100, 80, 255);
        matOutlineSolid.Stencil.Mode = CompareMode.Always;
        matOutlineSolid.Stencil.Ref = 0x02;
        matOutlineSolid.Stencil.OpPass = StencilOp.StencilReplace;

        // Outline material.
        // The second pass draws the same sphere slightly larger, only on pixels
        // outside the silhouette already marked by the first pass.
        var matOutlineRing = R3D.GetDefaultMaterial();
        matOutlineRing.Albedo.Color = new Color(255, 220, 0, 255);
        matOutlineRing.Stencil.Mode = CompareMode.Notequal;
        matOutlineRing.Stencil.Ref = 0x02;
        matOutlineRing.CullMode = CullMode.Front;
        matOutlineRing.Unlit = true;

        // Configure lighting, shadows, and ambient color
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(10, 10, 15, 255));

        var light = R3D.CreateLight(LightType.Spot);
        R3D.LightLookAt(light, new Vector3(4, 8, 5), Vector3.Zero);
        R3D.SetShadowSoftness(light, 8.0f);
        R3D.SetLightActive(light, true);
        R3D.EnableShadow(light);

        var camera = new Camera3D
        {
            Position = new Vector3(0, 3, 5),
            Target = Vector3.Zero,
            Up = new Vector3(0, 1, 0),
            FovY = 55
        };

        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);

            BeginDrawing();
                ClearBackground(Color.Black);

                R3D.Begin(camera);
                    // Base scene geometry
                    R3D.DrawMesh(plane, matGround, new Vector3(0.0f, -0.5f, 0.0f), 1.0f);
                    R3D.DrawMesh(box, matWall, new Vector3(0.0f, 0.5f, 0.0f), 1.0f);

                    // X-Ray sphere: visible solid pass, then transparent pass through the wall
                    R3D.DrawMesh(sphere, matXraySolid, new Vector3(0.0f, 0.5f, -1.5f), 1.0f);
                    R3D.DrawMesh(sphere, matXrayGhost, new Vector3(0.0f, 0.5f, -1.5f), 1.0f);

                    // Outline sphere: normal object pass, then slightly enlarged outline pass
                    R3D.DrawMesh(sphere, matOutlineSolid, new Vector3(2.2f, 0.2f, 0.8f), 1.00f);
                    R3D.DrawMesh(sphere, matOutlineRing, new Vector3(2.2f, 0.2f, 0.8f), 1.08f);
                R3D.End();
            EndDrawing();
        }

        R3D.UnloadMesh(sphere);
        R3D.UnloadMesh(box);
        R3D.UnloadMesh(plane);
        R3D.Close();

        CloseWindow();

        return 0;
    }
}
