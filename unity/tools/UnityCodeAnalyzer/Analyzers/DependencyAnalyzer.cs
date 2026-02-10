namespace UnityCodeAnalyzer.Analyzers;

using UnityCodeAnalyzer.Models;

/// <summary>
/// 分析目录间的依赖关系
/// </summary>
public class DependencyAnalyzer
{
    /// <summary>
    /// Runtime 依赖调用模式（通过方法名匹配）
    /// </summary>
    private static readonly HashSet<string> RuntimeDependencyCalls =
    [
        "GetComponent", "GetComponentInChildren", "GetComponentInParent",
        "GetComponents", "GetComponentsInChildren", "GetComponentsInParent",
        "FindObjectOfType", "FindObjectsOfType", "FindFirstObjectByType", "FindAnyObjectByType",
        "AddComponent"
    ];

    /// <summary>
    /// 分析跨目录依赖关系
    /// </summary>
    public void AnalyzeDependencies(ProjectAnalysis project)
    {
        // 建立命名空间到目录的映射
        var namespaceToDirMap = BuildNamespaceToDirMap(project);

        foreach (var dir in project.Directories)
        {
            var localNamespaces = dir.Files
                .Where(f => f.Namespace != null)
                .Select(f => f.Namespace!)
                .Distinct()
                .ToHashSet();

            // 基于 using 声明分析跨目录引用
            var externalUsings = dir.Files
                .SelectMany(f => f.Usings)
                .Distinct()
                .Where(u => !IsSystemNamespace(u) && !localNamespaces.Contains(u));

            foreach (var usingNs in externalUsings)
            {
                // 查找该命名空间属于哪个目录
                var matchedDir = FindDirectoryForNamespace(usingNs, namespaceToDirMap);
                if (matchedDir != null && matchedDir != dir.Path)
                {
                    if (!dir.Dependencies.ReferencedDirectories.Contains(matchedDir))
                        dir.Dependencies.ReferencedDirectories.Add(matchedDir);
                }
            }

            dir.Dependencies.ReferencedDirectories.Sort();
        }
    }

    private static Dictionary<string, string> BuildNamespaceToDirMap(ProjectAnalysis project)
    {
        var map = new Dictionary<string, string>();
        foreach (var dir in project.Directories)
        {
            foreach (var file in dir.Files)
            {
                if (file.Namespace != null && !map.ContainsKey(file.Namespace))
                {
                    map[file.Namespace] = dir.Path;
                }
            }
        }
        return map;
    }

    private static string? FindDirectoryForNamespace(string ns, Dictionary<string, string> map)
    {
        // 精确匹配
        if (map.TryGetValue(ns, out var dir))
            return dir;

        // 前缀匹配（例如 using Game.Core 可能匹配到 Game.Core.Utils 目录下的命名空间）
        var bestMatch = map.Keys
            .Where(k => k.StartsWith(ns + ".") || ns.StartsWith(k + "."))
            .OrderByDescending(k => k.Length)
            .FirstOrDefault();

        return bestMatch != null ? map[bestMatch] : null;
    }

    private static bool IsSystemNamespace(string ns)
    {
        return ns.StartsWith("System") ||
               ns.StartsWith("UnityEngine") ||
               ns.StartsWith("UnityEditor") ||
               ns.StartsWith("Unity.") ||
               ns.StartsWith("TMPro") ||
               ns.StartsWith("Microsoft") ||
               ns.StartsWith("Newtonsoft") ||
               ns == "DG.Tweening" ||
               ns.StartsWith("DG.Tweening");
    }
}
