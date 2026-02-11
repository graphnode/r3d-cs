using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

// ReSharper disable InconsistentNaming

namespace R3D_cs;

/// <summary>
///     Handwritten utility methods extending the generated R3D bindings.
///     This file is preserved when regenerating bindings.
/// </summary>
public static unsafe partial class R3D
{
    /// <summary>
    ///     Delegate for updating environment settings by reference.
    /// </summary>
    /// <param name="env">Reference to the environment to modify.</param>
    public delegate void EnvironmentUpdater(ref Environment env);

    /// <summary>
    ///     Gets a default material with sensible base values.
    ///     Use this as a starting point when creating new materials.
    /// </summary>
    /// <remarks>
    ///     Default values:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Albedo: White color, no texture</description>
    ///         </item>
    ///         <item>
    ///             <description>Emission: White color, no texture, 0 energy</description>
    ///         </item>
    ///         <item>
    ///             <description>Normal: No texture, scale 1.0</description>
    ///         </item>
    ///         <item>
    ///             <description>ORM: Occlusion 1.0, Roughness 1.0, Metalness 0.0</description>
    ///         </item>
    ///         <item>
    ///             <description>UV: Offset (0, 0), Scale (1, 1)</description>
    ///         </item>
    ///         <item>
    ///             <description>Alpha Cutoff: 0.01</description>
    ///         </item>
    ///         <item>
    ///             <description>Depth: Standard depth test (LESS)</description>
    ///         </item>
    ///         <item>
    ///             <description>Stencil: Disabled (ALWAYS)</description>
    ///         </item>
    ///         <item>
    ///             <description>Transparency: Disabled</description>
    ///         </item>
    ///         <item>
    ///             <description>Billboard: Disabled</description>
    ///         </item>
    ///         <item>
    ///             <description>Blend: Mix</description>
    ///         </item>
    ///         <item>
    ///             <description>Cull: Back</description>
    ///         </item>
    ///         <item>
    ///             <description>Unlit: false</description>
    ///         </item>
    ///         <item>
    ///             <description>Shader: null</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static Material MATERIAL_BASE =>
        new()
        {
            Albedo = new AlbedoMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255)
            },
            Emission = new EmissionMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255),
                Energy = 0.0f
            },
            Normal = new NormalMap
            {
                Texture = default,
                Scale = 1.0f
            },
            Orm = new OrmMap
            {
                Texture = default,
                Occlusion = 1.0f,
                Roughness = 1.0f,
                Metalness = 0.0f
            },
            UvOffset = Vector2.Zero,
            UvScale = Vector2.One,
            AlphaCutoff = 0.01f,
            Depth = new DepthState
            {
                Mode = CompareMode.Less,
                OffsetFactor = 0.0f,
                OffsetUnits = 0.0f,
                RangeNear = 0.0f,
                RangeFar = 1.0f
            },
            Stencil = new StencilState
            {
                Mode = CompareMode.Always,
                Ref = 0x00,
                Mask = 0xFF,
                OpFail = StencilOp.StencilKeep,
                OpZFail = StencilOp.StencilKeep,
                OpPass = StencilOp.StencilReplace
            },
            TransparencyMode = TransparencyMode.Disabled,
            BillboardMode = BillboardMode.Disabled,
            BlendMode = BlendMode.Mix,
            CullMode = CullMode.Back,
            Unlit = false,
            Shader = default
        };

    /// <summary>
    ///     Gets default procedural sky parameters for cubemap generation.
    ///     Use this as a starting point when creating procedural skyboxes with <see cref="GenCubemapSky" />.
    /// </summary>
    /// <remarks>
    ///     Creates a sky with blue-gray tones, brown ground, and a white sun.
    /// </remarks>
    public static CubemapSky CUBEMAP_SKY_BASE =>
        new()
        {
            SkyTopColor = new Color(98, 116, 140, 255),
            SkyHorizonColor = new Color(165, 167, 171, 255),
            SkyHorizonCurve = 0.15f,
            SkyEnergy = 1.0f,
            GroundBottomColor = new Color(51, 43, 34, 255),
            GroundHorizonColor = new Color(165, 167, 171, 255),
            GroundHorizonCurve = 0.02f,
            GroundEnergy = 1.0f,
            SunDirection = new Vector3(-1.0f, -1.0f, -1.0f),
            SunColor = new Color(255, 255, 255, 255),
            SunSize = 1.5f * Raylib.DEG2RAD,
            SunCurve = 0.15f,
            SunEnergy = 1.0f
        };

    /// <summary>
    ///     Gets default decal parameters.
    ///     Use this as a starting point when creating decals.
    /// </summary>
    /// <remarks>
    ///     Default values:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Albedo: White color, no texture</description>
    ///         </item>
    ///         <item>
    ///             <description>Emission: White color, no texture, 0 energy</description>
    ///         </item>
    ///         <item>
    ///             <description>Normal: No texture, scale 1.0</description>
    ///         </item>
    ///         <item>
    ///             <description>ORM: Occlusion 1.0, Roughness 1.0, Metalness 0.0</description>
    ///         </item>
    ///         <item>
    ///             <description>UV: Offset (0, 0), Scale (1, 1)</description>
    ///         </item>
    ///         <item>
    ///             <description>Alpha Cutoff: 0.01</description>
    ///         </item>
    ///         <item>
    ///             <description>Normal Threshold: 0</description>
    ///         </item>
    ///         <item>
    ///             <description>Fade Width: 0</description>
    ///         </item>
    ///         <item>
    ///             <description>ApplyColor: true</description>
    ///         </item>
    ///         <item>
    ///             <description>Shader: null</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static Decal DECAL_BASE =>
        new()
        {
            Albedo = new AlbedoMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255)
            },
            Emission = new EmissionMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255),
                Energy = 0.0f
            },
            Normal = new NormalMap
            {
                Texture = default,
                Scale = 1.0f
            },
            Orm = new OrmMap
            {
                Texture = default,
                Occlusion = 1.0f,
                Roughness = 1.0f,
                Metalness = 0.0f
            },
            UvOffset = Vector2.Zero,
            UvScale = Vector2.One,
            AlphaCutoff = 0.01f,
            NormalThreshold = 0,
            FadeWidth = 0,
            ApplyColor = true,
            Shader = default
        };

    /// <summary>
    ///     Gets a reference to the current environment settings.
    ///     Allows direct modification of environment properties without copying.
    /// </summary>
    /// <returns>A reference to the current <see cref="Environment" />.</returns>
    public static ref Environment GetEnvironmentEx()
    {
        return ref Unsafe.AsRef<Environment>(GetEnvironment());
    }

    /// <summary>
    ///     Updates the current environment settings using a callback.
    ///     This is the recommended way to modify environment settings.
    /// </summary>
    /// <param name="updater">A delegate that receives the environment by reference for modification.</param>
    /// <example>
    ///     <code>
    /// R3D.SetEnvironmentEx((ref env) =>
    /// {
    ///     env.Bloom.Mode = Bloom.Mix;
    ///     env.Tonemap.Mode = Tonemap.Filmic;
    /// });
    /// </code>
    /// </example>
    public static void SetEnvironmentEx(EnvironmentUpdater updater)
    {
        updater(ref GetEnvironmentEx());
    }

    /// <summary>
    ///     Sets the environment from a struct value.
    ///     Useful for restoring a previously saved environment.
    /// </summary>
    /// <param name="env">The environment settings to apply.</param>
    /// <example>
    ///     <code>
    /// // Save current environment
    /// Environment saved = R3D.GetEnvironmentEx();
    ///
    /// // Modify current...
    /// R3D.SetEnvironmentEx((ref env) => env.Bloom.Mode = Bloom.Additive);
    ///
    /// // Restore saved environment
    /// R3D.SetEnvironmentEx(saved);
    /// </code>
    /// </example>
    public static void SetEnvironmentEx(Environment env)
    {
        SetEnvironment(&env);
    }

    /// <summary>
    ///     Maps instance buffer data to a typed span for direct CPU access.
    ///     Call <see cref="UnmapInstances" /> when done writing.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to map (e.g., Vector3 for positions, Color for colors).</typeparam>
    /// <param name="buffer">The instance buffer to map.</param>
    /// <param name="flag">Which instance attribute to map (Position, Rotation, Scale, or Color).</param>
    /// <returns>A span allowing direct read/write access to the instance data.</returns>
    /// <example>
    ///     <code>
    /// var positions = R3D.MapInstances&lt;Vector3&gt;(instances, InstanceFlags.Position);
    /// for (int i = 0; i &lt; 100; i++)
    ///     positions[i] = new Vector3(i, 0, 0);
    /// R3D.UnmapInstances(instances, InstanceFlags.Position);
    /// </code>
    /// </example>
    public static Span<T> MapInstances<T>(InstanceBuffer buffer, InstanceFlags flag) where T : unmanaged
    {
        return new Span<T>((void*)MapInstances(buffer, flag), buffer.Capacity);
    }

    /// <summary>
    ///     Uploads instance data from a span to the GPU.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the data (e.g., Vector3 for positions).</typeparam>
    /// <param name="buffer">The instance buffer to upload to.</param>
    /// <param name="flag">Which instance attribute to upload (Position, Rotation, Scale, or Color).</param>
    /// <param name="offset">Starting index in the buffer to upload to.</param>
    /// <param name="data">The source data span.</param>
    /// <param name="count">Number of elements to upload. If null, uploads the entire span.</param>
    /// <example>
    ///     <code>
    /// Span&lt;Vector3&gt; positions = stackalloc Vector3[1000];
    /// // ... fill positions ...
    /// R3D.UploadInstances(instances, InstanceFlags.Position, 0, positions, activeCount);
    /// </code>
    /// </example>
    public static void UploadInstances<T>(InstanceBuffer buffer, InstanceFlags flag, int offset, ReadOnlySpan<T> data, int? count = null) where T : unmanaged
    {
        int length = count ?? data.Length;
        if (length == 0) return;
        fixed (T* ptr = data)
        {
            UploadInstances(buffer, flag, offset, length, (IntPtr)ptr);
        }
    }

    // ---- Generic uniform convenience overloads ----

    /// <summary>
    ///     Sets a uniform value on a screen shader using a typed ref instead of void*.
    /// </summary>
    /// <typeparam name="T">The unmanaged uniform type (e.g., float, Vector2, Matrix4x4).</typeparam>
    /// <param name="shader">Target screen shader.</param>
    /// <param name="name">Name of the uniform.</param>
    /// <param name="value">Reference to the uniform value.</param>
    public static void SetScreenShaderUniform<T>(ScreenShader shader, string name, ref T value) where T : unmanaged
    {
        SetScreenShaderUniform(shader, name, Unsafe.AsPointer(ref value));
    }

    /// <summary>
    ///     Sets a uniform value on a surface shader using a typed ref instead of void*.
    /// </summary>
    /// <typeparam name="T">The unmanaged uniform type (e.g., float, Vector2, Matrix4x4).</typeparam>
    /// <param name="shader">Target surface shader.</param>
    /// <param name="name">Name of the uniform.</param>
    /// <param name="value">Reference to the uniform value.</param>
    public static void SetSurfaceShaderUniform<T>(SurfaceShader shader, string name, ref T value) where T : unmanaged
    {
        SetSurfaceShaderUniform(shader, name, Unsafe.AsPointer(ref value));
    }

    /// <summary>
    ///     Creates a 3D mesh from CPU-side mesh data, computing the bounding box automatically.
    /// </summary>
    /// <param name="type">Primitive type used to interpret vertex data.</param>
    /// <param name="data">MeshData containing vertices and indices.</param>
    /// <param name="usage">Hint on how the mesh will be used.</param>
    /// <returns>Created Mesh.</returns>
    public static Mesh LoadMesh(PrimitiveType type, MeshData data, MeshUsage usage)
        => LoadMesh(type, data, null, usage);

    /// <summary>
    ///     Creates a 3D mesh from CPU-side mesh data with an explicit bounding box.
    /// </summary>
    /// <param name="type">Primitive type used to interpret vertex data.</param>
    /// <param name="data">MeshData containing vertices and indices.</param>
    /// <param name="aabb">Bounding box for the mesh.</param>
    /// <param name="usage">Hint on how the mesh will be used.</param>
    /// <returns>Created Mesh.</returns>
    public static Mesh LoadMesh(PrimitiveType type, MeshData data, ref BoundingBox aabb, MeshUsage usage)
    {
        fixed (BoundingBox* ptr = &aabb)
            return LoadMesh(type, data, ptr, usage);
    }

    /// <summary>
    ///     Updates an existing mesh with new CPU-side data, computing the bounding box automatically.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="data">New MeshData to upload.</param>
    /// <returns>True if the update was successful.</returns>
    public static bool UpdateMesh(ref Mesh mesh, MeshData data)
        => UpdateMesh(ref mesh, data, null);

    /// <summary>
    ///     Updates an existing mesh with new CPU-side data and an explicit bounding box.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="data">New MeshData to upload.</param>
    /// <param name="aabb">Bounding box for the mesh.</param>
    /// <returns>True if the update was successful.</returns>
    public static bool UpdateMesh(ref Mesh mesh, MeshData data, ref BoundingBox aabb)
    {
        fixed (BoundingBox* ptr = &aabb)
            return UpdateMesh(ref mesh, data, ptr);
    }

    /// <summary>
    ///     Sets the screen shader chain from a span of shaders.
    /// </summary>
    public static void SetScreenShaderChain(ReadOnlySpan<ScreenShader> shaders)
    {
        fixed (ScreenShader* ptr = shaders)
            SetScreenShaderChain(ptr, shaders.Length);
    }
}
