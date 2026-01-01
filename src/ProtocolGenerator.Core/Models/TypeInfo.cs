namespace ProtocolGenerator.Core.Models;

public abstract class TypeInfo
{
    public required string Name { get; set; }
}

public class StructInfo : TypeInfo
{
    public required List<FieldInfo> Fields { get; set; }
    public int SizeInBytes { get; set; }
}

public class FieldInfo
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public int Offset { get; set; }
    public int SizeInBytes { get; set; }
}

public class TypedefInfo : TypeInfo
{
    public required string UnderlyingType { get; set; }
}

public class EnumInfo : TypeInfo
{
    public required string UnderlyingType { get; set; }
    public required Dictionary<string, long> Values { get; set; }
}
