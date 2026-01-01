using ProtocolGenerator.Core.Parsers;
using ProtocolGenerator.Core.Mappers;
using ProtocolGenerator.Core.Generators;
using System.Text;

namespace ProtocolGenerator.CLI;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Protocol Code Generator");
        Console.WriteLine("=======================\n");

        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        var xmlFile = args[0];
        var outputDir = args.Length > 1 ? args[1] : "output";

        try
        {
            Generate(xmlFile, outputDir);
            Console.WriteLine("\nCode generation completed successfully!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static void Generate(string xmlFile, string outputDir)
    {
        if (!File.Exists(xmlFile))
        {
            throw new FileNotFoundException($"XML file not found: {xmlFile}");
        }

        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"Parsing XML protocol: {xmlFile}");
        var xmlParser = new XmlProtocolParser();
        var protocol = xmlParser.Parse(xmlFile);

        Console.WriteLine($"Protocol: {protocol.Name}");
        Console.WriteLine($"Messages: {protocol.Messages.Count}");
        Console.WriteLine($"Enums: {protocol.Enums.Count}");

        // Generate C++ code
        Console.WriteLine("\nGenerating C++ code...");
        var templatesPath = AppContext.BaseDirectory;
        var cppGenerator = new CppCodeGenerator(templatesPath);
        var cppCode = cppGenerator.Generate(protocol);
        var cppOutputPath = Path.Combine(outputDir, $"{protocol.Name}.h");
        File.WriteAllText(cppOutputPath, cppCode, new UTF8Encoding(false)); // No BOM
        Console.WriteLine($"  -> {cppOutputPath}");

        // Generate C# code directly from protocol
        Console.WriteLine("\nGenerating C# code...");
        var csharpGenerator = new CSharpProtocolGenerator(templatesPath);
        var csharpCode = csharpGenerator.Generate(protocol);
        var csharpOutputPath = Path.Combine(outputDir, $"{protocol.Name}.cs");
        File.WriteAllText(csharpOutputPath, csharpCode, new UTF8Encoding(false)); // No BOM
        Console.WriteLine($"  -> {csharpOutputPath}");

        // Generate summary
        Console.WriteLine("\n--- Generation Summary ---");
        Console.WriteLine($"C++ Header: {cppOutputPath}");
        Console.WriteLine($"C# File: {csharpOutputPath}");
        Console.WriteLine($"Total types: {protocol.Enums.Count + protocol.Messages.Count}");
    }

    static void ShowUsage()
    {
        Console.WriteLine("Usage: ProtocolGenerator <xml-file> [output-dir]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  xml-file    Path to the XML protocol definition file");
        Console.WriteLine("  output-dir  Output directory (default: 'output')");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  ProtocolGenerator protocol.xml ./generated");
    }
}
