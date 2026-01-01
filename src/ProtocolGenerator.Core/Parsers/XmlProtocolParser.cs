using System.Xml.Linq;
using ProtocolGenerator.Core.Models;

namespace ProtocolGenerator.Core.Parsers;

public class XmlProtocolParser
{
    public Protocol Parse(string xmlFilePath)
    {
        var doc = XDocument.Load(xmlFilePath);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid XML: missing root element");

        var protocol = new Protocol
        {
            Name = root.Attribute("name")?.Value ?? "Protocol",
            Namespace = root.Attribute("namespace")?.Value,
            Messages = new List<MessageInfo>(),
            Enums = new List<EnumInfo>()
        };

        // Parse enums
        foreach (var enumElement in root.Elements("enum"))
        {
            protocol.Enums.Add(ParseEnum(enumElement));
        }

        // Parse messages
        foreach (var messageElement in root.Elements("message"))
        {
            protocol.Messages.Add(ParseMessage(messageElement));
        }

        return protocol;
    }

    private EnumInfo ParseEnum(XElement enumElement)
    {
        var enumInfo = new EnumInfo
        {
            Name = enumElement.Attribute("name")?.Value ?? throw new InvalidOperationException("Enum missing name"),
            UnderlyingType = enumElement.Attribute("type")?.Value ?? "int",
            Values = new Dictionary<string, long>()
        };

        foreach (var valueElement in enumElement.Elements("value"))
        {
            var name = valueElement.Attribute("name")?.Value ?? throw new InvalidOperationException("Enum value missing name");
            var value = long.Parse(valueElement.Attribute("value")?.Value ?? "0");
            enumInfo.Values[name] = value;
        }

        return enumInfo;
    }

    private MessageInfo ParseMessage(XElement messageElement)
    {
        var message = new MessageInfo
        {
            Name = messageElement.Attribute("name")?.Value ?? throw new InvalidOperationException("Message missing name"),
            Id = int.Parse(messageElement.Attribute("id")?.Value ?? "0"),
            Fields = new List<MessageField>()
        };

        foreach (var fieldElement in messageElement.Elements("field"))
        {
            message.Fields.Add(ParseField(fieldElement));
        }

        return message;
    }

    private MessageField ParseField(XElement fieldElement)
    {
        var type = fieldElement.Attribute("type")?.Value ?? throw new InvalidOperationException("Field missing type");
        var isArray = type.EndsWith("[]");
        var baseType = isArray ? type.TrimEnd('[', ']') : type;

        var field = new MessageField
        {
            Name = fieldElement.Attribute("name")?.Value ?? throw new InvalidOperationException("Field missing name"),
            Type = baseType,
            IsArray = isArray,
            ArraySize = fieldElement.Attribute("size") != null
                ? int.Parse(fieldElement.Attribute("size")!.Value)
                : null
        };

        return field;
    }
}
