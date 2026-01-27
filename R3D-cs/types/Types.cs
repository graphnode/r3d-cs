using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;

// ReSharper disable InconsistentNaming

namespace R3d_cs;

[StructLayout(LayoutKind.Sequential)]
public struct R3D_AlbedoMap
{
    public Texture2D texture;
    public Color color;
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EmissionMap
{
    public Texture2D texture;
    public Color color;
    public float energy;
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_NormalMap
{
    public Texture2D texture;
    public float scale;
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_OrmMap
{
    public Texture2D texture;
    public float occlusion;
    public float roughness;
    public float metalness;
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Material
{
    public R3D_AlbedoMap albedo;
    public R3D_EmissionMap emission;
    public R3D_NormalMap normal;
    public R3D_OrmMap orm;
    
    public R3D_TransparencyMode transparencyMode; // enum as int
    public R3D_BillboardMode billboardMode;       // enum as int
    public R3D_BlendMode blendMode;               // enum as int
    public R3D_CullMode cullMode;                 

    public Vector2 uvOffset;
    public Vector2 uvScale;
    public float alphaCutoff;
    // Note: actual header may contain additional fields; extend if needed.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Mesh
{
    public uint vao, vbo, ebo;                     // OpenGL objects handles.
    public int vertexCount, indexCount;            // Number of vertices and indices currently in use.
    public int allocVertexCount, allocIndexCount;  // Number of vertices and indices allocated in GPU buffers.
    public R3D_ShadowCastMode shadowCastMode;      // Shadow casting mode for the mesh.
    public R3D_PrimitiveType primitiveType;        // Type of primitive that constitutes the vertices.
    public R3D_MeshUsage usage;                    // Hint about the usage of the mesh, retained in case of update if there is a reallocation.
    public R3D_Layer layerMask;                    // Bitfield indicating the rendering layer(s) of this mesh.
    public BoundingBox aabb;                       // Axis-Aligned Bounding Box in local space.
}

public struct R3D_Light { public int id; }

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Cubemap {     
    public uint texture;
    public uint fbo;
    public int size;
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvBackground {
    public Color color;            // Background color when there is no skybox
    public float energy;           // Energy multiplier applied to background (skybox or color)
    public float skyBlur;          // Sky blur factor [0,1], based on mipmaps, very fast
    public R3D_Cubemap sky;        // Skybox asset (used if ID is non-zero)
    public Quaternion rotation;    // Skybox rotation (pitch, yaw, roll as quaternion)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_AmbientMap {
    public R3D_AmbientFlags flags;  // Components generated for this map
    public uint irradiance;         // Diffuse IBL cubemap (may be 0 if not generated)
    public uint prefilter;          // Specular prefiltered cubemap (may be 0 if not generated)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvAmbient {
    public Color color;            // Ambient light color when there is no ambient map
    public float energy;           // Energy multiplier for ambient light (map or color)
    public R3D_AmbientMap map;     // IBL environment map, can be generated from skybox
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvSSAO {
    public int sampleCount;        // Number of samples to compute SSAO (default: 16)
    public float intensity;        // Base occlusion strength multiplier (default: 1.0)
    public float power;            // Exponential falloff for sharper darkening (default: 1.5)
    public float radius;           // Sampling radius in world space (default: 0.25)
    public float bias;             // Depth bias to prevent self-shadowing, good value is ~2% of the radius (default: 0.007)
    [MarshalAs(UnmanagedType.I1)]
    public bool enabled;           // Enable/disable SSAO effect (default: false)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvSSIL {
    public int sampleCount;         // Number of samples to compute indirect lighting (default: 4)
    public int sliceCount;          // Number of depth slices for accumulation (default: 4)
    public float sampleRadius;      // Maximum distance to gather light from (default: 5.0)
    public float hitThickness;      // Thickness threshold for occluders (default: 0.5)
    public float aoPower;           // Exponential falloff for visibility factor (too high = more noise) (default: 1.0)
    public float energy;            // Multiplier for indirect light intensity (default: 1.0)
    public float bounce;            // Bounce feeback factor. (default: 0.5)
                                    // Simulates light bounces by re-injecting the SSIL from the previous frame into the current direct light.
                                    //  Be careful not to make the factor too high in order to avoid a feedback loop.
    public float convergence;       // Temporal convergence factor (0 disables it, default 0.5).
                                    //  Smooths sudden light flashes by blending with previous frames.
                                    //  Higher values produce smoother results but may cause ghosting.
                                    //  Tip: The faster the screen changes, the higher the convergence can be acceptable.
                                    //  Requires an additional history buffer (so require more memory).
                                    //  If multiple SSIL passes are done in the same frame, the history may be inconsistent,
                                    //  in that case, enable SSIL/convergence for only one pass per frame.
    [MarshalAs(UnmanagedType.I1)]
    public bool enabled;            // Enable/disable SSIL effect (default: false)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvBloom {
    public R3D_Bloom mode;         // Bloom blending mode (default: R3D_BLOOM_DISABLED)
    public float levels;           // Mipmap spread factor [0-1]: higher = wider glow (default: 0.5)
    public float intensity;        // Bloom strength multiplier (default: 0.05)
    public float threshold;        // Minimum brightness to trigger bloom (default: 0.0)
    public float softThreshold;    // Softness of brightness cutoff transition (default: 0.5)
    public float filterRadius;     // Blur filter radius during upscaling (default: 1.0)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvSSR {
    public int maxRaySteps;            // Maximum ray marching iterations (default: 64)
    public int binarySearchSteps;      // Refinement steps for intersection (default: 8)
    public float rayMarchLength;       // Maximum ray distance in view space (default: 8.0)
    public float depthThickness;       // Depth tolerance for valid hits (default: 0.2)
    public float depthTolerance;       // Negative margin to prevent false negatives (default: 0.005)
    public float edgeFadeStart;        // Screen edge fade start [0-1] (default: 0.7)
    public float edgeFadeEnd;          // Screen edge fade end [0-1] (default: 1.0)
    [MarshalAs(UnmanagedType.I1)]
    public bool enabled;               // Enable/disable SSR (default: false)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvFog {
    public R3D_Fog mode;           // Fog distribution mode (default: R3D_FOG_DISABLED)
    public Color color;            // Fog tint color (default: white)
    public float start;            // Linear mode: distance where fog begins (default: 1.0)
    public float end;              // Linear mode: distance of full fog density (default: 50.0)
    public float density;          // Exponential modes: fog thickness factor (default: 0.05)
    public float skyAffect;        // Fog influence on skybox [0-1] (default: 0.5)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvTonemap {
    public R3D_Tonemap mode;       // Tone mapping algorithm (default: R3D_TONEMAP_LINEAR)
    public float exposure;         // Scene brightness multiplier (default: 1.0)
    public float white;            // Reference white point (not used for AGX) (default: 1.0)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvColor {
    public float brightness;       // Overall brightness multiplier (default: 1.0)
    public float contrast;         // Contrast between dark and bright areas (default: 1.0)
    public float saturation;       // Color intensity (default: 1.0)
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_EnvDoF {
    public R3D_DoF mode;                    // Enable/disable state (default: R3D_DOF_DISABLED)
    public float focusPoint;                // Focus distance in meters from camera (default: 10.0)
    public float focusScale;                // Depth of field depth: lower = shallower (default: 1.0)
    public float maxBlurSize;               // Maximum blur radius, similar to aperture (default: 20.0)
    [MarshalAs(UnmanagedType.I1)]
    public bool debugMode;                  // Color-coded visualization: green=near, blue=far (default: false)
}

// The top-level environment (order matters and must match native header)
[StructLayout(LayoutKind.Sequential)]
public struct R3D_Environment {
    public R3D_EnvBackground background;    // Background and skybox settings
    public R3D_EnvAmbient    ambient;       // Ambient lighting configuration
    public R3D_EnvSSAO       ssao;          // Screen space ambient occlusion
    public R3D_EnvSSIL       ssil;          // Screen space indirect lighting
    public R3D_EnvBloom      bloom;         // Bloom glow effect
    public R3D_EnvSSR        ssr;           // Screen space reflections
    public R3D_EnvFog        fog;           // Atmospheric fog
    public R3D_EnvDoF        dof;           // Depth of field focus effect
    public R3D_EnvTonemap    tonemap;       // HDR tone mapping
    public R3D_EnvColor      color;         // Color grading adjustments
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_InstanceBuffer {
    public unsafe fixed uint buffers[4];
    public int capacity;
    public int flags;
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Model
{
    public unsafe R3D_Mesh* meshes;                   // Array of meshes composing the model.
    public unsafe R3D_MeshData* meshData;             // Array of meshes data in RAM (optional, can be NULL).
    public unsafe R3D_Material* materials;            // Array of materials used by the model.
    public unsafe int* meshMaterials;                 // Array of material indices, one per mesh.

    public int meshCount;                             // Number of meshes.
    public int materialCount;                         // Number of materials.

    public BoundingBox aabb;                          // Axis-Aligned Bounding Box encompassing the whole model.
    public R3D_Skeleton skeleton;                     // Skeleton hierarchy and bind pose used for skinning (NULL if non-skinned).
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_MeshData
{
    public unsafe R3D_Vertex* vertices;         // Pointer to vertex data in CPU memory.
    public unsafe uint* indices;                // Pointer to index data in CPU memory.
    public int vertexCount;                     // Number of vertices.
    public int indexCount;                      // Number of indices.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Vertex
{
    public Vector3 position;                // The 3D position of the vertex in object space.
    public Vector2 texcoord;                // The 2D texture coordinates (UV) for mapping textures.
    public Vector3 normal;                  // The normal vector used for lighting calculations.
    public Color color;                     // Vertex color, in RGBA32.
    public Vector4 tangent;                 // The tangent vector, used in normal mapping (often with a handedness in w).
    public unsafe fixed int boneIds[4];     // Indices of up to 4 bones that influence this vertex (for skinning).
    public unsafe fixed float weights[4];   // Corresponding bone weights (should sum to 1.0). Defines the influence of each bone.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Skeleton
{
    public unsafe R3D_BoneInfo* bones;          // Array of bone descriptors defining the hierarchy and names.
    public int boneCount;                       // Total number of bones in the skeleton.

    public unsafe Matrix4x4* localBind;         // Bind pose matrices relative to parent
    public unsafe Matrix4x4* modelBind;         // Bind pose matrices in model/global space
    public unsafe Matrix4x4* invBind;           // Inverse bind matrices (model space) for skinning
    public Matrix4x4 rootBind;                  // Root correction if local bind is not identity

    public uint skinTexture;                    // Texture ID that contains the bind pose for GPU skinning. This is a 1D Texture RGBA16F 4*boneCount.
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct R3D_BoneInfo {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string name;    // Bone name (max 31 characters + null terminator).
    public int parent;                                                          // Index of the parent bone (-1 if root).
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct R3D_CubemapSky
{
    public Color skyTopColor;          // Sky color at zenith
    public Color skyHorizonColor;      // Sky color at horizon
    public float skyHorizonCurve;      // Gradient curve exponent (0.01 - 1.0, typical: 0.15)
    public float skyEnergy;            // Sky brightness multiplier

    public Color groundBottomColor;    // Ground color at nadir
    public Color groundHorizonColor;   // Ground color at horizon
    public float groundHorizonCurve;   // Gradient curve exponent (typical: 0.02)
    public float groundEnergy;         // Ground brightness multiplier

    public Vector3 sunDirection;       // Direction from which light comes (can take not normalized)
    public Color sunColor;             // Sun disk color
    public float sunSize;              // Sun angular size in radians (real sun: ~0.0087 rad = 0.5°)
    public float sunCurve;             // Sun edge softness exponent (typical: 0.15)
    public float sunEnergy;            // Sun brightness multiplier
}


[StructLayout(LayoutKind.Sequential)]
public struct R3D_AnimationLib
{
    public unsafe R3D_Animation* animations;    // Array of animations included in this library.
    public int count;                           // Number of animations contained in the library.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_AnimationTrack
{
    public unsafe float* times;     // Keyframe times (sorted, in animation ticks).
    public unsafe void*  values;    // Keyframe values (Vector3 or Quaternion).
    public int count;               // Number of keyframes.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_AnimationChannel
{
    public R3D_AnimationTrack translation; // Translation track (Vector3).
    public R3D_AnimationTrack rotation;    // Rotation track (Quaternion).
    public R3D_AnimationTrack scale;       // Scale track (Vector3).
    public int boneIndex;                  // Index of the affected bone.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Animation
{
    public unsafe R3D_AnimationChannel* channels;       // Array of animation channels, one per animated bone.
    public int channelCount;                            // Total number of channels in this animation.
    public float ticksPerSecond;                        // Playback rate; number of animation ticks per second.
    public float duration;                              // Total length of the animation, in ticks.
    public int boneCount;                               // Number of bones in the target skeleton.
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] 
    public string name;                                 // Animation name (null-terminated string).
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_AnimationState
{
    public float currentTime;  // Current playback time in animation ticks.
    public float weight;       // Blending weight; any positive value is valid.
    public float speed;        // Playback speed; can be negative for reverse playback.
    [MarshalAs(UnmanagedType.I1)]
    public bool play;          // Whether the animation is currently playing.
    [MarshalAs(UnmanagedType.I1)]
    public bool loop;          // True to enable looping playback.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_AnimationPlayer
{
    public unsafe R3D_AnimationState* states;           // Array of active animation states, one per animation.
    public R3D_AnimationLib animLib;                    // Animation library providing the available animations.
    public R3D_Skeleton skeleton;                       // Skeleton to animate.

    public unsafe Matrix4x4* localPose;                 // Array of bone transforms representing the blended local pose.
    public unsafe Matrix4x4* modelPose;                 // Array of bone transforms in model space, obtained by hierarchical accumulation.
    public unsafe Matrix4x4* skinBuffer;                // Array of final skinning matrices (invBind * modelPose), sent to the GPU.
    public nint skinTexture;                            // GPU texture ID storing the skinning matrices as a 1D RGBA16F texture.

    public nint eventCallback;                          // TODO: Callback function to receive animation events.
    public nint eventUserData;                          // Optional user data pointer passed to the callback.
}

[StructLayout(LayoutKind.Sequential)]
public struct R3D_Decal {
    public R3D_AlbedoMap albedo;       // Albedo map (if the texture is undefined, implicitly treat `applyColor` as false, with alpha = 1.0)
    public R3D_EmissionMap emission;   // Emission map
    public R3D_NormalMap normal;       // Normal map
    public R3D_OrmMap orm;             // Occlusion-Roughness-Metalness map
    public Vector2 uvOffset;           // UV offset (default: {0.0f, 0.0f})
    public Vector2 uvScale;            // UV scale (default: {1.0f, 1.0f})
    public float alphaCutoff;          // Alpha cutoff threshold (default: 0.01f)
    public float normalThreshold;      // Maximum angle against the surface normal to draw decal. 0.0f disables threshold. (default: 0.0f)
    public float fadeWidth;            // The width of fading along the normal threshold (default: 0.0f)
    public bool applyColor;            // Indicates that the albedo color will not be rendered, only the alpha component of the albedo will be used as a mask. (default: true)
}

public enum R3D_Bloom {
    R3D_BLOOM_DISABLED,     // No bloom effect applied
    R3D_BLOOM_MIX,          // Linear interpolation blend between scene and bloom
    R3D_BLOOM_ADDITIVE,     // Additive blending, intensifying bright regions
    R3D_BLOOM_SCREEN        // Screen blending for softer highlight enhancement
}

public enum R3D_DoF {
    R3D_DOF_DISABLED,       // No depth of field effect
    R3D_DOF_ENABLED         // Depth of field enabled with focus point and blur
}

public enum R3D_Tonemap {
    R3D_TONEMAP_LINEAR,     // Direct linear mapping (no compression)
    R3D_TONEMAP_REINHARD,   // Reinhard operator, balanced HDR compression
    R3D_TONEMAP_FILMIC,     // Film-like response curve
    R3D_TONEMAP_ACES,       // Academy Color Encoding System (cinematic standard)
    R3D_TONEMAP_AGX,        // Modern algorithm preserving highlights and shadows
    R3D_TONEMAP_COUNT       // Internal: number of tonemap modes
}

public enum R3D_Fog {
    R3D_FOG_DISABLED,       // No fog effect
    R3D_FOG_LINEAR,         // Linear density increase between start and end distances
    R3D_FOG_EXP2,           // Exponential squared density (exp2), more realistic
    R3D_FOG_EXP             // Simple exponential density increase
}

public enum R3D_LightType
{
    R3D_LIGHT_DIR,                  // Directional light, affects the entire scene with parallel rays.
    R3D_LIGHT_SPOT,                 // Spot light, emits light in a cone shape.
    R3D_LIGHT_OMNI,                 // Omni light, emits light in all directions from a single point.
    R3D_LIGHT_TYPE_COUNT
}

public enum R3D_TransparencyMode
{
    R3D_TRANSPARENCY_DISABLED,      // No transparency, supports alpha cutoff.
    R3D_TRANSPARENCY_PREPASS,       // Supports transparency with shadows. Writes shadows for alpha > 0.1 and depth for alpha > 0.99.
    R3D_TRANSPARENCY_ALPHA,         // Standard transparency without shadows or depth writes.
}

public enum R3D_BillboardMode
{
    R3D_BILLBOARD_DISABLED,         // Billboarding is disabled; the object retains its original orientation.
    R3D_BILLBOARD_FRONT,            // Full billboarding; the object fully faces the camera, rotating on all axes.
    R3D_BILLBOARD_Y_AXIS            // Y-axis constrained billboarding; the object rotates only around the Y-axis,
                                    // keeping its "up" orientation fixed. This is suitable for upright objects like characters or signs.
}

public enum R3D_BlendMode
{
    R3D_BLEND_MIX,                  // Default mode: the result will be opaque or alpha blended depending on the transparency mode.
    R3D_BLEND_ADDITIVE,             // Additive blending: source color is added to the destination, making bright effects.
    R3D_BLEND_MULTIPLY,             // Multiply blending: source color is multiplied with the destination, darkening the image.
    R3D_BLEND_PREMULTIPLIED_ALPHA   // Premultiplied alpha blending: source color is blended with the destination assuming the source color is already multiplied by its alpha.
}

public enum R3D_CullMode
{
    R3D_CULL_NONE,              // No culling; all faces are rendered.
    R3D_CULL_BACK,              // Cull back-facing polygons (faces with clockwise winding order).
    R3D_CULL_FRONT              // Cull front-facing polygons (faces with counter-clockwise winding order).
}

public enum R3D_ShadowCastMode
{
    R3D_SHADOW_CAST_ON_AUTO,            // The object casts shadows; the faces used are determined by the material's culling mode.
    R3D_SHADOW_CAST_ON_DOUBLE_SIDED,    // The object casts shadows with both front and back faces, ignoring face culling.
    R3D_SHADOW_CAST_ON_FRONT_SIDE,      // The object casts shadows with only front faces, culling back faces.
    R3D_SHADOW_CAST_ON_BACK_SIDE,       // The object casts shadows with only back faces, culling front faces.
    R3D_SHADOW_CAST_ONLY_AUTO,          // The object only casts shadows; the faces used are determined by the material's culling mode.
    R3D_SHADOW_CAST_ONLY_DOUBLE_SIDED,  // The object only casts shadows with both front and back faces, ignoring face culling.
    R3D_SHADOW_CAST_ONLY_FRONT_SIDE,    // The object only casts shadows with only front faces, culling back faces.
    R3D_SHADOW_CAST_ONLY_BACK_SIDE,     // The object only casts shadows with only back faces, culling front faces.
    R3D_SHADOW_CAST_DISABLED            // The object does not cast shadows at all.
}

public enum R3D_ShadowUpdateMode {
    R3D_SHADOW_UPDATE_MANUAL,           // Shadow maps update only when explicitly requested.
    R3D_SHADOW_UPDATE_INTERVAL,         // Shadow maps update at defined time intervals.
    R3D_SHADOW_UPDATE_CONTINUOUS        // Shadow maps update every frame for real-time accuracy.
}

public enum R3D_PrimitiveType
{
    R3D_PRIMITIVE_POINTS,           // Each vertex represents a single point.
    R3D_PRIMITIVE_LINES,            // Each pair of vertices forms an independent line segment.
    R3D_PRIMITIVE_LINE_STRIP,       // Connected series of line segments sharing vertices.
    R3D_PRIMITIVE_LINE_LOOP,        // Closed loop of connected line segments.
    R3D_PRIMITIVE_TRIANGLES,        // Each set of three vertices forms an independent triangle.
    R3D_PRIMITIVE_TRIANGLE_STRIP,   // Connected strip of triangles sharing vertices.
    R3D_PRIMITIVE_TRIANGLE_FAN      // Fan of triangles sharing the first vertex.
}

public enum R3D_MeshUsage
{
    R3D_STATIC_MESH,            // Will never be updated.
    R3D_DYNAMIC_MESH,           // Will be updated occasionally.
    R3D_STREAMED_MESH           // Will be update on each frame.
}

[Flags]
public enum R3D_AmbientFlags
{
    R3D_AMBIENT_ILLUMINATION = 1 << 0,
    R3D_AMBIENT_REFLECTION   = 1 << 1
}

[Flags]
public enum R3D_Layer : uint
{
    R3D_LAYER_01 = 1 << 0,
    R3D_LAYER_02 = 1 << 1,
    R3D_LAYER_03 = 1 << 2,
    R3D_LAYER_04 = 1 << 3,
    R3D_LAYER_05 = 1 << 4,
    R3D_LAYER_06 = 1 << 5,
    R3D_LAYER_07 = 1 << 6,
    R3D_LAYER_08 = 1 << 7,
    R3D_LAYER_09 = 1 << 8,
    R3D_LAYER_10 = 1 << 9,
    R3D_LAYER_11 = 1 << 10,
    R3D_LAYER_12 = 1 << 11,
    R3D_LAYER_13 = 1 << 12,
    R3D_LAYER_14 = 1 << 13,
    R3D_LAYER_15 = 1 << 14,
    R3D_LAYER_16 = 1 << 15,

    R3D_LAYER_ALL = R3D_LAYER_01 | R3D_LAYER_02 | R3D_LAYER_03 | R3D_LAYER_04 | R3D_LAYER_05 | R3D_LAYER_06 | R3D_LAYER_07 | R3D_LAYER_08 | R3D_LAYER_09 | R3D_LAYER_10 |
                    R3D_LAYER_11 | R3D_LAYER_12 | R3D_LAYER_13 | R3D_LAYER_14 | R3D_LAYER_15 | R3D_LAYER_16
}


public enum R3D_CubemapLayout
{
    R3D_CUBEMAP_LAYOUT_AUTO_DETECT,             // Automatically detect layout type
    R3D_CUBEMAP_LAYOUT_LINE_VERTICAL,           // Layout is defined by a vertical line with faces
    R3D_CUBEMAP_LAYOUT_LINE_HORIZONTAL,         // Layout is defined by a horizontal line with faces
    R3D_CUBEMAP_LAYOUT_CROSS_THREE_BY_FOUR,     // Layout is defined by a 3x4 cross with cubemap faces
    R3D_CUBEMAP_LAYOUT_CROSS_FOUR_BY_THREE,     // Layout is defined by a 4x3 cross with cubemap faces
    R3D_CUBEMAP_LAYOUT_PANORAMA                 // Layout is defined by an equirectangular panorama
}

public struct R3D_Probe { public int id; }

[Flags]
public enum R3D_ProbeFlags
{
    R3D_PROBE_ILLUMINATION = 1 << 0,
    R3D_PROBE_REFLECTION   = 1 << 1
}

[Flags]
public enum R3D_InstanceFlags
{
    R3D_INSTANCE_POSITION  = 1 << 0,          // Vector3
    R3D_INSTANCE_ROTATION  = 1 << 1,          // Quaternion
    R3D_INSTANCE_SCALE     = 1 << 2,          // Vector3
    R3D_INSTANCE_COLOR     = 1 << 3,          // Color
}

public enum R3D_AntiAliasing
{
    R3D_ANTI_ALIASING_DISABLED, // Anti-aliasing is disabled. Edges may appear jagged.
    R3D_ANTI_ALIASING_FXAA,     // FXAA is applied. Smooths edges efficiently but may appear blurry.
}

public enum R3D_AspectMode
{
    R3D_ASPECT_EXPAND,      // Expands the rendered output to fully fill the target (render texture or window).
    R3D_ASPECT_KEEP         // Preserves the target's aspect ratio without distortion, adding empty gaps if necessary.
}

public enum R3D_UpscaleMode
{
    R3D_UPSCALE_NEAREST,    // Nearest-neighbor upscaling: very fast, but produces blocky pixels.
    R3D_UPSCALE_LINEAR,     // Bilinear upscaling: very fast, smoother than nearest, but can appear blurry.
    R3D_UPSCALE_BICUBIC,    // Bicubic (Catmull-Rom) upscaling: slower, smoother, and less blurry than linear.
    R3D_UPSCALE_LANCZOS     // Lanczos-2 upscaling: preserves more fine details, but is the most expensive.
}
