namespace UnityCodeAnalyzer.Analyzers;

using System.Text.Json;
using UnityCodeAnalyzer.Models;

/// <summary>
/// 目录级分析器：递归扫描目录，聚合文件分析结果
/// </summary>
public class DirectoryAnalyzer
{
    private readonly FileAnalyzer _fileAnalyzer = new();

    /// <summary>
    /// 应跳过的目录名（小写匹配）
    /// </summary>
    private static readonly HashSet<string> SkipDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Library", "Temp", "Logs", "obj", "Build", "Builds",
        "MemoryCaptures", "Recordings", "UserSettings",
        ".git", ".vs", ".idea", ".vscode",
        "TextMesh Pro", "TextMeshPro",
        "node_modules", "bin"
    };

    /// <summary>
    /// 应跳过的文件扩展名
    /// </summary>
    private static readonly HashSet<string> SkipExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".meta", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".gif", ".bmp",
        ".fbx", ".obj", ".blend", ".3ds", ".dae",
        ".wav", ".mp3", ".ogg", ".aiff", ".flac",
        ".mat", ".physicMaterial", ".physicsMaterial2D",
        ".dll", ".so", ".a", ".aar", ".jar", ".bundle",
        ".ttf", ".otf", ".fontsettings",
        ".lighting", ".giparams",
        ".unity", ".prefab", ".asset", ".preset"
    };

    /// <summary>
    /// 分析整个项目
    /// </summary>
    public ProjectAnalysis AnalyzeProject(string projectPath)
    {
        var assetsPath = FindAssetsPath(projectPath);
        var result = new ProjectAnalysis
        {
            ProjectPath = assetsPath,
            AnalyzedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        AnalyzeDirectoryRecursive(assetsPath, assetsPath, result);

        result.TotalFiles = result.Directories.Sum(d => d.Files.Count);
        result.TotalClasses = result.Directories.Sum(d => d.Stats.TotalScripts);

        return result;
    }

    private static string FindAssetsPath(string projectPath)
    {
        // 如果直接指向 Assets 目录
        if (Path.GetFileName(projectPath).Equals("Assets", StringComparison.OrdinalIgnoreCase))
            return projectPath;

        // 查找 Assets 子目录
        var assetsPath = Path.Combine(projectPath, "Assets");
        if (Directory.Exists(assetsPath))
            return assetsPath;

        // 当前路径就是分析根目录
        return projectPath;
    }

    private void AnalyzeDirectoryRecursive(string dirPath, string basePath, ProjectAnalysis project)
    {
        var dirName = Path.GetFileName(dirPath);

        // 跳过特定目录
        if (ShouldSkipDirectory(dirName, dirPath))
            return;

        // 收集 .cs 文件
        var csFiles = Directory.GetFiles(dirPath, "*.cs", SearchOption.TopDirectoryOnly);

        // 检测 .asmdef
        var asmdefFile = Directory.GetFiles(dirPath, "*.asmdef", SearchOption.TopDirectoryOnly).FirstOrDefault();
        string? asmdefName = null;
        if (asmdefFile != null)
        {
            asmdefName = ParseAsmdefName(asmdefFile);
        }

        // 如果有 .cs 文件，分析此目录
        if (csFiles.Length > 0)
        {
            var dirAnalysis = new DirectoryAnalysis
            {
                Path = Path.GetRelativePath(basePath, dirPath).Replace('\\', '/'),
                Asmdef = asmdefName
            };

            foreach (var csFile in csFiles)
            {
                try
                {
                    var fileResult = _fileAnalyzer.Analyze(csFile, basePath);
                    dirAnalysis.Files.Add(fileResult);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Failed to analyze {csFile}: {ex.Message}");
                }
            }

            // 计算统计信息
            ComputeStats(dirAnalysis);

            // 收集依赖命名空间
            dirAnalysis.Dependencies.UsesNamespaces = dirAnalysis.Files
                .SelectMany(f => f.Usings)
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            project.Directories.Add(dirAnalysis);
        }

        // 递归子目录
        foreach (var subDir in Directory.GetDirectories(dirPath))
        {
            AnalyzeDirectoryRecursive(subDir, basePath, project);
        }
    }

    private static bool ShouldSkipDirectory(string dirName, string dirPath)
    {
        if (SkipDirectories.Contains(dirName))
            return true;

        // 跳过以 . 开头的隐藏目录
        if (dirName.StartsWith('.'))
            return true;

        // 检查是否为第三方插件目录（有 LICENSE 但无自研 .cs 文件）
        // 这里做简化判断：如果是 Plugins 下的子目录且不含 .asmdef，可能是第三方
        // 但保守起见，仍然分析
        return false;
    }

    private static string? ParseAsmdefName(string asmdefPath)
    {
        try
        {
            var json = File.ReadAllText(asmdefPath);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null;
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(asmdefPath);
        }
    }

    private static void ComputeStats(DirectoryAnalysis dir)
    {
        var allTypes = dir.Files.SelectMany(f => f.Types).ToList();

        dir.Stats = new DirectoryStats
        {
            TotalScripts = dir.Files.Count,
            MonoBehaviours = allTypes.Count(t => t.UnityType == "MonoBehaviour"),
            ScriptableObjects = allTypes.Count(t => t.UnityType == "ScriptableObject"),
            Interfaces = allTypes.Count(t => t.Kind == "interface"),
            EditorScripts = allTypes.Count(t => t.UnityType == "Editor"),
            PureCSharp = allTypes.Count(t => t.UnityType == "PureCSharp" && t.Kind == "class"),
            Enums = allTypes.Count(t => t.Kind == "enum")
        };
    }
}
