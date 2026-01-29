using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CppAst;

namespace R3D_cs.GenerateBindings;

/// <summary>
///     Generates C# XML documentation comments from C++ Doxygen-style comments.
/// </summary>
public static class CommentGenerator
{
    // Cache for file contents to avoid re-reading
    private static readonly Dictionary<string, string[]> FileCache = new();

    /// <summary>
    ///     Generates XML documentation comments for C# from a CppAst comment.
    /// </summary>
    /// <param name="sb">StringBuilder to append the generated comments to.</param>
    /// <param name="comment">The CppAst comment to process.</param>
    /// <param name="originalName">The original C++ name to include in seealso.</param>
    /// <param name="prefix"></param>
    public static bool Generate(StringBuilder sb, CppComment? comment, string? originalName = null, string prefix = "")
    {
        var hasComment = false;

        if (comment != null)
        {
            string? briefText = null;
            var descriptionTexts = new List<string>();
            string? returnText = null;
            var paramTexts = new List<(string name, string description)>();
            var remarkTexts = new List<string>();
            var warningTexts = new List<string>();

            var foundBrief = false;
            var inParams = false;

            foreach (var child in comment.Children)
                switch (child)
                {
                    case CppCommentBlockCommand { CommandName: "brief" } cmd:
                        briefText = ExtractText(cmd);
                        foundBrief = true;
                        inParams = false;
                        break;

                    case CppCommentBlockCommand { CommandName: "note" or "remark" or "remarks" } cmd:
                        string remarkText = ExtractText(cmd);
                        if (!string.IsNullOrWhiteSpace(remarkText))
                            remarkTexts.Add(remarkText);
                        inParams = false;
                        break;

                    case CppCommentBlockCommand { CommandName: "warning" or "warn" } cmd:
                        string warningText = ExtractText(cmd);
                        if (!string.IsNullOrWhiteSpace(warningText))
                            warningTexts.Add(warningText);
                        inParams = false;
                        break;

                    case CppCommentBlockCommand { CommandName: "return" or "returns" } cmd:
                        returnText = ExtractText(cmd);
                        inParams = false;
                        break;

                    case CppCommentParamCommand paramCmd:
                        string paramDesc = ExtractText(paramCmd);
                        if (!string.IsNullOrWhiteSpace(paramDesc))
                            paramTexts.Add((paramCmd.ParamName, paramDesc));
                        inParams = true;
                        break;

                    case CppCommentParagraph paragraph:
                        string paragraphText = ExtractText(paragraph);
                        if (string.IsNullOrWhiteSpace(paragraphText))
                            break;

                        if (!foundBrief)
                        {
                            // Use first non-empty paragraph as brief if no @brief found
                            briefText = paragraphText;
                            foundBrief = true;
                        }
                        else if (!inParams)
                            // Additional paragraphs after brief go into description
                            descriptionTexts.Add(paragraphText);

                        break;
                }

            // Generate <summary>
            if (!string.IsNullOrWhiteSpace(briefText))
            {
                sb.AppendLine($"{prefix}/// <summary>");
                AppendMultilineText(sb, briefText, prefix);
                // Include additional description paragraphs in summary
                foreach (string desc in descriptionTexts)
                {
                    sb.AppendLine($"{prefix}/// <para>");
                    AppendMultilineText(sb, desc, prefix);
                    sb.AppendLine($"{prefix}/// </para>");
                }

                sb.AppendLine($"{prefix}/// </summary>");
                hasComment = true;
            }

            // Generate <param> elements
            foreach ((string name, string description) in paramTexts)
            {
                sb.AppendLine($"{prefix}/// <param name=\"{XmlEscape(name)}\">{XmlEscape(description)}</param>");
                hasComment = true;
            }

            // Generate <returns>
            if (!string.IsNullOrWhiteSpace(returnText))
            {
                sb.AppendLine($"{prefix}/// <returns>{XmlEscape(returnText)}</returns>");
                hasComment = true;
            }

            // Generate <remarks> (includes notes and warnings)
            if (remarkTexts.Count > 0 || warningTexts.Count > 0)
            {
                sb.AppendLine($"{prefix}/// <remarks>");

                // Warnings first with prefix
                foreach (string warning in warningTexts)
                {
                    sb.AppendLine($"{prefix}/// <b>Warning:</b>");
                    AppendMultilineText(sb, warning, prefix);
                }

                foreach (string remark in remarkTexts) AppendMultilineText(sb, remark, prefix);

                sb.AppendLine($"{prefix}/// </remarks>");
                hasComment = true;
            }
        }

        if (originalName != null)
        {
            sb.AppendLine($"{prefix}/// <seealso>{XmlEscape(originalName)}</seealso>");
            hasComment = true;
        }

        return hasComment;
    }

