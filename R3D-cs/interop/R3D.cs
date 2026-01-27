using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Raylib_cs;
using static Raylib_cs.Raylib;

[assembly: DisableRuntimeMarshalling]

namespace R3d_cs;

// ReSharper disable InconsistentNaming

[SuppressUnmanagedCodeSecurity]
public static unsafe partial class R3D
{
    /// <summary>
    /// Used by DllImport to load the native library
    /// </summary>
    public const string NativeLibName = "r3d";
    
    public const string R3D_VERSION = "0.7.0";
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_Init(int resWidth, int resHeight);

    [LibraryImport(NativeLibName)]
    public static partial R3D_Mesh R3D_GenMeshPlane(float width, float length, int resX, int resZ);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_Mesh R3D_GenMeshSphere(float radius, int rings, int slices);

    [LibraryImport(NativeLibName)]
    public static partial R3D_Mesh R3D_GenMeshCube(float width, float height, float length);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_Mesh R3D_GenMeshQuad(float width, float length, int resX, int resZ, Vector3 frontDir);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_Mesh R3D_GenMeshCylinder(float bottomRadius, float topRadius, float height, int slices);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_Material R3D_GetDefaultMaterial();

    // Create light (returns int id)
    [LibraryImport(NativeLibName)]
    public static partial R3D_Light R3D_CreateLight(R3D_LightType type); // type: R3D_LightType enum

    [LibraryImport(NativeLibName)]
    public static partial void R3D_LightLookAt(R3D_Light id, Vector3 position, Vector3 target);

    [LibraryImport(NativeLibName)]
    public static partial void R3D_EnableShadow(R3D_Light id);

    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetLightActive(R3D_Light id, [MarshalAs(UnmanagedType.I1)] bool active);

    [LibraryImport(NativeLibName)]
    public static partial void R3D_Begin(Camera3D camera);

    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawMesh(R3D_Mesh mesh, R3D_Material material, Vector3 position, float scale);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawMeshEx(R3D_Mesh mesh, R3D_Material material, Vector3 position, Quaternion rotation, Vector3 scale);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawModel(R3D_Model model, Vector3 position, float scale);

    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawDecal(R3D_Decal decal, Vector3 position, float scale);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawDecalEx(R3D_Decal decal, Vector3 position, Quaternion rotation, Vector3 scale);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawDecalInstanced(R3D_Decal decal, R3D_InstanceBuffer instances, int count);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_End();

    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadMesh(R3D_Mesh mesh);

    [LibraryImport(NativeLibName)]
    public static partial void R3D_Close();
    
    [LibraryImport(NativeLibName, EntryPoint = "R3D_GetEnvironment")]
    private static unsafe partial R3D_Environment* R3D_GetEnvironmentInternal();
    
    public static ref R3D_Environment R3D_GetEnvironment() => ref Unsafe.AsRef<R3D_Environment>(R3D_GetEnvironmentInternal());
    public static ref R3D_Environment R3D_ENVIRONMENT_GET => ref Unsafe.AsRef<R3D_Environment>( R3D_GetEnvironmentInternal());

    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetEnvironment(ref R3D_Environment env);
    
    public delegate void EnvironmentUpdater(ref R3D_Environment env);

    public static void R3D_ENVIRONMENT_SET(EnvironmentUpdater updater) => updater(ref R3D_GetEnvironment());

    
    public static R3D_Material R3D_MATERIAL_BASE =>
        new()
        {
            albedo = new R3D_AlbedoMap
            {
                texture = default,
                color = new Color(255, 255, 255, 255),
            },
            emission = new R3D_EmissionMap
            {
                texture = default,
                color = new Color(255, 255, 255, 255),
                energy = 0.0f,
            },
            normal = new R3D_NormalMap
            {
                texture = default,
                scale = 1.0f,
            },
            orm = new R3D_OrmMap
            {
                texture = default,
                occlusion = 1.0f,
                roughness = 1.0f,
                metalness = 0.0f,
            },
            transparencyMode = R3D_TransparencyMode.R3D_TRANSPARENCY_DISABLED,
            billboardMode = R3D_BillboardMode.R3D_BILLBOARD_DISABLED,
            blendMode = R3D_BlendMode.R3D_BLEND_MIX,
            cullMode = R3D_CullMode.R3D_CULL_BACK,
            uvOffset = Vector2.Zero,
            uvScale = Vector2.One,
            alphaCutoff = 0.01f,
        };

    public static R3D_CubemapSky R3D_CUBEMAP_SKY_BASE =>
        new()
        {
            skyTopColor = new Color(98, 116, 140, 255),
            skyHorizonColor = new Color(165, 167, 171, 255),
            skyHorizonCurve = 0.15f,
            skyEnergy = 1.0f,
            groundBottomColor = new Color(51, 43, 34, 255),
            groundHorizonColor = new Color(165, 167, 171, 255),
            groundHorizonCurve = 0.02f,
            groundEnergy = 1.0f,
            sunDirection = new Vector3(-1.0f, -1.0f, -1.0f),
            sunColor = new Color(255, 255, 255, 255),
            sunSize = 1.5f * DEG2RAD,
            sunCurve = 0.15f,
            sunEnergy = 1.0f,
        };

