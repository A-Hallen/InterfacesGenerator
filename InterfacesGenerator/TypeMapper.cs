namespace InterfacesGenerator;

public static class TypeMapper
{
    private static readonly Dictionary<string, string> TypeMappings = new()
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
            var elementType = csharpType[..^2];
            var tsElementType = MapCSharpTypeToTypeScript(elementType, imports, currentDirectory);
            return $"{tsElementType}[]";
        }

        if (csharpType.EndsWith("?"))
        {
            var nonNullableType = csharpType[..^1];
            var tsNonNullableType = MapCSharpTypeToTypeScript(nonNullableType, imports, currentDirectory);
            return $"{tsNonNullableType} | null";
        }

        if (csharpType.Contains('<') && csharpType.Contains('>'))
        {
            var genericStart = csharpType.IndexOf('<');
            var genericEnd = csharpType.LastIndexOf('>');
            
            if (genericStart != -1 && genericEnd != -1)
            {
                var genericType = csharpType.Substring(0, genericStart);
                var typeArguments = csharpType.Substring(genericStart + 1, genericEnd - genericStart - 1);

                switch (genericType)
                {
                    case "Dictionary":
                    {
                        var args = typeArguments.Split(',');
                        if (args.Length == 2)
                        {
                            var keyType = MapCSharpTypeToTypeScript(args[0].Trim(), imports, currentDirectory);
                            var valueType = MapCSharpTypeToTypeScript(args[1].Trim(), imports, currentDirectory);
                            return $"Record<{keyType}, {valueType}>";
                        }

                        break;
                    }
                    case "List":
                    case "IEnumerable":
                    case "ICollection":
                    case "IList":
                    {
                        var elementType = MapCSharpTypeToTypeScript(typeArguments.Trim(), imports, currentDirectory);
                        return $"{elementType}[]";
                    }
                }
            }
        }

        if (TypeMappings.TryGetValue(csharpType, out var tsType))
        {
            return tsType;
        }

        return csharpType == "Unit" ? "void" : csharpType;
    }

    public static string CamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToLowerInvariant(input[0]) + input[1..];
    }
}
