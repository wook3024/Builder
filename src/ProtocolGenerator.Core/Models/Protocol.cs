namespace ProtocolGenerator.Core.Models;

public class Protocol
{
    public required string Name { get; set; }
    public string? Namespace { get; set; }
    public required List<MessageInfo> Messages { get; set; }
    public required List<EnumInfo> Enums { get; set; }
}

public class MessageInfo
{
    public required string Name { get; set; }
    public required int Id { get; set; }
    public required List<MessageField> Fields { get; set; }
}

public class MessageField
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public bool IsArray { get; set; }
    public int? ArraySize { get; set; }
}
