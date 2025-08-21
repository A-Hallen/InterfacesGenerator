using System.Text;

namespace GeneradorInterfaces;

public static class TypeMapper
{
    public static Dictionary<string, string> TypeMappings = new()
    {
        { "string", "string" },
        { "int", "number" },
        { "long", "number" },
        { "decimal", "number" },
        { "double", "number" },
        { "float", "number" },
        { "bool", "boolean" },
        { "DateTime", "string" },
        { "Guid", "string" },
        { "object", "any" },
        { "dynamic", "any" },
        { "Dictionary<string, string>", "Record<string, string>" },
        { "Dictionary<string, int>", "Record<string, number>" },
        { "Dictionary<string, bool>", "Record<string, boolean>" }
    };

    public static string MapCSharpTypeToTypeScript(string csharpType, HashSet<string> imports, string currentDirectory)
    {
        if (csharpType.EndsWith("[]"))
        {
            var elementType = csharpType.Substring(0, csharpType.Length - 2);
            var tsElementType = MapCSharpTypeToTypeScript(elementType, imports, currentDirectory);
            return $"{tsElementType}[]";
        }

        if (csharpType.EndsWith("?"))
        {
            var nonNullableType = csharpType.Substring(0, csharpType.Length - 1);
            var tsNonNullableType = MapCSharpTypeToTypeScript(nonNullableType, imports, currentDirectory);
            return $"{tsNonNullableType} | null";
        }

        if (csharpType.Contains("<") && csharpType.Contains(">"))
        {
            var genericStart = csharpType.IndexOf('<');
            var genericEnd = csharpType.LastIndexOf('>');
            
            if (genericStart != -1 && genericEnd != -1)
            {
                var genericType = csharpType.Substring(0, genericStart);
                var typeArguments = csharpType.Substring(genericStart + 1, genericEnd - genericStart - 1);

                if (genericType == "Dictionary")
                {
                    var args = typeArguments.Split(',');
                    if (args.Length == 2)
                    {
                        var keyType = MapCSharpTypeToTypeScript(args[0].Trim(), imports, currentDirectory);
                        var valueType = MapCSharpTypeToTypeScript(args[1].Trim(), imports, currentDirectory);
                        return $"Record<{keyType}, {valueType}>";
                    }
                }

                if (genericType == "List" || genericType == "IEnumerable" || genericType == "ICollection" || genericType == "IList")
                {
                    var elementType = MapCSharpTypeToTypeScript(typeArguments.Trim(), imports, currentDirectory);
                    return $"{elementType}[]";
                }
            }
        }

        if (TypeMappings.TryGetValue(csharpType, out var tsType))
        {
            return tsType;
        }

        if (csharpType != "Unit" && !IsBasicType(csharpType))
        {
            var typeDirectory = FindTypeDirectory(csharpType);
            if (!string.IsNullOrEmpty(typeDirectory) && typeDirectory != currentDirectory)
            {
                var relativePath = PathUtils.GetRelativePath(currentDirectory, typeDirectory);
                imports.Add($"import {{ {csharpType} }} from '{relativePath}/{Path.GetFileName(typeDirectory)}';");
            }
        }

        if (csharpType == "Unit")
        {
            return "void";
        }

        return csharpType;
    }

    private static bool IsBasicType(string typeName)
    {
        return typeName == "string" || typeName == "number" || typeName == "boolean" || typeName == "any" ||
               typeName == "void" || typeName == "null" || typeName == "undefined" || typeName == "object";
    }

    private static string FindTypeDirectory(string typeName)
    {
        return string.Empty;
    }

    public static string CamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}