    public static R3D_Decal R3D_DECAL_BASE =>
        new()
        {
            albedo = new R3D_AlbedoMap
            {
                texture = default,
                color = new Color(255, 255, 255, 255),
            },
            emission = new R3D_EmissionMap
            {
                texture = default,
                color = new Color(255, 255, 255, 255),
                energy = 0.0f,
            },
            normal = new R3D_NormalMap
            {
                texture = default,
                scale = 1.0f,
            },
            orm = new R3D_OrmMap
            {
                texture = default,
                occlusion = 1.0f,
                roughness = 1.0f,
                metalness = 0.0f,
            },
            uvOffset = Vector2.Zero,
            uvScale = Vector2.One,
            alphaCutoff = 0.01f,
            normalThreshold = 0,
            fadeWidth = 0,
            applyColor = true
        };

    [LibraryImport(NativeLibName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial R3D_Cubemap R3D_LoadCubemap(string fileName, R3D_CubemapLayout layout);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_AmbientMap R3D_GenAmbientMap(R3D_Cubemap cubemap, R3D_AmbientFlags flags);
    
    [LibraryImport(NativeLibName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial R3D_AlbedoMap R3D_LoadAlbedoMap(string fileName, Color color);
    
    [LibraryImport(NativeLibName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial R3D_NormalMap R3D_LoadNormalMap(string fileName, float scale);

    [LibraryImport(NativeLibName)]
    public static partial R3D_Probe R3D_CreateProbe(R3D_ProbeFlags flags);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetProbePosition(R3D_Probe id, Vector3 position);
        
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetProbeShadows(R3D_Probe id, [MarshalAs(UnmanagedType.U1)] bool active);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetProbeFalloff(R3D_Probe id, float falloff);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetProbeActive(R3D_Probe id, [MarshalAs(UnmanagedType.U1)] bool active);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadAmbientMap(R3D_AmbientMap ambientMap);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadCubemap(R3D_Cubemap cubemap);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_InstanceBuffer R3D_LoadInstanceBuffer(int capacity, R3D_InstanceFlags flags);
    
    [LibraryImport(NativeLibName)]
    public static partial nint R3D_MapInstances(R3D_InstanceBuffer buffer, R3D_InstanceFlags flag);
    
    public static Span<T> R3D_MapInstances<T>(R3D_InstanceBuffer buffer, R3D_InstanceFlags flag) where T : unmanaged =>
        new((void*)R3D_MapInstances(buffer, flag), buffer.capacity * Unsafe.SizeOf<T>());
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UploadInstances(R3D_InstanceBuffer buffer, R3D_InstanceFlags flag, int offset, int count, void* data);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnmapInstances(R3D_InstanceBuffer buffer, R3D_InstanceFlags flags);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawLightShape(R3D_Light id);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetLightPosition(R3D_Light id, Vector3 position);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetLightDirection(R3D_Light id, Vector3 direction);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetLightColor(R3D_Light id, Color color);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetLightRange(R3D_Light id, float range);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetLightEnergy(R3D_Light id, float energy);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetShadowUpdateMode(R3D_Light id, R3D_ShadowUpdateMode mode);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetShadowDepthBias(R3D_Light id, float value);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetShadowSoftness(R3D_Light id, float softness);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawMeshInstanced(R3D_Mesh mesh, R3D_Material material, R3D_InstanceBuffer instances, int count);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadInstanceBuffer(R3D_InstanceBuffer buffer);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetAntiAliasing(R3D_AntiAliasing mode);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetTextureFilter(TextureFilter filter);
    
    [LibraryImport(NativeLibName, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial R3D_Model R3D_LoadModel(string filePath);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawModelPro(R3D_Model model, Matrix4x4 transform);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadModel(R3D_Model model, [MarshalAs(UnmanagedType.I1)] bool unloadMaterials);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadMaterial(R3D_Material material);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_Cubemap R3D_GenCubemapSky(int size, R3D_CubemapSky @params);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_AntiAliasing R3D_GetAntiAliasing();
    
    [LibraryImport(NativeLibName)]
    public static partial Texture2D R3D_GetBlackTexture();
    
    [LibraryImport(NativeLibName)]
    public static partial Vector3 R3D_GetLightPosition(R3D_Light id);
    
    [LibraryImport(NativeLibName, StringMarshalling = StringMarshalling.Utf8)]
    public static partial R3D_AnimationLib R3D_LoadAnimationLib(string filePath);
    
    [LibraryImport(NativeLibName)]
    public static partial R3D_AnimationPlayer R3D_LoadAnimationPlayer(R3D_Skeleton skeleton, R3D_AnimationLib animLib);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetAnimationWeight(ref R3D_AnimationPlayer player, int animIndex, float weight);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetAnimationLoop(ref R3D_AnimationPlayer player, int animIndex, [MarshalAs(UnmanagedType.I1)] bool loop);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_PlayAnimation(ref R3D_AnimationPlayer player, int animIndex);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UpdateAnimationPlayer(ref R3D_AnimationPlayer player, float dt);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawAnimatedModel(R3D_Model model, R3D_AnimationPlayer player, Vector3 position, float scale);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_DrawAnimatedModelInstanced(R3D_Model model, R3D_AnimationPlayer player, R3D_InstanceBuffer instances, int count);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadAnimationPlayer(R3D_AnimationPlayer player);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadAnimationLib(R3D_AnimationLib animLib);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetAspectMode(R3D_AspectMode mode);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_SetUpscaleMode(R3D_UpscaleMode mode);
    
    [LibraryImport(NativeLibName)]
    public static partial void R3D_UnloadDecalMaps(R3D_Decal decal);
}