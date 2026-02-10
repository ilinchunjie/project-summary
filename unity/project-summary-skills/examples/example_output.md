# 📋 项目概览：SpaceRogue

## 基本信息

| 属性       | 值                |
| ---------- | ----------------- |
| Unity 版本 | 2022.3.18f1       |
| 渲染管线   | URP               |
| 目标平台   | Android / iOS     |
| 脚本后端   | IL2CPP            |
| .NET 版本  | .NET Standard 2.1 |

## 架构概述

SpaceRogue 是一款太空 Roguelike 射击游戏。项目使用事件驱动架构，核心系统通过 `GameEventBus` 解耦。游戏逻辑分为 Core（核心框架）、Gameplay（战斗/关卡）、UI（界面）三大模块，通过 Assembly Definition 隔离编译。战斗系统采用 ECS-like 的组件组合模式，敌人 AI 使用行为树。

## 目录结构

```
Assets/
├── Scripts/            — 全部 C# 逻辑代码
│   ├── Core/           — 框架层：事件总线、对象池、状态机、存档
│   ├── Gameplay/       — 游戏逻辑：战斗、关卡生成、道具、AI
│   ├── UI/             — 界面系统：HUD、菜单、弹窗
│   └── Editor/         — 编辑器工具：关卡编辑器、数据导入
├── Prefabs/            — 83 个预制体
├── Scenes/             — 4 个场景（Boot, MainMenu, Game, Loading）
├── Resources/          — 动态加载配置表（JSON）
├── ScriptableObjects/  — 武器/敌人/关卡配置数据
├── Art/                — 美术资源（Spine 动画、UI 图集）
└── Plugins/            — DOTween、Addressables
```

## Assembly Definition 模块划分

| Assembly              | 路径                | 职责           | 依赖                                     |
| --------------------- | ------------------- | -------------- | ---------------------------------------- |
| `SpaceRogue.Core`     | `Scripts/Core/`     | 框架层基础设施 | 无                                       |
| `SpaceRogue.Gameplay` | `Scripts/Gameplay/` | 游戏运行时逻辑 | `SpaceRogue.Core`                        |
| `SpaceRogue.UI`       | `Scripts/UI/`       | 界面系统       | `SpaceRogue.Core`                        |
| `SpaceRogue.Editor`   | `Scripts/Editor/`   | 编辑器扩展工具 | `SpaceRogue.Core`, `SpaceRogue.Gameplay` |

## 入口与启动流程

1. **启动场景**：`Boot.unity`
2. **启动脚本**：`GameBootstrap : MonoBehaviour`
3. **初始化顺序**：
   - `GameBootstrap.Awake()` → 初始化 `ServiceLocator`
   - 注册核心服务：`EventBus`、`ObjectPool`、`SaveManager`
   - 加载全局配置 ScriptableObject
   - 异步加载 `MainMenu` 场景

## 第三方依赖

| 包名                     | 版本    | 用途                 |
| ------------------------ | ------- | -------------------- |
| `com.unity.addressables` | 1.21.19 | 资源异步加载与热更新 |
| `com.unity.textmeshpro`  | 3.0.6   | 高质量文本渲染       |
| DOTween (手动导入)       | 1.2.745 | 动画补间             |
| Spine-Unity (手动导入)   | 4.1     | 2D 骨骼动画          |

## 构建与部署

- **构建方式**：Jenkins CI + 自定义 `BuildPipeline` 脚本
- **特殊配置**：使用 Addressables 分包，首包 < 150MB
- **多平台差异**：iOS 使用 `#if UNITY_IOS` 处理 IAP 和推送

---

## 📁 `Scripts/Core/` — 框架层基础设施

**所属模块 / Assembly**：`SpaceRogue.Core`

### 概述

Core 模块提供与游戏逻辑无关的底层框架能力，包括事件总线、对象池、有限状态机和存档系统。所有其他模块依赖 Core，但 Core 不依赖任何业务模块。

### 核心脚本

| 文件名                          | 类型                 | 职责                                |
| ------------------------------- | -------------------- | ----------------------------------- |
| `GameEventBus.cs`               | 纯逻辑类 (Singleton) | 全局事件发布/订阅系统，支持泛型事件 |
| `ObjectPoolManager.cs`          | MonoBehaviour        | 通用对象池，支持自动扩容和预热      |
| `StateMachine.cs`               | 纯逻辑类             | 泛型有限状态机，支持状态栈          |
| `IState.cs`                     | 接口                 | 状态接口，定义 Enter/Execute/Exit   |
| `SaveManager.cs`                | 纯逻辑类 (Singleton) | JSON 序列化存档，支持多存档槽       |
| `ServiceLocator.cs`             | 纯逻辑类             | 轻量级服务定位器，替代硬编码依赖    |
| `MonoSingleton.cs`              | MonoBehaviour        | 泛型 MonoBehaviour 单例基类         |
| `Extensions/UnityExtensions.cs` | 纯逻辑类             | Transform、Vector3 等常用扩展方法   |

### 关键接口

```csharp
// 事件总线
public static class GameEventBus
{
    public static void Publish<T>(T eventData) where T : struct;
    public static void Subscribe<T>(Action<T> handler) where T : struct;
    public static void Unsubscribe<T>(Action<T> handler) where T : struct;
}

// 状态机
public interface IState
{
    void Enter();
    void Execute(float deltaTime);
    void Exit();
}

public class StateMachine<T>
{
    public IState CurrentState { get; }
    public void ChangeState(IState newState);
    public void PushState(IState state);
    public IState PopState();
}

// 服务定位器
public static class ServiceLocator
{
    public static void Register<T>(T service);
    public static T Get<T>();
}
```

