namespace UnityCodeAnalyzer.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityCodeAnalyzer.Models;
using FieldInfo = UnityCodeAnalyzer.Models.FieldInfo;
using MethodInfo = UnityCodeAnalyzer.Models.MethodInfo;
using ParameterInfo = UnityCodeAnalyzer.Models.ParameterInfo;
using PropertyInfo = UnityCodeAnalyzer.Models.PropertyInfo;
using EventInfo = UnityCodeAnalyzer.Models.EventInfo;
using TypeInfo = UnityCodeAnalyzer.Models.TypeInfo;

/// <summary>
/// 使用 Roslyn 解析单个 C# 文件，提取结构化信息
/// </summary>
public class FileAnalyzer
{
    /// <summary>
    /// 已知的 MonoBehaviour 继承链类型
    /// </summary>
    private static readonly HashSet<string> MonoBehaviourTypes =
    [
        "MonoBehaviour", "NetworkBehaviour", "StateMachineBehaviour",
        "UIBehaviour", "Graphic", "MaskableGraphic", "Selectable",
        "Button", "Toggle", "Slider", "Scrollbar", "InputField",
        "Image", "RawImage", "Text"
    ];

    /// <summary>
    /// 已知的 ScriptableObject 继承链类型
    /// </summary>
    private static readonly HashSet<string> ScriptableObjectTypes =
    [
        "ScriptableObject", "ScriptableSingleton"
    ];

    /// <summary>
    /// 已知的 Editor 相关基类
    /// </summary>
    private static readonly HashSet<string> EditorTypes =
    [
        "Editor", "EditorWindow", "PropertyDrawer", "DecoratorDrawer",
        "ScriptableWizard", "AssetPostprocessor", "AssetModificationProcessor"
    ];

    /// <summary>
    /// Unity 序列化相关特性
    /// </summary>
    private static readonly HashSet<string> SerializableAttributes =
    [
        "SerializeField", "SerializeReference", "Header", "Tooltip",
        "Range", "Min", "Max", "TextArea", "Multiline", "Space",
        "HideInInspector", "FormerlySerializedAs"
    ];

    /// <summary>
    /// 分析单个 C# 文件
    /// </summary>
    public FileAnalysis Analyze(string filePath, string basePath)
    {
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code, path: filePath);
        var root = tree.GetCompilationUnitRoot();

        var result = new FileAnalysis
        {
            Name = Path.GetFileName(filePath),
            RelativePath = Path.GetRelativePath(basePath, filePath).Replace('\\', '/'),
            Usings = ExtractUsings(root),
            Namespace = ExtractNamespace(root),
            Types = ExtractTypes(root.Members)
        };

