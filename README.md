![R3D-cs Logo](https://media.githubusercontent.com/media/graphnode/r3d-cs/master/R3D-cs/logo/r3d-cs_256x256.png "R3D-cs Logo")

# R3D-cs

[![CI](https://img.shields.io/github/actions/workflow/status/graphnode/r3d-cs/ci.yml)](https://github.com/graphnode/r3d-cs/actions)
[![NuGet](https://img.shields.io/nuget/v/r3d-cs?color=0b6cff)](https://www.nuget.org/packages/R3D-cs)
[![NuGet Downloads](https://img.shields.io/nuget/dt/r3d-cs?color=9b4f97)](https://www.nuget.org/packages/R3D-cs)

.NET/C# bindings for [r3d](https://github.com/Bigfoot71/r3d), an advanced 3D rendering library built on top of raylib.

> **Experimental**: This package is in early development. APIs may change between versions.

> **Note**: [r3d](https://github.com/Bigfoot71/r3d) itself is under heavy development and has not reached version 1.0. Expect breaking changes in the underlying
> library as well.

> **Important**: For now this package includes its own `raylib.dll` from r3d with some features enabled (like HDR file loading).
> This will overwrite the native library provided by raylib-cs.

## Installation - NuGet (Easy mode)

```
dotnet add package R3D-cs --prerelease
```

## Installation - Manual (Hard mode)

1. Download/clone the repo.
2. Add the R3D-cs project to your solution as an existing project.
3. Download/build the native libraries from R3D repository (check the Actions tab for artifacts) and from raylib releases.
4. Make sure the native libraries are in the same folder as the output.

## Requirements

- .NET 10.0 or later (might work with earlier versions but not tested)
- [Raylib-cs](https://www.nuget.org/packages/Raylib-cs) (included as a dependency)
- Native r3d library for your platform (included in package)

## Quick Start

```csharp
using System.Numerics;
using Raylib_cs;
using R3D_cs;

// Initialize window
Raylib.InitWindow(800, 450, "[r3d] - Basic example");
Raylib.SetTargetFPS(60);

// Initialize R3D
R3D.Init(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

// Create meshes
var plane = R3D.GenMeshPlane(1000, 1000, 1, 1);
var sphere = R3D.GenMeshSphere(0.5f, 64, 64);
var material = R3D.GetDefaultMaterial();

// Setup environment
var env = R3D.GetEnvironmentEx();
env.Ambient.Color = new Color(10, 10, 10, 255);
R3D.SetEnvironmentEx(env);

// Create light
Light light = R3D.CreateLight(LightType.Spot);
R3D.LightLookAt(light, new Vector3(0, 10, 5), Vector3.Zero);
R3D.EnableShadow(light);
R3D.SetLightActive(light, true);

// Setup camera
var camera = new Camera3D() {
    Position = new Vector3(0, 2, 2),
    Target = Vector3.Zero,
    Up = new Vector3(0, 1, 0),
    FovY = 60
};

// Main loop
while (!Raylib.WindowShouldClose())
{
    Raylib.UpdateCamera(ref camera, CameraMode.Orbital);

    Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.RayWhite);

        R3D.Begin(camera);
            R3D.DrawMesh(plane, material, new Vector3(0, -0.5f, 0), 1.0f);
            R3D.DrawMesh(sphere, material, Vector3.Zero, 1.0f);
        R3D.End();
    Raylib.EndDrawing();
}

// Cleanup
R3D.UnloadMesh(sphere);
R3D.UnloadMesh(plane);
R3D.Close();

Raylib.CloseWindow();
```

## Alternatives

- [R3d.Net](https://github.com/Kiriller12/R3d.Net) - .NET bindings for r3d by Kiriller12

## Documentation

- [r3d documentation](https://github.com/Bigfoot71/r3d)
- [raylib-cs documentation](https://github.com/ChrisDill/Raylib-cs)

## Contributing

Feel free to open an issue. If you'd like to contribute, please fork the repository and make changes as you'd like. Pull requests are welcome.

## License

This project is licensed under the zlib license. See [r3d](https://github.com/Bigfoot71/r3d) for the original library license.

## Acknowledgements

We're grateful to the raylib, r3d, raylib-cs, and r3d-net developers for their outstanding contributions to the raylib community. This project's structure,
documentation, and packaging are all inspired by their excellent work.