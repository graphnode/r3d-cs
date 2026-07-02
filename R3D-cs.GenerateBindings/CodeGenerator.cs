using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppAst;
using static R3D_cs.GenerateBindings.StringHelpers;
using static R3D_cs.GenerateBindings.TypeMapper;

namespace R3D_cs.GenerateBindings;

/// <summary>
///     Generates C# binding code from parsed C++ headers.
/// </summary>
public class CodeGenerator(string outputDir)
{
    private const string Namespace = "R3D_cs";
    private const string NativeLibName = "r3d";

    /// <summary>
    ///     C# types that are legal element types for a <c>fixed</c> buffer.
    ///     Anything else (enums, structs like Vector4) needs an <c>[InlineArray]</c> wrapper.
    /// </summary>
    private static readonly HashSet<string> FixedBufferPrimitives =
    [
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "char"
    ];

    /// <summary>
    ///     Caches the full text of parsed header files so field/parameter source spellings
    ///     can be read from their source spans.
    /// </summary>
    private static readonly Dictionary<string, string> SourceFileCache = new();

    /// <summary>
    ///     Reads the original source text spanned by a parsed element (e.g. <c>int8_t normal[4]</c>).
    ///     Used to recover information lost during canonicalization, such as the distinction between
    ///     plain <c>char</c> (a string) and <c>int8_t</c>/<c>signed char</c> (numeric), which MSVC
    ///     collapses to the same <see cref="CppPrimitiveKind.Char"/>.
    /// </summary>
    private static string GetSourceSpelling(CppElement element)
    {
        try
        {
            var span = element.Span;
            string? file = span.Start.File;
            if (string.IsNullOrEmpty(file))
                return "";
            if (!SourceFileCache.TryGetValue(file, out string? text))
            {
                text = File.ReadAllText(file);
                SourceFileCache[file] = text;
            }
            int start = span.Start.Offset;
            int end = span.End.Offset;
            if (start < 0 || end <= start || end > text.Length)
                return "";
            return text.Substring(start, end - start);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    ///     Determines whether a char-typed field/parameter is actually a signed 8-bit numeric buffer
    ///     (<c>int8_t</c> / <c>signed char</c>) rather than a text string (<c>char</c>).
    /// </summary>
    private static bool IsSignedCharBuffer(CppElement element)
    {
        string s = GetSourceSpelling(element);
        return s.Contains("int8_t") || s.Contains("signed char");
    }

    /// <summary>
    ///     Rewrites C fixed-width integer constant macros (e.g. <c>UINT32_C(0xFFFFFFFF)</c>) into
    ///     plain C# literals that a C# enum initializer accepts.
    /// </summary>
    private static string SanitizeMacroValue(string value)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            value,
            @"\b(?:U?INT(?:8|16|32|64|MAX|PTR)_C)\s*\(\s*([^)]*?)\s*\)",
            "$1");
    }