        return result;
    }

    private static List<string> ExtractUsings(CompilationUnitSyntax root)
    {
        return root.Usings
            .Select(u => u.Name?.ToString() ?? string.Empty)
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList();
    }

    private static string? ExtractNamespace(CompilationUnitSyntax root)
    {
        // 支持 file-scoped namespace 和传统 namespace
        var fileScopedNs = root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScopedNs != null)
            return fileScopedNs.Name.ToString();

        var ns = root.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        return ns?.Name.ToString();
    }

    private List<TypeInfo> ExtractTypes(SyntaxList<MemberDeclarationSyntax> members)
    {
        var types = new List<TypeInfo>();

        foreach (var member in members)
        {
            switch (member)
            {
                case NamespaceDeclarationSyntax ns:
                    types.AddRange(ExtractTypes(ns.Members));
                    break;
                case FileScopedNamespaceDeclarationSyntax fsns:
                    types.AddRange(ExtractTypes(fsns.Members));
                    break;
                case ClassDeclarationSyntax classDecl:
                    types.Add(AnalyzeTypeDeclaration(classDecl, "class"));
                    break;
                case InterfaceDeclarationSyntax interfaceDecl:
                    types.Add(AnalyzeTypeDeclaration(interfaceDecl, "interface"));
                    break;
                case StructDeclarationSyntax structDecl:
                    types.Add(AnalyzeTypeDeclaration(structDecl, "struct"));
                    break;
                case EnumDeclarationSyntax enumDecl:
                    types.Add(AnalyzeEnum(enumDecl));
                    break;
                case RecordDeclarationSyntax recordDecl:
                    types.Add(AnalyzeTypeDeclaration(recordDecl, "record"));
                    break;
            }
        }

        return types;
    }

    private TypeInfo AnalyzeTypeDeclaration(TypeDeclarationSyntax decl, string kind)
    {
        var baseTypes = decl.BaseList?.Types.Select(t => t.Type.ToString()).ToList() ?? [];
        var baseType = baseTypes.FirstOrDefault();
        var interfaces = baseTypes.Skip(baseType != null ? 1 : 0).ToList();

        // 对于 interface，第一个 base 也是 interface
        if (kind == "interface")
        {
            interfaces = baseTypes;
            baseType = null;
        }

        var info = new TypeInfo
        {
            Name = decl.Identifier.Text + FormatTypeParameters(decl.TypeParameterList),
            Kind = kind,
            Modifiers = decl.Modifiers.Select(m => m.Text).ToList(),
            BaseType = baseType,
            Interfaces = interfaces,
            UnityType = ClassifyUnityType(baseType, decl),
            Attributes = ExtractAttributes(decl.AttributeLists),
            Methods = ExtractMethods(decl),
            Fields = ExtractFields(decl),
            Properties = ExtractProperties(decl),
            Events = ExtractEvents(decl),
            NestedTypes = ExtractNestedTypes(decl)
        };

        return info;
    }

    private TypeInfo AnalyzeEnum(EnumDeclarationSyntax decl)
    {
        return new TypeInfo
        {
            Name = decl.Identifier.Text,
            Kind = "enum",
            Modifiers = decl.Modifiers.Select(m => m.Text).ToList(),
            UnityType = "PureCSharp",
            Attributes = ExtractAttributes(decl.AttributeLists),
            EnumValues = decl.Members.Select(m => m.Identifier.Text).ToList()
        };
    }

    private List<TypeInfo> ExtractNestedTypes(TypeDeclarationSyntax decl)
    {
        var nested = new List<TypeInfo>();
        foreach (var member in decl.Members)
        {
            switch (member)
            {
                case ClassDeclarationSyntax c:
                    nested.Add(AnalyzeTypeDeclaration(c, "class"));
                    break;
                case InterfaceDeclarationSyntax i:
                    nested.Add(AnalyzeTypeDeclaration(i, "interface"));
                    break;
                case StructDeclarationSyntax s:
                    nested.Add(AnalyzeTypeDeclaration(s, "struct"));
                    break;
                case EnumDeclarationSyntax e:
                    nested.Add(AnalyzeEnum(e));
                    break;
            }
        }
        return nested;
    }

    private string ClassifyUnityType(string? baseType, TypeDeclarationSyntax decl)
    {
        if (baseType == null) return "PureCSharp";

        // 提取类名（去掉泛型参数和命名空间前缀）
        var simpleBase = baseType.Split('.').Last().Split('<').First();

        if (MonoBehaviourTypes.Contains(simpleBase)) return "MonoBehaviour";
        if (ScriptableObjectTypes.Contains(simpleBase)) return "ScriptableObject";
        if (EditorTypes.Contains(simpleBase)) return "Editor";

        // 检查是否在 Editor 命名空间下
        var parentNs = decl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        if (parentNs?.Name.ToString().Contains("Editor") == true) return "Editor";

        return "PureCSharp";
    }

    private static List<AttributeInfo> ExtractAttributes(SyntaxList<AttributeListSyntax> attributeLists)
    {
        var attrs = new List<AttributeInfo>();
        foreach (var list in attributeLists)
        {
            foreach (var attr in list.Attributes)
            {
                attrs.Add(new AttributeInfo
                {
                    Name = attr.Name.ToString(),
                    Arguments = attr.ArgumentList?.Arguments
                        .Select(a => a.ToString())
                        .ToList() ?? []
                });
            }
        }
        return attrs;
    }

    private static List<MethodInfo> ExtractMethods(TypeDeclarationSyntax decl)
    {
        var methods = new List<MethodInfo>();

        foreach (var method in decl.Members.OfType<MethodDeclarationSyntax>())
        {
            var returnType = method.ReturnType.ToString();
            methods.Add(new MethodInfo
            {
                Name = method.Identifier.Text,
                ReturnType = returnType,
                Modifiers = method.Modifiers.Select(m => m.Text).ToList(),
                TypeParameters = method.TypeParameterList?.Parameters
                    .Select(p => p.Identifier.Text).ToList() ?? [],
                Parameters = method.ParameterList.Parameters.Select(p => new ParameterInfo
                {
                    Name = p.Identifier.Text,
                    Type = p.Type?.ToString() ?? "var",
                    DefaultValue = p.Default?.Value.ToString(),
                    Modifier = p.Modifiers.FirstOrDefault().Text is { Length: > 0 } mod ? mod : null
                }).ToList(),
                Attributes = ExtractAttributes(method.AttributeLists),
                IsCoroutine = returnType is "IEnumerator" or "IEnumerable",
                IsAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword))
            });
        }

        // 构造函数
        foreach (var ctor in decl.Members.OfType<ConstructorDeclarationSyntax>())
        {
            methods.Add(new MethodInfo
            {
                Name = ".ctor",
                ReturnType = "void",
                Modifiers = ctor.Modifiers.Select(m => m.Text).ToList(),
                Parameters = ctor.ParameterList.Parameters.Select(p => new ParameterInfo
                {
                    Name = p.Identifier.Text,
                    Type = p.Type?.ToString() ?? "var",
                    DefaultValue = p.Default?.Value.ToString(),
                    Modifier = p.Modifiers.FirstOrDefault().Text is { Length: > 0 } mod ? mod : null
                }).ToList(),
                Attributes = ExtractAttributes(ctor.AttributeLists)
            });
        }

        return methods;
    }

    private List<FieldInfo> ExtractFields(TypeDeclarationSyntax decl)
    {
        var fields = new List<FieldInfo>();

        foreach (var field in decl.Members.OfType<FieldDeclarationSyntax>())
        {
            var attrs = ExtractAttributes(field.AttributeLists);
            var isSerializable = IsFieldSerializable(field, attrs);

            foreach (var variable in field.Declaration.Variables)
            {
                fields.Add(new FieldInfo
                {
                    Name = variable.Identifier.Text,
                    Type = field.Declaration.Type.ToString(),
                    Modifiers = field.Modifiers.Select(m => m.Text).ToList(),
                    Attributes = attrs,
                    IsSerializable = isSerializable
                });
            }
        }

        return fields;
    }

    private static bool IsFieldSerializable(FieldDeclarationSyntax field, List<AttributeInfo> attrs)
    {
        // 有 [SerializeField] 或 [SerializeReference]
        if (attrs.Any(a => a.Name is "SerializeField" or "SerializeReference"))
            return true;

        // public 且非 static、非 readonly、非 const，且没有 [NonSerialized] / [HideInInspector]
        var mods = field.Modifiers;
        if (mods.Any(m => m.IsKind(SyntaxKind.PublicKeyword)) &&
            !mods.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
            !mods.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)) &&
            !mods.Any(m => m.IsKind(SyntaxKind.ConstKeyword)) &&
            !attrs.Any(a => a.Name == "NonSerialized"))
        {
            return true;
        }

        return false;
    }

    private static List<PropertyInfo> ExtractProperties(TypeDeclarationSyntax decl)
    {
        var props = new List<PropertyInfo>();

        foreach (var prop in decl.Members.OfType<PropertyDeclarationSyntax>())
        {
            props.Add(new PropertyInfo
            {
                Name = prop.Identifier.Text,
                Type = prop.Type.ToString(),
                Modifiers = prop.Modifiers.Select(m => m.Text).ToList(),
                HasGetter = prop.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? prop.ExpressionBody != null,
                HasSetter = prop.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false,
                Attributes = ExtractAttributes(prop.AttributeLists)
            });
        }

        return props;
    }

    private static List<EventInfo> ExtractEvents(TypeDeclarationSyntax decl)
    {
        var events = new List<EventInfo>();

        // event field declarations: public event Action<int> OnDamage;
        foreach (var evt in decl.Members.OfType<EventFieldDeclarationSyntax>())
        {
            foreach (var variable in evt.Declaration.Variables)
            {
                events.Add(new EventInfo
                {
                    Name = variable.Identifier.Text,
                    Type = evt.Declaration.Type.ToString(),
                    Modifiers = evt.Modifiers.Select(m => m.Text).ToList()
                });
            }
        }

        // event property declarations (with add/remove)
        foreach (var evt in decl.Members.OfType<EventDeclarationSyntax>())
        {
            events.Add(new EventInfo
            {
                Name = evt.Identifier.Text,
                Type = evt.Type.ToString(),
                Modifiers = evt.Modifiers.Select(m => m.Text).ToList()
            });
        }

        return events;
    }

    private static string FormatTypeParameters(TypeParameterListSyntax? typeParams)
    {
        if (typeParams == null || typeParams.Parameters.Count == 0)
            return string.Empty;
        return "<" + string.Join(", ", typeParams.Parameters.Select(p => p.Identifier.Text)) + ">";
    }
}
