using System.Collections.Generic;
using System.Linq;

namespace R3D_cs.GenerateBindings;

/// <summary>
/// String manipulation utilities for code generation.
/// </summary>
public static class StringHelpers
{
    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate",
        "do", "double", "else", "enum", "event", "explicit", "extern", "false",
        "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
        "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private",
        "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    ];

    /// <summary>
    /// Strips the "R3D_" prefix from a name if present.
    /// </summary>
    public static string StripR3DPrefix(string name) => name.StartsWith("R3D_") ? name[4..] : name;

    /// <summary>
    /// Converts a SCREAMING_SNAKE_CASE or snake_case string to PascalCase.
    /// </summary>
    public static string ToPascalCase(string input)
    {
        string[] parts = input.Split('_');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            if (parts[i].All(char.IsUpper))
                parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..].ToLower();
            else
                parts[i] = char.ToUpper(parts[i][0]) + parts[i][1..];
        }
        return string.Join("", parts);
    }

    /// <summary>
    /// Escapes a name if it's a C# keyword by prefixing with @.
    /// </summary>
    public static string EscapeIdentifier(string name)
    {
        return CSharpKeywords.Contains(name) ? "@" + name : name;
    }
}
