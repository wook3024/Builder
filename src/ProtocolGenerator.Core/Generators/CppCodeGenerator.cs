using Scriban;
using ProtocolGenerator.Core.Models;

namespace ProtocolGenerator.Core.Generators;

public class CppCodeGenerator
{
    private readonly Template _headerTemplate;
    private readonly Template _structTemplate;
    private readonly Template _enumTemplate;

    public CppCodeGenerator(string templatesPath = "templates")
    {
        _headerTemplate = LoadTemplate(Path.Combine(templatesPath, "cpp_header.scriban"));
        _structTemplate = LoadTemplate(Path.Combine(templatesPath, "cpp_struct.scriban"));
        _enumTemplate = LoadTemplate(Path.Combine(templatesPath, "cpp_enum.scriban"));
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
        var enums = string.Join("\n\n", protocol.Enums.Select(GenerateEnum));
        var messages = string.Join("\n\n", protocol.Messages.Select(GenerateMessage));

        var model = new
        {
            Name = protocol.Name,
            Namespace = protocol.Namespace ?? "Protocol",
            Enums = enums,
            Messages = messages
        };

        return _headerTemplate.Render(model);
    }

    private string GenerateEnum(EnumInfo enumInfo)
    {
        var model = new
        {
            Name = enumInfo.Name,
            UnderlyingType = enumInfo.UnderlyingType,
            Values = enumInfo.Values.Select(kvp => new
            {
                Name = kvp.Key,
                Value = kvp.Value
            }).ToArray()
        };

        return _enumTemplate.Render(model);
    }

    private string GenerateMessage(MessageInfo message)
    {
        var model = new
        {
            Name = message.Name,
            Id = message.Id,
            Fields = message.Fields.Select(f => new
            {
                Name = f.Name,
                Type = GetCppType(f),
                Comment = f.IsArray ? $"Array of {f.Type}" : null
            }).ToArray()
        };

        return _structTemplate.Render(model);
    }

    private string GetCppType(MessageField field)
    {
        var baseType = MapToCppType(field.Type);

        if (field.IsArray)
        {
            if (field.ArraySize.HasValue)
            {
                return $"std::array<{baseType}, {field.ArraySize.Value}>";
            }
            else
            {
                return $"std::vector<{baseType}>";
            }
        }

        return baseType;
    }

    private string MapToCppType(string type)
    {
        return type switch
        {
            "byte" => "uint8_t",
            "sbyte" => "int8_t",
            "short" => "int16_t",
            "ushort" => "uint16_t",
            "int" => "int32_t",
            "uint" => "uint32_t",
            "long" => "int64_t",
            "ulong" => "uint64_t",
            "float" => "float",
            "double" => "double",
            "bool" => "bool",
            "string" => "std::string",
            _ => type
        };
    }
}
