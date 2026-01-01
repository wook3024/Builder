using Scriban;
using ProtocolGenerator.Core.Models;
using ProtocolGenerator.Core.Mappers;

namespace ProtocolGenerator.Core.Generators;

public class CSharpCodeGenerator
{
    private readonly TypeMapper _typeMapper;
    private readonly Template _namespaceTemplate;
    private readonly Template _structTemplate;
    private readonly Template _enumTemplate;

    public CSharpCodeGenerator(TypeMapper typeMapper, string templatesPath = "templates")
    {
        _typeMapper = typeMapper;
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

    public string Generate(Dictionary<string, TypeInfo> types, string namespaceName = "Protocol")
    {
        var enums = new List<string>();
        var structs = new List<string>();

        foreach (var type in types.Values)
        {
            if (type is EnumInfo enumInfo)
            {
                enums.Add(GenerateEnum(enumInfo));
            }
            else if (type is StructInfo structInfo)
            {
                structs.Add(GenerateStruct(structInfo));
            }
        }

        var model = new
        {
            Namespace = namespaceName,
            Enums = string.Join("\n\n", enums),
            Structs = string.Join("\n\n", structs)
        };

        return _namespaceTemplate.Render(model);
    }

    public string GenerateStruct(StructInfo structInfo)
    {
        var model = new
        {
            Name = structInfo.Name,
            SizeInBytes = structInfo.SizeInBytes,
            Fields = structInfo.Fields.Select(f => new
            {
                Name = f.Name,
                Type = _typeMapper.MapToCSharp(f.Type),
                OriginalType = f.Type,
                MarshalAttribute = _typeMapper.GetMarshalAttribute(f.Type),
                Offset = f.Offset,
                IsFixedArray = _typeMapper.IsFixedArray(f.Type, out var arraySize),
                ArraySize = arraySize
            }).ToArray()
        };

        return _structTemplate.Render(model);
    }

    public string GenerateEnum(EnumInfo enumInfo)
    {
        var underlyingType = _typeMapper.MapToCSharp(enumInfo.UnderlyingType);

        var model = new
        {
            Name = enumInfo.Name,
            UnderlyingType = underlyingType,
            ShowUnderlyingType = underlyingType != "int",
            Values = enumInfo.Values.Select(kvp => new
            {
                Name = kvp.Key,
                Value = kvp.Value
            }).ToArray()
        };

        return _enumTemplate.Render(model);
    }
}
