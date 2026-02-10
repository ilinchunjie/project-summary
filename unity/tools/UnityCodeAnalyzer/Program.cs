using System.Text.Json;
using System.Text.Json.Serialization;
using UnityCodeAnalyzer.Analyzers;

namespace UnityCodeAnalyzer;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var projectPath = args[0];
        string? outputPath = null;

        // 解析参数
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output" or "-o" when i + 1 < args.Length:
                    outputPath = args[++i];
                    break;
            }
        }

        if (!Directory.Exists(projectPath))
        {
            Console.Error.WriteLine($"Error: Path '{projectPath}' does not exist.");
            return 1;
        }

        try
        {
            Console.Error.WriteLine($"Analyzing: {projectPath}");

            // 目录分析
            var dirAnalyzer = new DirectoryAnalyzer();
            var project = dirAnalyzer.AnalyzeProject(projectPath);

            // 依赖分析
            var depAnalyzer = new DependencyAnalyzer();
            depAnalyzer.AnalyzeDependencies(project);

            Console.Error.WriteLine($"Found {project.TotalFiles} C# files, {project.TotalClasses} scripts in {project.Directories.Count} directories.");

            // 输出 JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(project, options);

            if (outputPath != null)
            {
                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(outputPath, json);
                Console.Error.WriteLine($"Output written to: {outputPath}");
            }
            else
            {
                Console.WriteLine(json);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.Error.WriteLine("""
            Unity Code Analyzer - Roslyn-based C# project structure analyzer

            Usage:
              UnityCodeAnalyzer <project-path> [options]

            Arguments:
              <project-path>    Path to Unity project root or Assets/ directory

            Options:
              --output, -o      Output file path (default: stdout)
              --help, -h        Show this help message

            Examples:
              UnityCodeAnalyzer ./MyUnityProject
              UnityCodeAnalyzer ./MyUnityProject/Assets --output analysis.json
              dotnet run -- /path/to/project -o result.json
            """);
    }
}
