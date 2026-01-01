using ClangSharp;
using ClangSharp.Interop;
using ProtocolGenerator.Core.Models;

namespace ProtocolGenerator.Core.Parsers;

public class CppAnalyzer : IDisposable
{
    private readonly CXIndex _index;

    public CppAnalyzer()
    {
        _index = CXIndex.Create(excludeDeclarationsFromPch: false, displayDiagnostics: true);
    }

    public Dictionary<string, TypeInfo> AnalyzeHeader(string headerPath, string[]? compilerArgs = null)
    {
        var types = new Dictionary<string, TypeInfo>();

        // Convert to absolute path
        headerPath = Path.GetFullPath(headerPath);

        compilerArgs ??= new[] { "-std=c++20" };

        CXTranslationUnit translationUnit;
        CXErrorCode errorCode = CXTranslationUnit.TryParse(
            _index,
            headerPath,
            compilerArgs,
            Array.Empty<CXUnsavedFile>(),
            CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies |
            CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord,
            out translationUnit
        );

        if (errorCode != CXErrorCode.CXError_Success)
        {
            throw new InvalidOperationException($"Failed to parse header: {headerPath}, Error: {errorCode}");
        }

        // Print diagnostics
        var numDiagnostics = translationUnit.NumDiagnostics;
        if (numDiagnostics > 0)
        {
            Console.WriteLine($"  Found {numDiagnostics} diagnostic(s):");
            for (uint i = 0; i < numDiagnostics; i++)
            {
                var diagnostic = translationUnit.GetDiagnostic(i);
                Console.WriteLine($"    {diagnostic.Format(CXDiagnosticDisplayOptions.CXDiagnostic_DisplayOption)}");
            }
        }

        VisitCursor(translationUnit.Cursor, types);
        translationUnit.Dispose();

        return types;
    }

    private void VisitCursor(CXCursor cursor, Dictionary<string, TypeInfo> types)
    {
        unsafe
        {
            cursor.VisitChildren((child, parent, clientData) =>
            {
                // Only process declarations from main file, skip includes
                if (!child.Location.IsFromMainFile)
                {
                    return CXChildVisitResult.CXChildVisit_Continue;
                }

                switch (child.Kind)
                {
                    case CXCursorKind.CXCursor_StructDecl:
                    case CXCursorKind.CXCursor_ClassDecl:
                        if (!child.Spelling.ToString().StartsWith("_"))
                        {
                            ProcessStruct(child, types);
                        }
                        break;

                    case CXCursorKind.CXCursor_TypedefDecl:
                    case CXCursorKind.CXCursor_TypeAliasDecl:  // C++11 using 구문도 처리
                        ProcessTypedef(child, types);
                        break;

                    case CXCursorKind.CXCursor_EnumDecl:
                        if (!string.IsNullOrEmpty(child.Spelling.ToString()))
                        {
                            ProcessEnum(child, types);
                        }
                        break;
                }

                VisitCursor(child, types);
                return CXChildVisitResult.CXChildVisit_Continue;
            }, default);
        }
    }

    private void ProcessStruct(CXCursor cursor, Dictionary<string, TypeInfo> types)
    {
        var name = cursor.Spelling.ToString();
        if (string.IsNullOrEmpty(name) || types.ContainsKey(name))
        {
            return;
        }

        var structInfo = new StructInfo
        {
            Name = name,
            Fields = new List<FieldInfo>(),
            SizeInBytes = (int)cursor.Type.SizeOf
        };

        unsafe
        {
            cursor.VisitChildren((field, parent, clientData) =>
            {
                if (field.Kind == CXCursorKind.CXCursor_FieldDecl)
                {
                    structInfo.Fields.Add(new FieldInfo
                    {
                        Name = field.Spelling.ToString(),
                        Type = field.Type.Spelling.ToString(),
                        Offset = (int)field.OffsetOfField / 8, // bits to bytes
                        SizeInBytes = (int)field.Type.SizeOf
                    });
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, default);
        }

        types[structInfo.Name] = structInfo;
    }

    private void ProcessTypedef(CXCursor cursor, Dictionary<string, TypeInfo> types)
    {
        var name = cursor.Spelling.ToString();
        if (string.IsNullOrEmpty(name) || types.ContainsKey(name))
        {
            return;
        }

        // typedef와 using 모두 처리
        // ClangSharp에서 TypeAliasDecl도 TypedefDeclUnderlyingType 속성을 지원하는지 확인 필요
        // 일반적으로 cursor.Type을 사용하면 underlying type을 얻을 수 있음
        string underlyingType;
        if (cursor.Kind == CXCursorKind.CXCursor_TypeAliasDecl)
        {
            // using 구문: cursor.Type이 underlying type을 가리킴
            underlyingType = cursor.Type.Spelling.ToString();
        }
        else
        {
            // typedef 구문: TypedefDeclUnderlyingType 사용
            underlyingType = cursor.TypedefDeclUnderlyingType.Spelling.ToString();
        }

        var typedefInfo = new TypedefInfo
        {
            Name = name,
            UnderlyingType = underlyingType
        };

        types[typedefInfo.Name] = typedefInfo;
    }

    private void ProcessEnum(CXCursor cursor, Dictionary<string, TypeInfo> types)
    {
        var name = cursor.Spelling.ToString();
        if (string.IsNullOrEmpty(name) || types.ContainsKey(name))
        {
            return;
        }

        var enumInfo = new EnumInfo
        {
            Name = name,
            UnderlyingType = cursor.EnumDecl_IntegerType.Spelling.ToString(),
            Values = new Dictionary<string, long>()
        };

        unsafe
        {
            cursor.VisitChildren((enumerator, parent, clientData) =>
            {
                if (enumerator.Kind == CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    enumInfo.Values[enumerator.Spelling.ToString()] = enumerator.EnumConstantDeclValue;
                }
                return CXChildVisitResult.CXChildVisit_Continue;
            }, default);
        }

        types[enumInfo.Name] = enumInfo;
    }

    public void Dispose()
    {
        _index.Dispose();
    }
}
