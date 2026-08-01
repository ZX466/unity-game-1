# Unity-Wander-In-Color-Game

![StartGame](https://github.com/ColorGalaxy/Unity-Wander-In-Color-Game/raw/master/Screenshot/StartGame.jpg)

**漫游色彩 (WanderInColor)** — 一款基于动态颜色匹配机制的2D横板平台跳跃游戏

[![B站视频](https://img.shields.io/badge/B站-游戏合集-blue?logo=bilibili)](https://www.bilibili.com/video/av93165679)

## 项目简介

在学习了Unity一段时间后，从玩法设计到美术风格、从关卡搭建到C#逻辑编码，**从0到1独立完成**的第一款完整的横板闯关小游戏。

**核心创新点**: 背景不再是静态单一图像，而是由**持续变化的纯色色块**组成（左右上下平移），这些颜色变化会实时影响场景中平台、地刺等元素的出现与消失。玩家需要根据背景颜色的变化来判断哪些平台可以安全站立。

## 游戏特色

| 特性 | 描述 |
|------|------|
| 动态颜色匹配 | 背景色块移动触发平台显隐逻辑 |
| 三大关卡 | 瀑布→森林 / 森林→洞穴 / 洞穴→熔洞 |
| 二连跳系统 | 支持二段跳跃 + Coyote Time + Jump Buffer |
| 多平台支持 | PC键盘 + Android触摸/摇杆 |
| 检查点重生 | 死亡后从最近检查点复活 |
| 进度存档 | 关卡解锁 + 最高分记录 |
| 🆕 道具系统 | 6种能力增益道具（速度/跳跃/护盾/磁铁/双倍分/颜色免疫）|
| 🆕 连击系统 | 6级连击倍率，最高3x得分加成 |
| 🆕 冲刺能力 | 快速闪避位移+残影特效 |
| 🆕 颜色护盾 | 与颜色匹配联动的防御系统 |
| 🆕 时间挑战 | 4种挑战模式（竞速/目标分/生存/收集）|

## 游戏截图

### 关卡选择界面
![LevelSelect](https://github.com/ColorGalaxy/Unity-Wander-In-Color-Game/raw/master/Screenshot/LevelSelect.jpg)

### UI界面
![UI](https://github.com/ColorGalaxy/Unity-Wander-In-Color-Game/raw/master/Screenshot/UI.png)

### 第一关：瀑布-森林
![Waterfall2](https://github.com/ColorGalaxy/Unity-Wander-In-Color-Game/raw/master/Screenshot/Waterfall2.png)

### 第二关：森林-山洞
![Forest](https://github.com/ColorGalaxy/Unity-Wander-In-Color-Game/raw/master/Screenshot/Forest.png)
![Forest2](https://github.com/ColorGalaxy/Unity-Wander-In-Color-Game/raw/master/Screenshot/Forest2.png)

### 第三关：山洞-熔洞
![Level3](https://github.com/ColorGalaxy/Unity-Wander-In-Color-Game/raw/master/Screenshot/Level3.png)

## 技术栈

| 类别 | 技术 |
|------|------|
| 引擎 | Unity 2022.3.2t13 |
| 语言 | C# (.NET Standard 2.1) |
| 物理引擎 | Box2D (Unity内置) |
| 触摸插件 | EasyTouch 5.x |
| UI系统 | uGUI (Canvas + RectTransform) |
| 动画系统 | Animator Controller |
| 音频系统 | AudioSource + AudioClip |
| 数据持久化 | PlayerPrefs |
| 目标平台 | Windows, Android |

## 操作说明

### PC端
| 按键 | 功能 |
|------|------|
| A/D 或 ←/→ | 左右移动 |
| 空格 | 跳跃（支持二连跳）|
| LeftShift / C | 冲刺（快速闪避）|
| F / V | 激活颜色护盾 |
| ESC | 暂停游戏 |

### Android端
| 操作 | 功能 |
|------|------|
| 左侧虚拟摇杆 | 移动角色 |
| 右侧跳跃按钮 | 跳跃（支持二连跳）|
| 冲刺按钮 | 快速闪避位移 |
| 护盾按钮 | 激活颜色护盾 |
| 暂停按钮 | 暂停菜单 |

## 新增玩法详解

### 道具系统
场景中散布6种道具，拾取后获得临时能力：

```
🟡 SpeedBoost  → 移动速度 ×1.5 (5秒)
🔵 JumpBoost   → 跳跃高度 ×1.5 (5秒)
🛡️ Shield      → 免疫一次死亡 (5秒)
🧲 Magnet      → 自动吸引金币 (5秒)
⭐ DoubleScore → 得分翻倍 (5秒)
🌈 ColorImmunity → 颜色匹配时获得护盾加成
```

### 连击系统
连续执行有效动作累积连击，触发得分倍率：

```
0-4x  →  1.0x 倍率
5-9x  →  NICE!    1.2x 🟢
10-19x → GREAT!   1.5x 🔵
20-34x → AWESOME! 2.0x 🟣
35-49x → INCRED!  2.5x 🔴
50+x  →  LEGEND!  3.0x 🟡
```

**可累积动作**: 收集金币、拾取道具、击败敌人、完美落地、冲刺穿越、连续跳跃

### 冲刺能力
- 无敌帧快速位移，可穿越危险区域
- 冷却0.8秒，空中可刷新使用次数
- 残影特效增强视觉反馈

### 时间挑战模式
4种预设模式增加重玩价值：
- **竞速**: 120秒内尽快通关（3x奖励）
- **目标分**: 90秒内达到2000分（2x奖励）
- **生存**: 在60秒内存活到底（4x奖励）
- **收集**: 45秒内收集30个金币（2x奖励）

## 核心机制详解

### 颜色匹配算法

```
背景色块(BackColor Tag) 进入触发器
    ↓
RGB值近似比较 (Mathf.Approximately)
    ↓
┌─────────────────┬──────────────────┐
│   颜色相同       │    颜色不同       │
├─────────────────┼──────────────────┤
│ 平台淡出隐藏     │ 平台淡入显示      │
│ 地刺激活(危险)   │ 地刺失活(安全)    │
│ Collider禁用     │ Collider启用      │
└─────────────────┴──────────────────┘
```

### 玩家状态机

```
[Idle] ←→ [Run] ←→ [Jump] → [DoubleJump] → [Fall] → [Land]
                ↑                                              ↓
                └──────────────────────────────────────────────┘
```

## 项目结构

```
Assets/
├── Scripts/                  # C#脚本
│   ├── GameManager.cs        # 全局游戏管理器（单例）
│   ├── Player/               # 玩家相关
│   │   ├── PlayerControl.cs  # 主控制器（物理+动画）
│   │   ├── InputManager.cs   # 统一输入管理
│   │   └── JoystickControl.cs # EasyTouch摇杆适配
│   ├── Background/           # 背景和颜色系统
│   │   └── ColorJudge.cs     # 颜色匹配核心逻辑
│   ├── Monster/              # 敌人和陷阱
│   ├── UI/                   # 用户界面
│   │   ├── GameStateUI.cs    # 游戏状态UI
│   │   └── LevelSelectUI.cs  # 关卡选择界面
│   ├── BGM/                  # 音频系统
│   │   └── AudioManager.cs   # 音频管理器
│   ├── Common/               # 公共组件
│   │   ├── EventManager.cs   # 事件分发系统
│   │   └── ParticleManager.cs # 粒子特效管理
│   └── DeadComponent/        # 死亡和重生
│       └── CheckPoint.cs     # 检查点组件
├── EasyTouch/               # 第三方触摸插件
├── Material/                 # 美术资源
└── Prefabs/                  # 预制体
```

## 开发环境配置

### 前置要求
- Unity 2022.3.2t13
- Visual Studio 2017+ 或 VS Code (带C#扩展)
- JDK 8+ (Android构建)
- Android SDK (Android构建)

### 导入步骤
1. 使用Unity Hub打开项目目录
2. 等待资源导入和编译完成
3. 打开 `start` 场景作为启动场景
4. 点击Play运行游戏

### 场景列表
| 场景名 | 用途 |
|--------|------|
| start | 主菜单 |
| LevelSelect | 关卡选择 |
| waterfall | 第一关：瀑布森林 |
| cave | 第二关：森林洞穴 |
| volcanocave | 第三关：洞穴熔洞 |
| login / register | 登录注册（遗留） |

## 架构文档

- [架构优化总结](./ArchitectureRefactor.md) - 完整的技术文档，包含所有模块说明
- [高级架构设计](./AdvancedArchitecture.md) - 架构设计思路和代码示例
- [重构记录](./REFACTOR_SUMMARY.md) - 重构过程总结

## 获奖记录

凭借该游戏在本科期间获得以下比赛奖项：
- 🏆 **世纪杯**
- 🏆 **发现杯**
- 🏆 **葫芦岛杯**

## 开发时间线

```
2017.03  项目启动，学习Unity基础
2017.05  完成核心玩法原型
2017.07  完成三大关卡设计
2017.09  优化美术和音效
2017.10  添加UI系统和存档功能
2017.11  Android移植和测试
2017.12  项目提交参赛
```

## 许可证

本项目仅供学习交流使用。

## 致谢

- **EasyTouch** - 强大的Unity触摸插件
- **Unity Technologies** - 优秀的游戏引擎
