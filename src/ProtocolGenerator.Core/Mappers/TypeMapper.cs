using System.Text.RegularExpressions;
using ProtocolGenerator.Core.Models;

namespace ProtocolGenerator.Core.Mappers;

public class TypeMapper
{
    private readonly Dictionary<string, string> _primitiveMapping;
    private readonly Dictionary<string, TypeInfo> _customTypes;

    public TypeMapper(Dictionary<string, TypeInfo> customTypes)
    {
        _customTypes = customTypes;
        _primitiveMapping = new Dictionary<string, string>
        {
            // Signed integers
            ["char"] = "sbyte",
            ["int8_t"] = "sbyte",
            ["int16_t"] = "short",
            ["int32_t"] = "int",
            ["int64_t"] = "long",
            ["short"] = "short",
            ["int"] = "int",
            ["long"] = "int",
            ["long long"] = "long",

            // Unsigned integers
            ["unsigned char"] = "byte",
            ["uint8_t"] = "byte",
            ["uint16_t"] = "ushort",
            ["uint32_t"] = "uint",
            ["uint64_t"] = "ulong",
            ["unsigned short"] = "ushort",
            ["unsigned int"] = "uint",
            ["unsigned long"] = "uint",
            ["unsigned long long"] = "ulong",
            ["size_t"] = "nuint",

            // Floating point
            ["float"] = "float",
            ["double"] = "double",

            // Boolean
            ["bool"] = "bool",

            // Character
            ["wchar_t"] = "char",

            // Void pointer
            ["void *"] = "IntPtr",
        };
    }

    public string MapToCSharp(string cppType)
    {
        // Remove const, volatile qualifiers
        cppType = Regex.Replace(cppType, @"\b(const|volatile)\s+", "");
        cppType = cppType.Trim();

        // 1. Primitive type mapping
        if (_primitiveMapping.TryGetValue(cppType, out var primitiveType))
        {
            return primitiveType;
        }

        // 2. Pointer types
        if (cppType.EndsWith("*"))
        {
            var baseType = cppType.TrimEnd('*').Trim();

            // char* and const char* -> string
            if (baseType == "char" || baseType == "const char")
            {
                return "string";
            }

            return "IntPtr";
        }

        // 3. std::string
        if (cppType == "std::string" || cppType == "string")
        {
            return "string";
        }

        // 4. std::vector<T>
        var vectorMatch = Regex.Match(cppType, @"std::vector<(.+)>");
        if (vectorMatch.Success)
        {
            var elementType = vectorMatch.Groups[1].Value.Trim();
            var mappedElement = MapToCSharp(elementType);
            return $"List<{mappedElement}>";
        }

        // 5. std::array<T, N>
        var arrayMatch = Regex.Match(cppType, @"std::array<(.+),\s*(\d+)>");
        if (arrayMatch.Success)
        {
            var elementType = arrayMatch.Groups[1].Value.Trim();
            var mappedElement = MapToCSharp(elementType);
            // Fixed size array - will use FixedBuffer in C#
            return $"{mappedElement}[]";
        }

        // 6. C-style array T[N]
        var cArrayMatch = Regex.Match(cppType, @"(.+)\[(\d+)\]");
        if (cArrayMatch.Success)
        {
            var elementType = cArrayMatch.Groups[1].Value.Trim();
            var mappedElement = MapToCSharp(elementType);
            return $"{mappedElement}[]";
        }

        // 7. Resolve typedef
        if (_customTypes.TryGetValue(cppType, out var typeInfo))
        {
            if (typeInfo is TypedefInfo typedef)
            {
                return MapToCSharp(typedef.UnderlyingType);
            }
        }

        // 8. Custom types (struct/enum) - use as-is
        return cppType;
    }

    public string? GetMarshalAttribute(string cppType)
    {
        cppType = Regex.Replace(cppType, @"\b(const|volatile)\s+", "").Trim();

        // String marshaling
        if (cppType.EndsWith("char *") || cppType.EndsWith("char*"))
        {
            return "[MarshalAs(UnmanagedType.LPStr)]";
        }

        if (cppType == "std::string" || cppType == "string")
        {
            return "[MarshalAs(UnmanagedType.LPStr)]";
        }

        // Array marshaling
        if (cppType.StartsWith("std::vector"))
        {
            return "[MarshalAs(UnmanagedType.LPArray)]";
        }

        return null;
    }

    public bool IsFixedArray(string cppType, out int arraySize)
    {
        arraySize = 0;

        // std::array<T, N>
        var arrayMatch = Regex.Match(cppType, @"std::array<.+,\s*(\d+)>");
        if (arrayMatch.Success)
        {
            arraySize = int.Parse(arrayMatch.Groups[1].Value);
            return true;
        }

        // C-style array T[N]
        var cArrayMatch = Regex.Match(cppType, @".+\[(\d+)\]");
        if (cArrayMatch.Success)
        {
            arraySize = int.Parse(cArrayMatch.Groups[1].Value);
            return true;
        }

        return false;
    }
}
