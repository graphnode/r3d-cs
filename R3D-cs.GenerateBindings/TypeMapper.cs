using System.Collections.Generic;
using CppAst;

namespace R3D_cs.GenerateBindings;

/// <summary>
///     Maps C/C++ types to C# types.
/// </summary>
public static class TypeMapper
{
    /// <summary>
    ///     Types that should remain as pointers (opaque handles, optional parameters).
    /// </summary>
    private static readonly HashSet<string> KeepAsPointerTypes =
    [
        "R3D_Importer", "R3D_Environment", "R3D_BoundingBox", "BoundingBox"
    ];

    /// <summary>
    ///     Opaque types whose pointer is wrapped inside the struct as an nint handle.
    ///     For these types, Type* maps to Type (by value) and Type** maps to Type*.
    /// </summary>
    public static readonly HashSet<string> OpaqueTypes = [];

    /// <summary>
    ///     Maps a C++ type to its C# equivalent.
    /// </summary>
    /// <returns>A tuple of (csType, isUnsafe, isFixedBuffer, fixedSize).</returns>
    public static (string csType, bool isUnsafe, bool isFixedBuffer, int fixedSize) MapType(CppType type)
    {
        // Exceptions for Raylib types
        switch (type.FullName)
        {
            case "RenderTexture": return ("RenderTexture2D", false, false, 0);
            case "Texture": return ("Texture2D", false, false, 0);
        }

        return type switch
        {
            CppPrimitiveType pt => MapPrimitiveType(pt),
            CppPointerType ptr => MapPointerType(ptr),
            CppArrayType arr => (MapType(arr.ElementType).csType, true, true, arr.Size),
            CppQualifiedType qt => MapType(qt.ElementType),
            CppTypedef td => MapTypedefType(td.Name),
            CppClass cls => MapStructType(cls.Name),
            CppEnum en => ("R3D_" + StringHelpers.StripR3DPrefix(en.Name), false, false, 0),
            _ => ("IntPtr", false, false, 0)
        };
    }

    private static (string csType, bool isUnsafe, bool isFixedBuffer, int fixedSize) MapPrimitiveType(CppPrimitiveType pt)
    {
        return pt.Kind switch
        {
            CppPrimitiveKind.Void => ("void", false, false, 0),
            CppPrimitiveKind.Bool => ("bool", false, false, 0),
            CppPrimitiveKind.Char => ("byte", false, false, 0),
            CppPrimitiveKind.Short => ("short", false, false, 0),
            CppPrimitiveKind.Int => ("int", false, false, 0),
            CppPrimitiveKind.LongLong => ("long", false, false, 0),
            CppPrimitiveKind.UnsignedChar => ("byte", false, false, 0),
            CppPrimitiveKind.UnsignedShort => ("ushort", false, false, 0),
            CppPrimitiveKind.UnsignedInt => ("uint", false, false, 0),
            CppPrimitiveKind.UnsignedLongLong => ("ulong", false, false, 0),
            CppPrimitiveKind.Float => ("float", false, false, 0),
            CppPrimitiveKind.Double => ("double", false, false, 0),
            _ => ("IntPtr", false, false, 0)
        };
    }

    private static (string csType, bool isUnsafe, bool isFixedBuffer, int fixedSize) MapPointerType(CppPointerType ptr)
    {
        return ptr.ElementType switch
        {
            CppPrimitiveType { Kind: CppPrimitiveKind.Void } => ("IntPtr", false, false, 0),
            CppQualifiedType { ElementType: CppPrimitiveType { Kind: CppPrimitiveKind.Char } } => ("string", false, false, 0), // const char*
            _ when IsOpaqueType(ptr.ElementType) => (MapType(ptr.ElementType).csType, false, false, 0), // Type* → Type (by value)
            _ => (MapType(ptr.ElementType).csType + "*", true, false, 0)
        };
    }

    /// <summary>
    ///     Checks whether a type resolves to a known opaque type.
    /// </summary>
    private static bool IsOpaqueType(CppType type)
    {
        var unwrapped = type;
        if (unwrapped is CppQualifiedType qt)
            unwrapped = qt.ElementType;

        return unwrapped switch
        {
            CppClass cls => OpaqueTypes.Contains(cls.Name),
            CppTypedef td => OpaqueTypes.Contains(td.Name),
            _ => false
        };
    }

    private static (string csType, bool isUnsafe, bool isFixedBuffer, int fixedSize) MapTypedefType(string name)
    {
        // Keep R3D_ prefix for our types (will be stripped later)
        if (name.StartsWith("R3D_"))
            return ("R3D_" + StringHelpers.StripR3DPrefix(name), false, false, 0);

        // External types
        return (name, false, false, 0);
    }

    private static (string csType, bool isUnsafe, bool isFixedBuffer, int fixedSize) MapStructType(string name)
    {
        if (name.StartsWith("R3D_"))
            return ("R3D_" + StringHelpers.StripR3DPrefix(name), false, false, 0);

        if (name == "Matrix")
            return ("Matrix4x4", false, false, 0);

        return (name, false, false, 0);
    }

    /// <summary>
    ///     Determines if a pointer parameter should use ref/out modifiers instead of raw pointers.
    /// </summary>
    /// <returns>A tuple of (modifier, finalType) where modifier is "ref", "out", or null.</returns>
    public static (string? modifier, string finalType) GetParameterModifier(CppType type, string mappedType, string? functionName)
    {
        // Only process pointer types
        if (type is not CppPointerType ptrType)
            return (null, mappedType);

        // Get the unqualified element type
        var elementType = ptrType.ElementType;
        if (elementType is CppQualifiedType t)
            elementType = t.ElementType;

        // Keep void* / const void* as IntPtr (raw memory buffers)
        if (elementType is CppPrimitiveType { Kind: CppPrimitiveKind.Void })
            return (null, mappedType);

        // Keep const char* as string (already handled)
        if (elementType is CppPrimitiveType { Kind: CppPrimitiveKind.Char })
            return (null, mappedType);

        // Get the underlying type name
        string elementTypeName = ptrType.ElementType switch
        {
            CppTypedef td => td.Name,
            CppClass cls => cls.Name,
            CppQualifiedType qt => qt.ElementType switch
            {
                CppTypedef td2 => td2.Name,
                CppClass cls2 => cls2.Name,
                _ => ""
            },
            CppPrimitiveType pt => pt.Kind.ToString().ToLower(),
            _ => ""
        };

        // Types that should stay as pointers (opaque handles, optional parameters)
        if (KeepAsPointerTypes.Contains(elementTypeName))
            return (null, mappedType);

        // Primitive pointers in Get* functions become out parameters
        if (functionName?.StartsWith("R3D_Get") == true && ptrType.ElementType is CppPrimitiveType)
        {
            string baseType = mappedType.TrimEnd('*');
            return ("out", baseType);
        }

        // Pointer-to-pointer types (e.g., Type**) should remain as-is (not converted to ref)
        // Check both the mapped string and the CppType structure (opaque types collapse one level)
        if (mappedType.EndsWith("**") || ptrType.ElementType is CppPointerType)
            return (null, mappedType);

        // Other struct/type pointers become ref parameters
        if (mappedType.EndsWith('*'))
        {
            string baseType = mappedType.TrimEnd('*');
            return ("ref", baseType);
        }

        return (null, mappedType);
    }
}
