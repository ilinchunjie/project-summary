# 跳过规则

以下目录和文件在分析过程中应跳过，不进行内容分析。

## 跳过的目录

### Unity 生成目录（不包含用户代码）
- `Library/` — Unity 缓存与导入数据
- `Temp/` — 临时构建文件
- `Logs/` — 日志输出
- `obj/` — 编译中间产物
- `Build/` / `Builds/` — 构建输出
- `MemoryCaptures/` — 内存快照
- `Recordings/` — Unity Recorder 输出

### 版本控制 / IDE
- `.git/`
- `.vs/`、`.idea/`、`.vscode/`
- `UserSettings/` — 用户个人设置

### 第三方插件源码（仅记录存在，不逐文件分析）
- `Assets/Plugins/` 下的第三方库目录
- `Assets/TextMesh Pro/`
- `Packages/` — Unity Package Manager 管理的包（缓存在 Library 中）
- 其他明确标记为第三方的目录（通常有 LICENSE 或 README 说明）

> **注意**：如果 `Plugins/` 下有项目自研的原生插件 wrapper，仍应分析。判断标准是目录中是否有项目特定的 C# 封装代码。

## 跳过的文件类型

### 元数据
- `*.meta` — Unity 资产元数据，不含逻辑

### 非代码资产（记录数量和组织方式，不分析内容）
- 纹理：`*.png`、`*.jpg`、`*.tga`、`*.psd`、`*.exr`
- 模型：`*.fbx`、`*.obj`、`*.blend`
- 音频：`*.wav`、`*.mp3`、`*.ogg`
- 动画：`*.anim`、`*.controller`（记录存在即可）
- 材质：`*.mat`（记录存在即可）
- Shader：`*.shader`、`*.hlsl`、`*.cginc`（**应分析**，但列在这里提醒区分对待）

### 序列化数据（根据需要选择性查看）
- `*.unity` — 场景文件（记录名称和数量，不分析 YAML 内容）
- `*.prefab` — 预制体（记录名称和数量，不分析 YAML 内容）
- `*.asset` — ScriptableObject 实例（记录存在，关注对应的 SO 定义脚本）

### 构建相关
- `*.dll`、`*.so`、`*.a` — 编译后的二进制
- `*.aar`、`*.jar` — Android 库

## 特殊标注目录

以下目录**不跳过**，但需要特殊处理：

- **`Resources/`**：内容会被打包，标注其存在和包含的资源类型
- **`StreamingAssets/`**：原样复制到构建中，标注其内容
- **`Editor/`**：仅在编辑器中运行的代码，与运行时代码分开分析
- **`Editor Default Resources/`**：编辑器资源目录
