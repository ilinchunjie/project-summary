namespace UnityCodeAnalyzer.Models;

using System.Text.Json.Serialization;

/// <summary>
/// 项目分析结果 - 顶层输出
/// </summary>
public class ProjectAnalysis
{
    [JsonPropertyName("projectPath")]
    public string ProjectPath { get; set; } = string.Empty;

    [JsonPropertyName("analyzedAt")]
    public string AnalyzedAt { get; set; } = string.Empty;

    [JsonPropertyName("totalFiles")]
    public int TotalFiles { get; set; }

    [JsonPropertyName("totalClasses")]
    public int TotalClasses { get; set; }

    [JsonPropertyName("directories")]
    public List<DirectoryAnalysis> Directories { get; set; } = [];
}

/// <summary>
/// 目录分析结果
/// </summary>
public class DirectoryAnalysis
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("asmdef")]
    public string? Asmdef { get; set; }

    [JsonPropertyName("files")]
    public List<FileAnalysis> Files { get; set; } = [];

    [JsonPropertyName("stats")]
    public DirectoryStats Stats { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public DirectoryDependencies Dependencies { get; set; } = new();
}

/// <summary>
/// 目录统计信息
/// </summary>
public class DirectoryStats
{
    [JsonPropertyName("totalScripts")]
    public int TotalScripts { get; set; }

    [JsonPropertyName("monoBehaviours")]
    public int MonoBehaviours { get; set; }

    [JsonPropertyName("scriptableObjects")]
    public int ScriptableObjects { get; set; }

    [JsonPropertyName("interfaces")]
    public int Interfaces { get; set; }

    [JsonPropertyName("editorScripts")]
    public int EditorScripts { get; set; }

    [JsonPropertyName("pureCSharp")]
    public int PureCSharp { get; set; }

    [JsonPropertyName("enums")]
    public int Enums { get; set; }
}

/// <summary>
/// 目录间依赖关系
/// </summary>
public class DirectoryDependencies
{
    [JsonPropertyName("usesNamespaces")]
    public List<string> UsesNamespaces { get; set; } = [];

    [JsonPropertyName("referencedDirectories")]
    public List<string> ReferencedDirectories { get; set; } = [];
}

/// <summary>
/// 单文件分析结果
/// </summary>
public class FileAnalysis
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }

    [JsonPropertyName("usings")]
    public List<string> Usings { get; set; } = [];

    [JsonPropertyName("types")]
    public List<TypeInfo> Types { get; set; } = [];
}

/// <summary>
/// 类/接口/结构体/枚举信息
/// </summary>
public class TypeInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty; // class, interface, struct, enum, record

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = [];

    [JsonPropertyName("baseType")]
    public string? BaseType { get; set; }

    [JsonPropertyName("interfaces")]
    public List<string> Interfaces { get; set; } = [];

    [JsonPropertyName("unityType")]
    public string UnityType { get; set; } = "PureCSharp"; // MonoBehaviour, ScriptableObject, Editor, EditorWindow, PureCSharp

    [JsonPropertyName("attributes")]
    public List<AttributeInfo> Attributes { get; set; } = [];

    [JsonPropertyName("methods")]
    public List<MethodInfo> Methods { get; set; } = [];

    [JsonPropertyName("fields")]
    public List<FieldInfo> Fields { get; set; } = [];

    [JsonPropertyName("properties")]
    public List<PropertyInfo> Properties { get; set; } = [];

    [JsonPropertyName("events")]
    public List<EventInfo> Events { get; set; } = [];

    [JsonPropertyName("nestedTypes")]
    public List<TypeInfo> NestedTypes { get; set; } = [];

    /// <summary>
    /// 枚举值（仅 kind == "enum" 时有效）
    /// </summary>
    [JsonPropertyName("enumValues")]
    public List<string> EnumValues { get; set; } = [];
}

/// <summary>
/// 特性标注
/// </summary>
public class AttributeInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public List<string> Arguments { get; set; } = [];
}

/// <summary>
/// 方法签名
/// </summary>
public class MethodInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("returnType")]
    public string ReturnType { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = [];

    [JsonPropertyName("typeParameters")]
    public List<string> TypeParameters { get; set; } = [];

    [JsonPropertyName("parameters")]
    public List<ParameterInfo> Parameters { get; set; } = [];

    [JsonPropertyName("attributes")]
    public List<AttributeInfo> Attributes { get; set; } = [];

    [JsonPropertyName("isCoroutine")]
    public bool IsCoroutine { get; set; }

    [JsonPropertyName("isAsync")]
    public bool IsAsync { get; set; }
}

/// <summary>
/// 方法参数
/// </summary>
public class ParameterInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    [JsonPropertyName("modifier")]
    public string? Modifier { get; set; } // ref, out, in, params
}

/// <summary>
/// 字段信息
/// </summary>
public class FieldInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = [];

    [JsonPropertyName("attributes")]
    public List<AttributeInfo> Attributes { get; set; } = [];

    [JsonPropertyName("isSerializable")]
    public bool IsSerializable { get; set; }
}

/// <summary>
/// 属性信息
/// </summary>
public class PropertyInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = [];

    [JsonPropertyName("hasGetter")]
    public bool HasGetter { get; set; }

    [JsonPropertyName("hasSetter")]
    public bool HasSetter { get; set; }

    [JsonPropertyName("attributes")]
    public List<AttributeInfo> Attributes { get; set; } = [];
}

/// <summary>
/// 事件信息
/// </summary>
public class EventInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = [];
}