    /// <summary>
    ///     Extracts all text content from a comment node recursively.
    /// </summary>
    private static string ExtractText(CppComment comment)
    {
        var sb = new StringBuilder();
        ExtractTextRecursive(comment, sb);
        return sb.ToString().Trim();
    }

    private static void ExtractTextRecursive(CppComment comment, StringBuilder sb)
    {
        switch (comment)
        {
            case CppCommentText text:
                string trimmedText = text.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(trimmedText))
                    break;

                // Check if this looks like a list item (starts with - or *)
                bool isListItem = trimmedText.StartsWith("-") || trimmedText.StartsWith("*");

                if (sb.Length > 0)
                {
                    if (isListItem)
                        // List items get their own line
                        sb.AppendLine();
                    else if (!char.IsWhiteSpace(sb[^1]))
                        // Regular text gets a space separator
                        sb.Append(' ');
                }

                sb.Append(trimmedText);
                break;

            case CppCommentInlineCommand inlineCmd:
                // Handle inline commands like @c, @p, etc.
                if (!string.IsNullOrEmpty(inlineCmd.CommandName))
                    foreach (string? arg in inlineCmd.Arguments)
                        sb.Append(arg);
                break;

            default:
                foreach (var child in comment.Children) ExtractTextRecursive(child, sb);
                break;
        }
    }

    /// <summary>
    ///     Appends text that may contain multiple lines, prefixing each line appropriately.
    ///     Converts bullet lists (lines starting with - or *) to proper XML list elements.
    /// </summary>
    private static void AppendMultilineText(StringBuilder sb, string text, string prefix)
    {
        string[] lines = text.Split('\n');
        var inList = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                continue;

            // Check if this is a list item
            bool isListItem = trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*");

            if (isListItem)
            {
                if (!inList)
                {
                    sb.AppendLine($"{prefix}/// <list type=\"bullet\">");
                    inList = true;
                }

                // Remove the leading - or * and trim
                string itemText = trimmedLine[1..].TrimStart();
                sb.AppendLine($"{prefix}/// <item><description>{XmlEscape(itemText)}</description></item>");
            }
            else
            {
                if (inList)
                {
                    sb.AppendLine($"{prefix}/// </list>");
                    inList = false;
                }

                sb.AppendLine($"{prefix}/// {XmlEscape(trimmedLine)}");
            }
        }

        if (inList) sb.AppendLine($"{prefix}/// </list>");
    }

    /// <summary>
    ///     Escapes special XML characters.
    /// </summary>
    private static string XmlEscape(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    /// <summary>
    ///     Generates XML documentation comments for a macro by extracting the comment from the source file.
    ///     Priority: inline comment (same line) > block comment (above line)
    /// </summary>
    /// <param name="sb">StringBuilder to append the generated comments to.</param>
    /// <param name="macro">The CppMacro to extract comments for.</param>
    /// <param name="originalName">The original macro name.</param>
    /// <param name="prefix">Line prefix for indentation.</param>
    /// <returns>True if a comment was generated, false otherwise.</returns>
    public static bool GenerateForMacro(StringBuilder sb, CppMacro macro, string? originalName = null, string prefix = "")
    {
        var hasComment = false;

        if (!string.IsNullOrEmpty(macro.SourceFile))
        {
            // First try inline comment (same line) - has priority
            string? inlineComment = ExtractInlineComment(macro.SourceFile, macro.Span.Start.Line);
            if (inlineComment != null)
            {
                sb.AppendLine($"{prefix}/// <summary>{XmlEscape(inlineComment)}</summary>");
                hasComment = true;
            }
            else
            {
                // Fall back to block comment above the line
                string? comment = ExtractBlockCommentAbove(macro.SourceFile, macro.Span.Start.Line);
                if (comment != null)
                    hasComment = GenerateFromRawComment(sb, comment, prefix);
            }
        }

        if (originalName != null)
        {
            sb.AppendLine($"{prefix}/// <seealso>{XmlEscape($"{originalName}")}</seealso>");
            hasComment = true;
        }

        return hasComment;
    }

    /// <summary>
    ///     Extracts an inline comment from the same line (e.g., /*&lt; Vector3 */ or // comment)
    /// </summary>
    private static string? ExtractInlineComment(string filePath, int lineNumber)
    {
        if (!FileCache.TryGetValue(filePath, out string[]? lines))
        {
            if (!File.Exists(filePath))
                return null;
            lines = File.ReadAllLines(filePath);
            FileCache[filePath] = lines;
        }

        // Line numbers are 1-based, array is 0-based
        int lineIndex = lineNumber - 1;
        if (lineIndex < 0 || lineIndex >= lines.Length)
            return null;

        string line = lines[lineIndex];

        // Look for /* ... */ style inline comment
        int commentStart = line.IndexOf("/*", StringComparison.Ordinal);
        if (commentStart >= 0)
        {
            int commentEnd = line.IndexOf("*/", commentStart + 2, StringComparison.Ordinal);
            if (commentEnd > commentStart)
            {
                string comment = line[(commentStart + 2)..commentEnd].Trim();
                // Remove leading < (Doxygen trailing comment marker)
                if (comment.StartsWith("<"))
                    comment = comment[1..].TrimStart();
                if (!string.IsNullOrWhiteSpace(comment))
                    return comment;
            }
        }

        // Look for // style inline comment
        int slashComment = line.IndexOf("//", StringComparison.Ordinal);
        if (slashComment >= 0)
        {
            string comment = line[(slashComment + 2)..].Trim();
            // Remove leading < (Doxygen trailing comment marker)
            if (comment.StartsWith("<"))
                comment = comment[1..].TrimStart();
            if (!string.IsNullOrWhiteSpace(comment))
                return comment;
        }

        return null;
    }

    /// <summary>
    ///     Extracts the Doxygen comment block preceding a given line in a source file.
    /// </summary>
    private static string? ExtractBlockCommentAbove(string filePath, int lineNumber)
    {
        if (!FileCache.TryGetValue(filePath, out string[]? lines))
        {
            if (!File.Exists(filePath))
                return null;
            lines = File.ReadAllLines(filePath);
            FileCache[filePath] = lines;
        }

        // Line numbers are 1-based, array is 0-based
        int endLine = lineNumber - 2; // Line before the macro
        if (endLine < 0)
            return null;

        // Find the end of the comment block (looking for */)
        while (endLine >= 0 && string.IsNullOrWhiteSpace(lines[endLine]))
            endLine--;

        if (endLine < 0 || !lines[endLine].TrimEnd().EndsWith("*/"))
            return null;

        // Find the start of the comment block (looking for /**)
        int startLine = endLine;
        while (startLine >= 0)
        {
            string trimmed = lines[startLine].TrimStart();
            if (trimmed.StartsWith("/**") || trimmed.StartsWith("/*!"))
                break;
            startLine--;
        }

        if (startLine < 0)
            return null;

        // Extract and clean the comment
        var commentLines = new List<string>();
        for (int i = startLine; i <= endLine; i++)
        {
            string line = lines[i];
            // Remove comment markers
            line = line.Trim();
            if (line.StartsWith("/**") || line.StartsWith("/*!"))
                line = line[3..];
            else if (line.StartsWith("*/"))
                line = "";
            else if (line.StartsWith("*"))
                line = line[1..];

            if (line.EndsWith("*/"))
                line = line[..^2];

            commentLines.Add(line.Trim());
        }

        return string.Join("\n", commentLines);
    }

    /// <summary>
    ///     Generates XML documentation from a raw Doxygen comment string.
    /// </summary>
    /// <param name="sb">StringBuilder to append the generated comments to.</param>
    /// <param name="rawComment">The raw comment string.</param>
    /// <param name="prefix">Line prefix for indentation.</param>
    /// <returns>True if a comment was generated, false otherwise.</returns>
    private static bool GenerateFromRawComment(StringBuilder sb, string rawComment, string prefix = "")
    {
        string? briefText = null;
        var descriptionTexts = new List<string>();
        var remarkTexts = new List<string>();
        var warningTexts = new List<string>();

        // Parse the raw comment
        string[] lines = rawComment.Split('\n');
        var currentSection = "desc"; // "desc", "brief", "note", "warning"
        var currentText = new StringBuilder();

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                // Empty line might end current paragraph
                if (currentText.Length > 0)
                {
                    SaveCurrentSection(currentSection, currentText.ToString().Trim(), ref briefText, descriptionTexts, remarkTexts, warningTexts);
                    currentText.Clear();
                }

                continue;
            }

            // Check for Doxygen commands
            if (trimmed.StartsWith("@brief") || trimmed.StartsWith("\\brief"))
            {
                if (currentText.Length > 0)
                {
                    SaveCurrentSection(currentSection, currentText.ToString().Trim(), ref briefText, descriptionTexts, remarkTexts, warningTexts);
                    currentText.Clear();
                }

                currentSection = "brief";
                string text = trimmed.Length > 6 ? trimmed[6..].TrimStart() : "";
                if (!string.IsNullOrEmpty(text))
                    currentText.Append(text);
            }
            else if (trimmed.StartsWith("@note") || trimmed.StartsWith("\\note"))
            {
                if (currentText.Length > 0)
                {
                    SaveCurrentSection(currentSection, currentText.ToString().Trim(), ref briefText, descriptionTexts, remarkTexts, warningTexts);
                    currentText.Clear();
                }

                currentSection = "note";
                string text = trimmed.Length > 5 ? trimmed[5..].TrimStart() : "";
                if (!string.IsNullOrEmpty(text))
                    currentText.Append(text);
            }
            else if (trimmed.StartsWith("@warning") || trimmed.StartsWith("\\warning"))
            {
                if (currentText.Length > 0)
                {
                    SaveCurrentSection(currentSection, currentText.ToString().Trim(), ref briefText, descriptionTexts, remarkTexts, warningTexts);
                    currentText.Clear();
                }

                currentSection = "warning";
                string text = trimmed.Length > 8 ? trimmed[8..].TrimStart() : "";
                if (!string.IsNullOrEmpty(text))
                    currentText.Append(text);
            }
            else if (trimmed.StartsWith('@') || trimmed.StartsWith('\\'))
            {
                // Skip other commands like @typedef, @param, etc.
            }
            else
            {
                // Regular text - check if it's a list item
                if (trimmed.StartsWith('-') || trimmed.StartsWith('*'))
                {
                    currentText.AppendLine();
                    currentText.Append(trimmed);
                }
                else
                {
                    if (currentText.Length > 0)
                        currentText.Append(' ');
                    currentText.Append(trimmed);
                }
            }
        }

        // Save any remaining text
        if (currentText.Length > 0) SaveCurrentSection(currentSection, currentText.ToString().Trim(), ref briefText, descriptionTexts, remarkTexts, warningTexts);

        // Generate output
        if (!string.IsNullOrWhiteSpace(briefText))
        {
            sb.AppendLine($"{prefix}/// <summary>");
            AppendMultilineText(sb, briefText, prefix);
            foreach (string desc in descriptionTexts)
            {
                sb.AppendLine($"{prefix}/// <para>");
                AppendMultilineText(sb, desc, prefix);
                sb.AppendLine($"{prefix}/// </para>");
            }

            sb.AppendLine($"{prefix}/// </summary>");
        }

        if (remarkTexts.Count > 0 || warningTexts.Count > 0)
        {
            sb.AppendLine($"{prefix}/// <remarks>");
            foreach (string warning in warningTexts)
            {
                sb.AppendLine($"{prefix}/// <b>Warning:</b>");
                AppendMultilineText(sb, warning, prefix);
            }

            foreach (string remark in remarkTexts) AppendMultilineText(sb, remark, prefix);
            sb.AppendLine($"{prefix}/// </remarks>");
        }

        return true;
    }

    private static void SaveCurrentSection(string section, string text, ref string? briefText, List<string> descriptionTexts, List<string> remarkTexts, List<string> warningTexts)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        switch (section)
        {
            case "brief":
                briefText = text;
                break;
            case "desc":
                if (briefText == null)
                    briefText = text;
                else
                    descriptionTexts.Add(text);
                break;
            case "note":
                remarkTexts.Add(text);
                break;
            case "warning":
                warningTexts.Add(text);
                break;
        }
    }
}
