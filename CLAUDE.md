# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

R3D-cs is a .NET/C# binding library for [r3d](https://github.com/Bigfoot71/r3d), an advanced 3D rendering library built on top of raylib. The project consists of:

1. **R3D-cs** - The main bindings library (auto-generated + manual utilities)
2. **R3D-cs.GenerateBindings** - Tool to generate C# bindings from r3d C headers
3. **Examples** - Sample applications demonstrating r3d features

## Critical Architecture Concepts

### Bindings Generation Architecture

The bindings are **partially auto-generated** from the r3d C headers:

- **Auto-generated files** (`.g.cs` suffix):
  - `enums/**/*.g.cs` - C enums mapped to C# enums
  - `types/**/*.g.cs` - C structs mapped to C# structs
  - `interop/**/*.g.cs` - P/Invoke declarations for C functions
  - These files have "Do not edit manually" headers

- **Hand-written files** (no `.g.cs` suffix):
  - `R3D-cs/interop/R3D.Utilities.cs` - Helper methods, constants, and convenience wrappers
  - Contains `MATERIAL_BASE`, `DECAL_BASE`, `CUBEMAP_SKY_BASE` constants
  - Contains `MapInstances<T>()`, `SetEnvironmentEx()` utility methods

**IMPORTANT**: When r3d structs change (like `Material` or `InstanceBuffer`), you MUST update:
1. Auto-generated files via bindings generator
2. `R3D.Utilities.cs` constants manually (they won't auto-update)
3. Native DLLs in `runtimes/win-x64/native/`

### Struct Marshaling with DisableRuntimeMarshalling

The project uses `[assembly: DisableRuntimeMarshalling]` in `R3D.core.g.cs` for performance. This means:

- Structs must have `[StructLayout(LayoutKind.Sequential)]`
- Fixed buffers work correctly with `LibraryImport` (preferred over `DllImport`)
- Structs with complex layouts may need special handling

**Known Issue**: Functions returning structs by value with fixed buffers may fail if the native DLL is out of sync with C# struct definitions. Symptoms: fields appear to have wrong values (e.g., `Capacity` reads from wrong offset).

### Native Library Synchronization

The C# bindings and native DLLs (`r3d.dll`, `raylib.dll`, `assimp-vc145-mt.dll`) MUST match:

```
C# Binding (*.g.cs) <---> C Header (*.h) <---> Compiled DLL (*.dll)
     ^                        ^                      ^
     |                        |                      |
  Generated from          Source of truth        Built from
```

**Production**: CI automatically builds matching DLLs for all platforms during release.
**Development**: You may need to build DLLs locally to test binding changes before CI runs.

If they don't match, you'll see:
- Crashes with access violations (`0xC0000005`)
- Struct fields containing garbage data
- Wrong function signatures causing stack corruption

## Building and Development

### Build the main library
```bash
dotnet build R3D-cs.sln
```

### Run examples
```bash
# Run specific example
cd Examples
dotnet run -- <example-name>  # e.g., shader, lights, transparency

# Run all examples sequentially
dotnet run
```

### Regenerate bindings from r3d source

**When to regenerate**:
- r3d library updates its C API
- New functions/structs/enums are added
- Existing struct layouts change

**Steps**:
```bash
cd R3D-cs.GenerateBindings

# Using local r3d repository (default output: ../R3D-cs)
dotnet run -- -p /path/to/r3d -v 0.7.0

# Specify custom output directory
dotnet run -- -p /path/to/r3d -v 0.7.0 -o /path/to/output
```

**After regeneration**, you MUST:
1. Update `R3D.Utilities.cs` if `Material`, `Decal`, or other constants changed
2. Rebuild r3d native library for local development/testing (see below)

### Rebuild r3d native library (Development Only)

**Important**: Building native libraries is only needed for local development and testing.

- **DO NOT commit** DLLs in `runtimes/` folder to git
- **CI builds them automatically** for all platforms during release
- Only build locally when you need to test bindings changes immediately

```bash
# In r3d repository
mkdir -p build && cd build
cmake -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON \
      -DR3D_ASSIMP_VENDORED=ON -DR3D_RAYLIB_VENDORED=ON ..
cmake --build . --config Release -j8

# Copy DLLs to R3D-cs for local testing (DO NOT COMMIT)
cp bin/Release/*.dll /path/to/R3D-cs/R3D-cs/runtimes/win-x64/native/
```

**Note**: Native DLLs in `runtimes/*/native/` are already in `.gitignore` and will not be committed. CI automatically builds and packages them for all platforms during the release process.

## Code Generation Internals

### TypeMapper.cs - Type Translation Rules

Maps C types to C# equivalents:

- Primitives: `int` → `int`, `float` → `float`, `bool` → `bool`
- Pointers: `Type*` → `Type*` (unsafe), `const char*` → `string`
- **Pointer-to-pointer**: `Type**` → `Type**` (NOT `ref Type`)
- Arrays: `Type[N]` → `fixed Type Field[N]`
- Opaque types: Empty structs (e.g., `ScreenShader`)

**GetParameterModifier()** logic:
- `Get*` functions with primitive pointers → `out` parameters
- Single pointers to structs → `ref` parameters
- Pointer-to-pointer (`**`) → kept as-is (raw pointer)
- Opaque handles → kept as pointers

### CommentGenerator.cs

Extracts Doxygen-style comments from C headers and converts to C# XML doc comments:
- `@brief` → `<summary>`
- `@param` → `<param>`
- `@return` → `<returns>`
- `@warning` → `<remarks><b>Warning:</b>`

### CodeGenerator.cs

Generates three file types:
1. **Enums** (`enums/`) - Strips `R3D_` prefix, converts to PascalCase
2. **Structs** (`types/`) - Adds `unsafe` if contains pointers/fixed buffers
3. **P/Invoke** (`interop/`) - Groups by C header file, uses `LibraryImport`

## Common Pitfalls

### When MATERIAL_BASE causes rendering issues
**Symptom**: Objects don't render or render incorrectly after r3d update
**Cause**: `Material` struct changed but `MATERIAL_BASE` in `R3D.Utilities.cs` wasn't updated
**Fix**: Compare `Material.g.cs` with `MATERIAL_BASE` and add missing fields with defaults

### When pointer-to-pointer functions crash
**Symptom**: Crash when calling functions like `SetScreenShaderChain`
**Cause**: Bindings generator converted `Type**` to `ref Type`
**Fix**: Should be auto-fixed by TypeMapper.cs check for `EndsWith("**")`, but verify binding

### When InstanceBuffer.Capacity is wrong
**Symptom**: Capacity shows 1 or garbage value instead of expected count
**Cause**: Native DLL built with different `R3D_INSTANCE_ATTRIBUTE_COUNT` than C# bindings
**Fix**: Rebuild r3d library and update DLLs

### When MapInstances crashes with IndexOutOfRangeException
**Symptom**: Accessing positions array throws index out of bounds
**Cause**: Span constructor was given byte count instead of element count
**Fix**: `new Span<T>(ptr, buffer.Capacity)` NOT `buffer.Capacity * sizeof(T)`

## Project File Structure

```
R3D-cs/
├── enums/          # Auto-generated enums (*.g.cs)
├── types/          # Auto-generated structs (*.g.cs)
├── interop/        # Auto-generated P/Invoke + R3D.Utilities.cs
└── runtimes/       # Native DLLs per platform
    └── win-x64/native/
        ├── r3d.dll
        ├── raylib.dll
        └── assimp-vc145-mt.dll

R3D-cs.GenerateBindings/
├── Program.cs          # CLI entry point
├── CodeGenerator.cs    # Main generation logic
├── TypeMapper.cs       # C to C# type mapping
├── CommentGenerator.cs # Doxygen to XML doc conversion
└── StringHelpers.cs    # Naming conventions

Examples/
├── Program.cs          # Example runner
└── *.cs               # Individual example files
```

## Dependencies

- **Raylib-cs** (7.0.2): C# bindings for raylib, required dependency
- **CppAst** (0.24.0): C/C++ parser for header files (bindings generator only)
- **r3d native libraries**: Included in `runtimes/`, built from r3d source
