---
name: unity-project-summary
description: 分析 Unity 游戏项目代码结构，以目录为单位总结代码逻辑，生成可供 AI 快速提取关键信息的文档
---

# Unity 项目代码结构分析

本 Skill 用于对 Unity 引擎开发的游戏项目进行系统性代码分析，输出结构化的 Markdown 文档，使 AI 能快速理解项目架构、模块职责和代码间的关联关系。

## 前置条件

- 项目根目录应包含 `Assets/`、`ProjectSettings/` 等 Unity 标志性目录
- 分析对象为 `Assets/` 目录下的 C# 脚本文件（`.cs`）

## 执行流程

### 第一步：识别项目信息

1. 读取 `ProjectSettings/ProjectVersion.txt` 获取 Unity 版本
2. 检查 `Packages/manifest.json` 获取渲染管线（URP / HDRP / Built-in）和关键包依赖
3. 检查 `ProjectSettings/ProjectSettings.asset` 获取目标平台信息
4. 检查根目录是否有 `.gitignore`、`README.md` 等辅助文件，获取额外上下文

### 第二步：建立目录树

1. 使用 `list_dir` 扫描 `Assets/` 下的顶层目录结构
2. 识别所有 `.asmdef`（Assembly Definition）文件的位置，这些定义了代码的逻辑模块边界
3. 根据 `rules/skip_patterns.md` 中的规则，标记需要跳过的目录
4. 建立初步的目录树，记录每个目录下的文件数量和类型分布

### 第三步：Roslyn 结构化分析

在逐目录人工分析之前，先使用 Roslyn 工具自动提取代码结构：

1. **执行分析工具**：
   ```bash
   ./tools/UnityCodeAnalyzer.exe <项目路径> --output analysis.json
   ```
   > 工具位于本 Skill 目录下的 `tools/UnityCodeAnalyzer.exe`，为自包含单文件应用，无需额外安装 .NET Runtime

2. **工具自动提取的信息**：
   - 每个目录下的 `.cs` 文件列表及统计（MonoBehaviour / ScriptableObject / Editor / 纯逻辑类数量）
   - 所有类/接口/结构体/枚举的定义，含继承关系和接口实现
   - 方法签名（参数、返回值、修饰符、是否协程/异步）
   - 字段和属性（类型、序列化标记、特性标注）
   - 事件声明
   - `using` 依赖和跨目录引用关系
   - `.asmdef` Assembly Definition 检测

3. **阅读 JSON 输出**：使用 `view_file` 阅读生成的 `analysis.json`，获取项目的全貌

### 第四步：深入分析

基于 Roslyn JSON 输出，**有针对性地**阅读源码：

1. **无需逐文件阅读**：JSON 已提供完整的类结构和 API 签名
2. **仅对以下情况阅读源码**：
   - JSON 中显示为复杂类（方法多、依赖多）的核心脚本
   - 需要理解业务逻辑（如状态机转换、AI 决策树）的关键实现
   - 涉及架构设计模式的基类和接口
   - 发现有 TODO / HACK 注释的文件
3. **补充 Roslyn 无法提取的信息**：
   - 代码注释中的架构说明
   - 方法体内的 `GetComponent<T>()` 等运行时依赖调用
   - 复杂的条件编译（`#if UNITY_IOS` 等）

### 第五步：递归判断

> **关键原则**：递归判断基于 **代码逻辑的内聚性与关联关系**，不设机械的深度限制。

对每个子目录，按照 `rules/recursion_rules.md` 中的规则决定是否递归。核心判断流程：

```
1. 该子目录是否在跳过列表中？ → 是 → 跳过
2. 子目录内的代码是否与父目录属于同一功能模块？
   → 是 → 递归，但将内容合并到父目录摘要中
3. 子目录是否构成独立的功能子系统？
   → 是 → 递归，生成独立的目录摘要
4. 子目录是否只有极少量（1-2个）简单文件？
   → 是 → 直接在父目录摘要中提及，无需独立摘要
5. 子目录是否有自己的 .asmdef 文件？
   → 是 → 视为独立模块，递归并生成独立摘要
```

### 第六步：生成输出文档

1. **项目概览**：按 `templates/project_overview.md` 模板生成全局概览
2. **目录摘要**：按 `templates/directory_summary.md` 模板为每个分析过的目录生成摘要
3. **最终文档组装**：将项目概览置于文档开头，随后按逻辑顺序排列各目录摘要

## 输出位置

将生成的文档输出到项目根目录下指定的位置（默认为 `docs/project-summary.md`），或按用户要求输出。

## 重要提示

- 分析过程中如发现代码注释中有架构说明或 TODO，应纳入摘要
- 对于特别大的文件（> 500 行），优先关注公共 API 和类结构，不必逐行分析
- 如果项目使用了 Addressables、Scriptable Build Pipeline 等高级特性，应在概览中标注
- 场景文件（`.unity`）和预制体（`.prefab`）不分析内容，但应记录其存在和数量
- `Resources/` 目录需特别标注，因为其内容会被打包且可通过 `Resources.Load` 动态加载