### 依赖关系

- **引用**：无外部依赖（纯框架层）
- **被引用**：`SpaceRogue.Gameplay`、`SpaceRogue.UI`、`SpaceRogue.Editor` — 所有模块依赖 Core 的事件总线和基础设施

### 备注

- `GameEventBus` 使用结构体事件避免 GC 分配，适合移动端
- `Extensions/` 子目录仅含 1 个工具文件，已合并在此摘要中

---

## 📁 `Scripts/Gameplay/` — 游戏运行时逻辑

**所属模块 / Assembly**：`SpaceRogue.Gameplay`

### 概述

Gameplay 模块包含所有战斗相关的运行时逻辑，分为武器系统、敌人 AI、关卡生成和道具四个子系统。使用组件组合模式构建实体，通过 `GameEventBus` 与其他模块通信。

### 核心脚本

| 文件名                         | 类型                   | 职责                                 |
| ------------------------------ | ---------------------- | ------------------------------------ |
| `PlayerController.cs`          | MonoBehaviour          | 玩家输入处理与移动控制               |
| `HealthComponent.cs`           | MonoBehaviour          | 通用生命值组件，支持无敌帧和护盾     |
| `DamageSystem.cs`              | 纯逻辑类               | 伤害计算核心，处理暴击/元素克制/减伤 |
| `WeaponBase.cs`                | MonoBehaviour (抽象类) | 武器基类，定义开火/装填/冷却接口     |
| `Weapons/LaserGun.cs`          | MonoBehaviour          | 激光武器实现                         |
| `Weapons/MissileL launcher.cs` | MonoBehaviour          | 导弹武器实现                         |
| `AI/BehaviorTree.cs`           | 纯逻辑类               | 行为树引擎，支持序列/选择/装饰节点   |
| `AI/EnemyBrain.cs`             | MonoBehaviour          | 敌人 AI 控制器，组装行为树           |
| `LevelGenerator.cs`            | MonoBehaviour          | 程序化关卡生成，随机房间+走廊        |
| `LootTable.cs`                 | ScriptableObject       | 掉落物权重配置表                     |

### 关键接口

```csharp
public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] private WeaponConfig config;
    public event Action<int> OnAmmoChanged;
    public abstract void Fire(Vector3 direction);
    public virtual void Reload();
    public float Cooldown { get; }
}

public class HealthComponent : MonoBehaviour
{
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDeath;
    public void TakeDamage(DamageInfo info);
    public void Heal(float amount);
    [SerializeField] private float maxHealth;
    [SerializeField] private float invincibleDuration;
}
```

### 依赖关系

- **引用**：`SpaceRogue.Core` — 使用事件总线、对象池、状态机
- **被引用**：`SpaceRogue.UI` — UI 监听战斗事件显示 HUD；`SpaceRogue.Editor` — 关卡编辑器读取关卡数据

### 备注

- `Weapons/` 子目录与父目录属于同一战斗模块，已合并分析
- `AI/` 子目录为独立子系统（7 个脚本，含行为树引擎），建议查看其独立摘要
- `DamageSystem` 中存在 TODO 注释："需要重构元素克制计算，当前硬编码了克制表"

---

## 📁 `Scripts/Gameplay/AI/` — 敌人 AI 子系统

**所属模块 / Assembly**：`SpaceRogue.Gameplay`

### 概述

独立的行为树 AI 框架，包含行为树引擎核心和预定义的 AI 行为节点。`EnemyBrain` 作为入口组件挂载在敌人预制体上，根据 `AIConfig`（ScriptableObject）组装行为树。

### 核心脚本

| 文件名                    | 类型              | 职责            |
| ------------------------- | ----------------- | --------------- |
| `BehaviorTree.cs`         | 纯逻辑类          | 行为树执行引擎  |
| `BTNode.cs`               | 纯逻辑类 (抽象类) | 行为树节点基类  |
| `Composites/Sequence.cs`  | 纯逻辑类          | 顺序组合节点    |
| `Composites/Selector.cs`  | 纯逻辑类          | 选择组合节点    |
| `Actions/ChaseAction.cs`  | 纯逻辑类          | 追踪玩家行为    |
| `Actions/AttackAction.cs` | 纯逻辑类          | 攻击行为        |
| `Actions/PatrolAction.cs` | 纯逻辑类          | 巡逻行为        |
| `AIConfig.cs`             | ScriptableObject  | AI 行为参数配置 |
| `EnemyBrain.cs`           | MonoBehaviour     | AI 控制器入口   |

### 关键接口

```csharp
public abstract class BTNode
{
    public enum Status { Running, Success, Failure }
    public abstract Status Evaluate(AIContext context);
}

public class BehaviorTree
{
    public void SetRoot(BTNode root);
    public void Tick(float deltaTime);
}

public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private AIConfig config;
    public void Initialize(BehaviorTree tree);
}
```

### 依赖关系

- **引用**：`SpaceRogue.Core` — 使用状态枚举和工具方法
- **被引用**：仅在 `Gameplay` 模块内部使用（`EnemySpawner` 创建敌人时注入 `EnemyBrain`）

### 备注

- 行为树引擎是自研实现，未使用第三方 BT 框架
- `Composites/` 和 `Actions/` 子目录文件简单且属于同一行为树框架，已合并分析
