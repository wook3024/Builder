using Scriban;
using ProtocolGenerator.Core.Models;

namespace ProtocolGenerator.Core.Generators;

public class CSharpProtocolGenerator
{
    private readonly Template _namespaceTemplate;
    private readonly Template _structTemplate;
    private readonly Template _enumTemplate;

    public CSharpProtocolGenerator(string templatesPath = "templates")
    {
        _namespaceTemplate = LoadTemplate(Path.Combine(templatesPath, "csharp_namespace.scriban"));
        _structTemplate = LoadTemplate(Path.Combine(templatesPath, "csharp_struct.scriban"));
        _enumTemplate = LoadTemplate(Path.Combine(templatesPath, "csharp_enum.scriban"));
    }

    private Template LoadTemplate(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Template file not found: {path}");
        }
        return Template.Parse(File.ReadAllText(path));
    }

    public string Generate(Protocol protocol)
    {
        var enums = new List<string>();
        var structs = new List<string>();

        // Generate enums
        foreach (var enumInfo in protocol.Enums)
        {
            enums.Add(GenerateEnum(enumInfo));
        }

        // Generate messages as structs
        foreach (var message in protocol.Messages)
        {
            structs.Add(GenerateMessage(message));
        }

        var model = new
        {
            @namespace = protocol.Namespace ?? protocol.Name,
            enums = string.Join("\n\n", enums),
            structs = string.Join("\n\n", structs)
        };

        return _namespaceTemplate.Render(model);
    }

    private string GenerateEnum(EnumInfo enumInfo)
    {
        var underlyingType = MapToCSharp(enumInfo.UnderlyingType);

        var model = new
        {
            name = enumInfo.Name,
            underlying_type = underlyingType,
            show_underlying_type = underlyingType != "int",
            values = enumInfo.Values.Select(kvp => new
            {
                name = kvp.Key,
                value = kvp.Value
            }).ToArray()
        };

        return _enumTemplate.Render(model);
    }

    private string GenerateMessage(MessageInfo message)
    {
        var model = new
        {
            name = message.Name,
            fields = message.Fields.Select((f, index) => new
            {
                name = f.Name,
                type = GetCSharpType(f),
                marshal_attribute = GetMarshalAttribute(f),
                is_fixed_array = f.IsArray && f.ArraySize.HasValue,
                array_size = f.ArraySize ?? 0,
                offset = index * 8, // Simplified offset calculation
                original_type = f.Type
            }).ToArray()
        };

        return _structTemplate.Render(model);
    }

    private string GetCSharpType(MessageField field)
    {
        var baseType = MapToCSharp(field.Type);

        if (field.IsArray)
        {
            if (field.ArraySize.HasValue)
            {
                return $"{baseType}[]"; // Fixed-size array
            }
            else
            {
                return $"List<{baseType}>"; // Dynamic array
            }
        }

        return baseType;
    }

    private string? GetMarshalAttribute(MessageField field)
    {
        if (field.Type == "string")
        {
            return "[MarshalAs(UnmanagedType.LPStr)]";
        }

        return null;
    }

    private string MapToCSharp(string type)
    {
        return type switch
        {
            "byte" => "byte",
            "sbyte" => "sbyte",
            "short" or "int16_t" => "short",
            "ushort" or "uint16_t" => "ushort",
            "int" or "int32_t" => "int",
            "uint" or "uint32_t" => "uint",
            "long" or "int64_t" => "long",
            "ulong" or "uint64_t" => "ulong",
            "float" => "float",
            "double" => "double",
            "bool" => "bool",
            "string" => "string",
            _ => type // Custom types (enums, etc.)
        };
    }
}