    /// <summary>
    ///     Determines whether a function parameter is a signed 8-bit numeric buffer
    ///     (<c>int8_t</c> / <c>signed char</c>). Parameters carry no usable source span, so the
    ///     containing function's declaration text is inspected instead.
    /// </summary>
    private static bool IsSignedCharParam(CppFunction function, CppParameter param)
    {
        string decl = GetSourceSpelling(function);
        int open = decl.IndexOf('(');
        if (open < 0)
            return false;

        string args = decl[(open + 1)..];
        int close = args.LastIndexOf(')');
        if (close >= 0)
            args = args[..close];

        foreach (string segment in args.Split(','))
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(segment, $@"\b{System.Text.RegularExpressions.Regex.Escape(param.Name)}\b"))
                return segment.Contains("int8_t") || segment.Contains("signed char");
        }

        return false;
    }

    /// <summary>
    ///     Generates all binding files from the parsed compilation.
    /// </summary>
    public void Generate(CppCompilation compilation, string version)
    {
        // Identify opaque types (structs with no fields) before generating
        TypeMapper.OpaqueTypes.Clear();
        foreach (var @class in compilation.Classes)
        {
            if (!IsR3DSource(@class.SourceFile))
                continue;
            if (@class.Fields.Count == 0 &&
                @class.ClassKind is CppClassKind.Struct or CppClassKind.Union)
                TypeMapper.OpaqueTypes.Add(@class.Name);
        }

        if (TypeMapper.OpaqueTypes.Count > 0)
            Console.WriteLine($"Detected opaque types: {string.Join(", ", TypeMapper.OpaqueTypes)}");

        // Clear output directories
        Console.WriteLine("Preparing output directories...");
        ClearDirectory(Path.Combine(outputDir, "enums"));
        ClearDirectory(Path.Combine(outputDir, "types"));
        ClearDirectory(Path.Combine(outputDir, "interop"));

        // Generate files
        Console.WriteLine("Generating enum bindings...");
        int enumCount = GenerateEnumsFiles(compilation);
        Console.WriteLine($"  Generated {enumCount} enum files");

        Console.WriteLine("Generating struct bindings...");
        int structCount = GenerateStructsFiles(compilation);
        Console.WriteLine($"  Generated {structCount} struct files");

        Console.WriteLine("Generating misc bindings (typedefs, handles, callbacks)...");
        int miscCount = GenerateMiscFiles(compilation);
        Console.WriteLine($"  Generated {miscCount} misc files");

        Console.WriteLine("Generating interop file...");
        int funcCount = GenerateInteropFiles(compilation, version);
        Console.WriteLine($"  Generated interop file with {funcCount} functions");
    }

    private int GenerateEnumsFiles(CppCompilation compilation)
    {
        var count = 0;

        foreach (var @enum in compilation.Enums)
        {
            if (!IsR3DSource(@enum.SourceFile))
                continue;

            string name = StripR3DPrefix(@enum.Name);
            bool isBitflag = name.EndsWith("Flags");
            string enumName = isBitflag ? name[..^5] : name;

            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generated by R3D-cs.GenerateBindings");
            sb.AppendLine("// Do not edit manually");
            sb.AppendLine();
            sb.AppendLine($"namespace {Namespace};");
            sb.AppendLine();

            CommentGenerator.Generate(sb, @enum.Comment, @enum.Name);

            if (isBitflag)
                sb.AppendLine("[Flags]");

            sb.Append($"public enum {name}");
            (string type, _, _, _) = MapType(@enum.IntegerType);
            if (type != "int")
                sb.Append($" : {type}");
            sb.AppendLine();
            sb.AppendLine("{");

            string prefix = name;
            if (name.EndsWith("Mode", StringComparison.OrdinalIgnoreCase)) prefix = name[..^4];
            if (name.EndsWith("Type", StringComparison.OrdinalIgnoreCase)) prefix = name[..^4];
            if (name.EndsWith("Status", StringComparison.OrdinalIgnoreCase)) prefix = name[..^6];

            for (var i = 0; i < @enum.Items.Count; i++)
            {
                var item = @enum.Items[i];
                string itemName = ToPascalCase(StripR3DPrefix(item.Name));

                // Try stripping full name first, then shorter prefix (handles "Mode"/"Type" in values)
                if (itemName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    itemName = itemName[name.Length..];
                else if (itemName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    itemName = itemName[prefix.Length..];

                bool hasComment = CommentGenerator.Generate(sb, item.Comment, $"{@enum.Name}.{item.Name}", "    ");

                if (isBitflag)
                    sb.AppendLine($"    {itemName} = {item.Value},");
                else
                    sb.AppendLine($"    {itemName},");

                if (i < @enum.Items.Count - 1 && hasComment)
                    sb.AppendLine();
            }

            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(outputDir, "enums", $"{enumName}.g.cs"), sb.ToString());
            count++;
        }

        return count;
    }

    /// <summary>
    ///     Explicit count expressions for pointer fields whose counts live in nested structs.
    /// </summary>
    private static readonly Dictionary<(string structName, string cFieldName), string> ExplicitCountExpressions = new()
    {
        [("R3D_AnimationPlayer", "states")] = "AnimLib.Count",
        [("R3D_AnimationPlayer", "localPose")] = "Skeleton.BoneCount",
        [("R3D_AnimationPlayer", "modelPose")] = "Skeleton.BoneCount",
        [("R3D_AnimationPlayer", "skinBuffer")] = "Skeleton.BoneCount",
    };

    /// <summary>
    ///     Finds the C# count expression for a pointer field by prefix-matching count fields,
    ///     falling back to a generic "Count" field, a lone count field, or the explicit map.
    /// </summary>
    /// <summary>
    ///     Returns the number of leading characters shared by two strings (case-insensitive).
    /// </summary>
    private static int CommonPrefixLength(string a, string b)
    {
        int len = Math.Min(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            if (char.ToUpperInvariant(a[i]) != char.ToUpperInvariant(b[i]))
                return i;
        }
        return len;
    }

    private static string? FindCountExpression(
        string nativeStructName,
        string cFieldName,
        string csFieldName,
        List<(string csName, string cName)> countFields)
    {
        // 1. Check explicit overrides (for nested-struct counts like AnimLib.Count)
        if (ExplicitCountExpressions.TryGetValue((nativeStructName, cFieldName), out string? expr))
            return expr;

        // 2. Common-prefix match: field name minus "Count"/"Capacity" suffix must share a
        //    significant common prefix with the pointer field name.  When both a Count and
        //    Capacity field match, prefer Capacity (it represents the allocated size).
        string? bestMatch = null;
        var bestLen = 0;
        bool bestIsCapacity = false;
        foreach (var (csName, _) in countFields)
        {
            bool isCapacity = csName.EndsWith("Capacity", StringComparison.OrdinalIgnoreCase) && csName.Length > 8;
            bool isCount = csName.EndsWith("Count", StringComparison.OrdinalIgnoreCase) && csName.Length > 5;
            if (!isCapacity && !isCount)
                continue;

            string prefix = isCapacity ? csName[..^8] : csName[..^5];
            int commonLen = CommonPrefixLength(csFieldName, prefix);
            int threshold = Math.Max(3, prefix.Length - 2);

            if (commonLen >= threshold && (commonLen > bestLen || (commonLen == bestLen && isCapacity && !bestIsCapacity)))
            {
                bestMatch = csName;
                bestLen = commonLen;
                bestIsCapacity = isCapacity;
            }
        }
        if (bestMatch != null)
            return bestMatch;

        // 3. Exact "Count" field
        var exact = countFields.FirstOrDefault(c =>
            c.csName.Equals("Count", StringComparison.OrdinalIgnoreCase));
        if (exact.csName != null)
            return exact.csName;

        // 4. Lone count field (only one int*Count field in the struct → shared count)
        if (countFields.Count == 1)
            return countFields[0].csName;

        return null;
    }

    private int GenerateStructsFiles(CppCompilation compilation)
    {
        var count = 0;

        foreach (var @class in compilation.Classes)
        {
            if (!IsR3DSource(@class.SourceFile))
                continue;

            // Skip non-opaque unions (C unions with fields can't be represented as C# structs)
            // Opaque unions (no fields) are generated as wrapper structs with nint handle
            if (@class.ClassKind == CppClassKind.Union && @class.Fields.Count > 0)
                continue;

            if (@class.ClassKind is not (CppClassKind.Struct or CppClassKind.Union))
                throw new Exception($"Unexpected class kind: {@class.ClassKind}");

            bool isOpaque = TypeMapper.OpaqueTypes.Contains(@class.Name);

            var sb = new StringBuilder();
            var usings = isOpaque
                ? new List<string> { "System", "System.Numerics", "System.Runtime.InteropServices", "Raylib_cs" }
                : new List<string> { "System", "System.Numerics", "System.Runtime.InteropServices", "System.Text", "Raylib_cs" };
            GenerateHeader(sb, usings);

            string className = StripR3DPrefix(@class.Name);
            bool needsUnsafe = @class.Fields.Select(f => MapType(f.Type)).Any(r => r.isUnsafe);

            CommentGenerator.Generate(sb, @class.Comment, @class.Name);

            sb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
            sb.Append($"public {(needsUnsafe ? "unsafe " : "")}struct {className}");
            sb.AppendLine();
            sb.AppendLine("{");

            if (isOpaque)
            {
                sb.AppendLine("    private nint _handle;");
            }

            // --- Pass 1: collect field metadata ---
            var pointerFields = new List<(string emitName, string elementType, string originalName, string cFieldName)>();
            var voidPointerFields = new List<(string emitName, string originalName, string cFieldName)>();
            var countFields = new List<(string csName, string cName)>();
            var fieldInfos = new List<(CppField field, string fieldType, string fieldName, string emitName, string access, bool isFixedBuffer, int fixedSize)>();

            foreach (var field in @class.Fields)
            {
                (string csType, _, bool isFixedBuffer, int fixedSize) = MapType(field.Type);

                string fieldType = StripR3DPrefix(csType);
                string fieldName = EscapeIdentifier(ToPascalCase(field.Name));

                // Override for callbacks
                if (fieldName.EndsWith("Callback", StringComparison.OrdinalIgnoreCase))
                    fieldType = "IntPtr";

                // Typed pointer fields become internal with _ prefix
                bool isTypedPointer = !isFixedBuffer
                    && !isOpaque
                    && fieldType.Contains('*')
                    && fieldType != "void*";

                string access = isTypedPointer ? "internal" : "public";
                string emitName = isTypedPointer
                    ? $"_{char.ToLower(fieldName[0])}{fieldName[1..]}"
                    : fieldName;

                if (isTypedPointer)
                {
                    string elementType = fieldType.Replace("*", "").Trim();
                    pointerFields.Add((emitName, elementType, fieldName, field.Name));
                }

                if (!isFixedBuffer && fieldType == "void*")
                    voidPointerFields.Add((emitName, fieldName, field.Name));

                if (!isFixedBuffer && fieldType is "int" or "uint"
                    && (fieldName.EndsWith("Count", StringComparison.OrdinalIgnoreCase)
                        || fieldName.EndsWith("Capacity", StringComparison.OrdinalIgnoreCase)))
                    countFields.Add((fieldName, field.Name));

                fieldInfos.Add((field, fieldType, fieldName, emitName, access, isFixedBuffer, fixedSize));
            }

            // Determine which count fields are consumed by a Span property
            var usedCountFields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (_, _, originalName, cFieldName) in pointerFields)
            {
                string? countExpr = FindCountExpression(@class.Name, cFieldName, originalName, countFields);
                if (countExpr != null) usedCountFields.Add(countExpr);
            }
            foreach (var (_, originalName, cFieldName) in voidPointerFields)
            {
                string? countExpr = FindCountExpression(@class.Name, cFieldName, originalName, countFields);
                if (countExpr != null) usedCountFields.Add(countExpr);
            }

            // Inline-array wrapper structs to emit after the main struct (for fixed buffers of
            // non-primitive element types like Vector4 or enums, which C# `fixed` cannot hold).
            var inlineArrays = new List<(string bufferType, string elementType, int size)>();

            // --- Pass 2: emit fields ---
            foreach (var (field, fieldType, fieldName, emitName, access, isFixedBuffer, fixedSize) in fieldInfos)
            {
                // Count fields consumed by a Span property become internal
                string finalAccess = usedCountFields.Contains(fieldName) ? "internal" : access;

                // char[] arrays canonicalize to the same primitive kind as int8_t/signed char.
                // Only a genuine `char[]` is a string; `int8_t[]` is a signed numeric buffer.
                bool isCharArray = isFixedBuffer
                    && field.Type is CppArrayType { ElementType: CppPrimitiveType { Kind: CppPrimitiveKind.Char } };
                bool isSignedCharBuffer = isCharArray && IsSignedCharBuffer(field);
                bool isStringBuffer = isCharArray && !isSignedCharBuffer;

                // The signed numeric char buffer maps to sbyte instead of the default byte.
                string emitType = isSignedCharBuffer ? "sbyte" : fieldType;

                if (isStringBuffer)
                {
                    string backingName = $"_{char.ToLower(fieldName[0])}{fieldName[1..]}";
                    int maxLen = fixedSize - 1;
                    CommentGenerator.Generate(sb, field.Comment, field.Name, "    ");
                    sb.AppendLine($"    internal fixed {fieldType} {backingName}[{fixedSize}];");
                    sb.AppendLine();
                    sb.AppendLine($"    /// <inheritdoc cref=\"{backingName}\"/>");
                    sb.AppendLine($"    public string {fieldName}");
                    sb.AppendLine( "    {");
                    sb.AppendLine( "        get");
                    sb.AppendLine( "        {");
                    sb.AppendLine($"            fixed (byte* ptr = {backingName})");
                    sb.AppendLine( "            {");
                    sb.AppendLine($"                int len = 0;");
                    sb.AppendLine($"                while (len < {fixedSize} && ptr[len] != 0) len++;");
                    sb.AppendLine($"                return Encoding.UTF8.GetString(ptr, len);");
                    sb.AppendLine( "            }");
                    sb.AppendLine( "        }");
                    sb.AppendLine( "        set");
                    sb.AppendLine( "        {");
                    sb.AppendLine($"            byte[] utf8 = Encoding.UTF8.GetBytes(value);");
                    sb.AppendLine($"            int len = Math.Min(utf8.Length, {maxLen});");
                    sb.AppendLine($"            for (int i = 0; i < len; i++) {backingName}[i] = utf8[i];");
                    sb.AppendLine($"            {backingName}[len] = 0;");
                    sb.AppendLine( "        }");
                    sb.AppendLine( "    }");
                }
                else
                {
                    CommentGenerator.Generate(sb, field.Comment, field.Name, "    ");
                    if (isFixedBuffer && FixedBufferPrimitives.Contains(emitType))
                    {
                        sb.AppendLine($"    {finalAccess} fixed {emitType} {emitName}[{fixedSize}];");
                    }
                    else if (isFixedBuffer)
                    {
                        // C# `fixed` cannot hold non-primitive element types; use an [InlineArray] wrapper.
                        string bufferType = $"{className}{fieldName}Buffer";
                        sb.AppendLine($"    {finalAccess} {bufferType} {emitName};");
                        inlineArrays.Add((bufferType, emitType, fixedSize));
                    }
                    else
                    {
                        sb.AppendLine($"    {finalAccess} {emitType} {emitName};");
                    }
                }

                sb.AppendLine();
            }

            // Emit Span<T> properties for typed pointer + count pairs
            foreach (var (emitName, elementType, originalName, cFieldName) in pointerFields)
            {
                string? countExpr = FindCountExpression(@class.Name, cFieldName, originalName, countFields);
                if (countExpr == null) continue;

                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// <see cref=\"{originalName}\"/> as a <see cref=\"Span{{T}}\"/>.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public Span<{elementType}> {originalName} => {emitName} != null ? new({emitName}, {countExpr}) : default;");
                sb.AppendLine();
            }

            // Emit generic cast method for void* pointer + count pairs
            foreach (var (emitName, originalName, cFieldName) in voidPointerFields)
            {
                string? countExpr = FindCountExpression(@class.Name, cFieldName, originalName, countFields);
                if (countExpr == null) continue;

                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// <see cref=\"{originalName}\"/> cast to the specified type as a <see cref=\"Span{{T}}\"/>.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public Span<T> {originalName}As<T>() where T : unmanaged => {emitName} != null ? new((T*){emitName}, {countExpr}) : default;");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            // Emit [InlineArray] wrapper structs for fixed buffers of non-primitive element types.
            foreach (var (bufferType, elementType, size) in inlineArrays)
            {
                sb.AppendLine();
                sb.AppendLine($"/// <summary>Inline fixed-size buffer of {size} <see cref=\"{elementType}\"/> elements.</summary>");
                sb.AppendLine($"[System.Runtime.CompilerServices.InlineArray({size})]");
                sb.AppendLine($"public struct {bufferType}");
                sb.AppendLine("{");
                sb.AppendLine($"    private {elementType} _element0;");
                sb.AppendLine("}");
            }

            File.WriteAllText(Path.Combine(outputDir, "types", $"{className}.g.cs"), sb.ToString());
            count++;
        }

        return count;
    }

    private int GenerateMiscFiles(CppCompilation compilation)
    {
        var count = 0;

        foreach (var typedef in compilation.Typedefs)
        {
            if (!IsR3DSource(typedef.SourceFile))
                continue;

            string name = StripR3DPrefix(typedef.Name);

            // Callbacks (function pointers)
            if (typedef.Name.EndsWith("Callback"))
            {
                if (typedef.ElementType is CppPointerType { ElementType: CppFunctionType funcType })
                {
                    (string returnType, _, _, _) = MapType(funcType.ReturnType);

                    var sb = new StringBuilder();
                    GenerateHeader(sb, ["System", "System.Numerics", "System.Runtime.InteropServices", "Raylib_cs"]);

                    CommentGenerator.Generate(sb, typedef.Comment, typedef.Name);

                    sb.AppendLine("[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
                    sb.Append($"public unsafe delegate {returnType} {name}(");

                    for (var i = 0; i < funcType.Parameters.Count; i++)
                    {
                        var param = funcType.Parameters[i];

                        (string paramType, _, _, _) = MapType(param.Type);
                        sb.Append($"{StripR3DPrefix(paramType)} {EscapeIdentifier(param.Name)}");

                        if (i < funcType.Parameters.Count - 1)
                            sb.Append(", ");
                    }

                    sb.AppendLine(");");
                    sb.AppendLine();

                    File.WriteAllText(Path.Combine(outputDir, "types", $"{name}.g.cs"), sb.ToString());
                    count++;
                }

                continue;
            }

            // Typedef aliases for opaque struct types (e.g., typedef struct R3D_ShaderCustom R3D_ScreenShader)
            if (typedef.ElementType is CppClass aliasedClass && TypeMapper.OpaqueTypes.Contains(aliasedClass.Name))
            {
                var sb = new StringBuilder();
                GenerateHeader(sb, ["System", "System.Numerics", "System.Runtime.InteropServices", "Raylib_cs"]);

                CommentGenerator.Generate(sb, typedef.Comment, typedef.Name);

                sb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
                sb.AppendLine($"public struct {name}");
                sb.AppendLine("{");
                sb.AppendLine("    private nint _handle;");
                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(outputDir, "types", $"{name}.g.cs"), sb.ToString());
                count++;
                continue;
            }

            // Fixed-length char array typedefs (e.g. typedef char R3D_MeshName[32])
            // → struct wrapping a fixed byte buffer with a UTF-8 string accessor.
            if (typedef.ElementType is CppArrayType { ElementType: CppPrimitiveType { Kind: CppPrimitiveKind.Char } } charArray)
            {
                int size = charArray.Size;
                int maxLen = size - 1;

                var sb = new StringBuilder();
                GenerateHeader(sb, ["System", "System.Numerics", "System.Runtime.InteropServices", "System.Text", "Raylib_cs"]);

                CommentGenerator.Generate(sb, typedef.Comment, typedef.Name);

                sb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
                sb.AppendLine($"public unsafe struct {name}");
                sb.AppendLine("{");
                sb.AppendLine($"    internal fixed byte _value[{size}];");
                sb.AppendLine();
                sb.AppendLine("    /// <summary>The UTF-8 string stored in this fixed-length buffer.</summary>");
                sb.AppendLine("    public string Value");
                sb.AppendLine("    {");
                sb.AppendLine("        get");
                sb.AppendLine("        {");
                sb.AppendLine("            fixed (byte* ptr = _value)");
                sb.AppendLine("            {");
                sb.AppendLine("                int len = 0;");
                sb.AppendLine($"                while (len < {size} && ptr[len] != 0) len++;");
                sb.AppendLine("                return Encoding.UTF8.GetString(ptr, len);");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine("        set");
                sb.AppendLine("        {");
                sb.AppendLine("            byte[] utf8 = Encoding.UTF8.GetBytes(value);");
                sb.AppendLine($"            int len = Math.Min(utf8.Length, {maxLen});");
                sb.AppendLine("            for (int i = 0; i < len; i++) _value[i] = utf8[i];");
                sb.AppendLine("            _value[len] = 0;");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    /// <inheritdoc/>");
                sb.AppendLine("    public override string ToString() => Value;");
                sb.AppendLine();
                sb.AppendLine($"    public static implicit operator string({name} v) => v.Value;");
                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(outputDir, "types", $"{name}.g.cs"), sb.ToString());
                count++;
                continue;
            }

            // Check for macro-based enums
            bool isBitflag = name.EndsWith("Flags");
            string enumName = isBitflag ? name[..^5] : name;
            string prefix = "R3D_" + enumName.ToUpperInvariant() + "_";

            var options = new List<CppMacro>();
            foreach (var macro in compilation.Macros)
            {
                if (macro.SourceFile != typedef.SourceFile || macro.Span.Start.Line < typedef.Span.End.Line)
                    continue;

                if (!macro.Name.StartsWith(prefix))
                    continue;

                options.Add(macro);
            }

            if (options.Count > 0)
            {
                var sb = new StringBuilder();
                GenerateHeader(sb, ["System", "System.Numerics", "System.Runtime.InteropServices", "Raylib_cs"]);

                CommentGenerator.Generate(sb, typedef.Comment, typedef.Name);

                sb.AppendLine("[Flags]");
                sb.Append($"public enum {name}");
                (string type, _, _, _) = MapType(typedef.ElementType);
                if (type != "int")
                    sb.Append($" : {type}");
                sb.AppendLine();
                sb.AppendLine("{");

                for (var i = 0; i < options.Count; i++)
                {
                    var macro = options[i];

                    string optionName = ToPascalCase(macro.Name[prefix.Length..]);

                    if (optionName.All(char.IsDigit))
                        optionName = ToPascalCase(StripR3DPrefix(macro.Name));

                    if (optionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        optionName = optionName[prefix.Length..];

                    bool hasComment = CommentGenerator.GenerateForMacro(sb, macro, macro.Name, "    ");

                    sb.AppendLine($"    {optionName} = {SanitizeMacroValue(macro.Value)},");

                    if (i < options.Count - 1 && hasComment)
                        sb.AppendLine();
                }

                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(outputDir, "enums", $"{name}.g.cs"), sb.ToString());
                count++;
                continue;
            }

            // Opaque handle types (typedef int32_t R3D_Something)
            if (typedef.ElementType is CppPrimitiveType { Kind: CppPrimitiveKind.Int })
            {
                var sb = new StringBuilder();
                GenerateHeader(sb);

                CommentGenerator.Generate(sb, typedef.Comment, typedef.Name);

                sb.AppendLine($"public struct {name}");
                sb.AppendLine("{");
                sb.AppendLine("    internal int id;");
                sb.AppendLine();
                sb.AppendLine($"    public static explicit operator int({name} handle) => handle.id;");
                sb.AppendLine($"    public static explicit operator {name}(int id) => new() {{ id = id }};");
                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(outputDir, "types", $"{name}.g.cs"), sb.ToString());
                count++;
            }
        }

        return count;
    }

    private int GenerateInteropFiles(CppCompilation compilation, string version)
    {
        var funcCount = 0;

        var functions =  compilation.Functions
            .Where(f => IsR3DSource(f.SourceFile))
            .GroupBy(f => Path.GetFileNameWithoutExtension(f.SourceFile));

        foreach (var group in functions)
        {
            var sb = new StringBuilder();
            if (group.Key == "r3d_core")
            {
                GenerateHeader(sb,
                    ["System", "System.Numerics", "System.Runtime.CompilerServices", "System.Runtime.InteropServices", "System.Security", "Raylib_cs", "static Raylib_cs.Raylib"],
                    ["[assembly: DisableRuntimeMarshalling]"]
                );
            }
            else
            {
                GenerateHeader(sb,
                    ["System", "System.Numerics", "System.Runtime.CompilerServices", "System.Runtime.InteropServices", "System.Security", "Raylib_cs", "static Raylib_cs.Raylib"]
                );
            }

            sb.AppendLine($"// {group.Key}.h;");
            sb.AppendLine();
            sb.AppendLine("[SuppressUnmanagedCodeSecurity]");
            sb.AppendLine("public static unsafe partial class R3D");
            sb.AppendLine("{");
            sb.AppendLine();
            if (group.Key == "r3d_core")
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Used by DllImport to load the native library");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public const string NativeLibName = \"{NativeLibName}\";");
                sb.AppendLine();
                sb.AppendLine($"    public const string R3D_VERSION = \"{version}\";");
                sb.AppendLine();
            }

            foreach (var function in group)
            {
                string? name = function.Name;
                (string returnType, _, _, _) = MapType(function.ReturnType);
                bool hasStringParam = function.Parameters.Any(p => p.Type is CppPointerType
                {
                    ElementType: CppQualifiedType { ElementType: CppPrimitiveType { Kind: CppPrimitiveKind.Char } }
                } && !IsSignedCharParam(function, p));

                CommentGenerator.Generate(sb, function.Comment, name, "    ");

                sb.Append("    [LibraryImport(NativeLibName");
                sb.Append($", EntryPoint = \"{name}\"");
                if (hasStringParam)
                    sb.Append(", StringMarshalling = StringMarshalling.Utf8");
                sb.AppendLine(")]");

                if (returnType == "bool")
                    sb.AppendLine("    [return: MarshalAs(UnmanagedType.I1)]");

                sb.Append($"    public static partial {StripR3DPrefix(returnType)} {StripR3DPrefix(name)}(");
                for (var i = 0; i < function.Parameters.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var param = function.Parameters[i];
                    (string paramType, _, bool isArrayParam, _) = MapType(param.Type);

                    // int8_t / signed char buffers canonicalize to `char`; recover the signed
                    // numeric intent (const int8_t* would otherwise be marshalled as a string).
                    if (IsSignedCharParam(function, param))
                        paramType = paramType == "string" ? "sbyte*" : paramType.Replace("byte", "sbyte");

                    // Array parameters (e.g. Vector3 corners[8]) decay to pointers in C.
                    if (isArrayParam)
                        paramType += "*";

                    if (paramType == "bool")
                        sb.Append("[MarshalAs(UnmanagedType.I1)] ");

                    // Convert pointer parameters to ref/out where appropriate
                    (string? modifier, string finalType) = GetParameterModifier(param.Type, paramType, name);
                    if (modifier != null)
                        sb.Append($"{modifier} ");

                    sb.Append($"{StripR3DPrefix(finalType)} {EscapeIdentifier(StripR3DPrefix(param.Name))}");
                }

                sb.AppendLine(");");
                sb.AppendLine();

                funcCount++;
            }

            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(outputDir, "interop", $"R3D.{group.Key.Replace("r3d_", "")}.g.cs"), sb.ToString());
        }

        return funcCount;
    }

    private void GenerateHeader(StringBuilder sb, List<string>? includes = null, List<string>? other = null)
    {
        sb.AppendLine("// Auto-generated by R3D-cs.GenerateBindings");
        sb.AppendLine("// Do not edit manually");
        sb.AppendLine();
        if (includes != null)
        {
            foreach (string include in includes)
                sb.AppendLine($"using {include};");
            sb.AppendLine();
        }

        if (other != null)
        {
            foreach (string line in other)
                sb.AppendLine(line);
            sb.AppendLine();
        }

        sb.AppendLine($"namespace {Namespace};");
        sb.AppendLine();
    }

    private bool IsR3DSource(string? sourceFile)
    {
        if (sourceFile == null)
            return false;

        // Normalize path separators for cross-platform compatibility
        char sep = Path.DirectorySeparatorChar;
        string normalized = sourceFile.Replace('/', sep).Replace('\\', sep);
        return normalized.Contains($"include{sep}r3d{sep}");
    }

    private static void ClearDirectory(string directory)
    {
        if (Directory.Exists(directory))
            // Only delete generated files (*.g.cs), preserve hand-written files
            foreach (string file in Directory.GetFiles(directory, "*.g.cs"))
                File.Delete(file);
        else
            Directory.CreateDirectory(directory);
    }
}
