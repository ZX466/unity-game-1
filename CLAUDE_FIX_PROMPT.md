# Unity-Wander-In-Color-Game — 完整修复提示词

> 本文件由团队（Kimi Lead + 4 位队友）对项目进行只读式深度勘察后汇编而成。
> **所有发现问题均为仅检查未修改，请 Claude 按此提示词进行修复。**

---

## 项目背景

Unity 2022.3.2t13 2D 横板平台跳跃游戏《漫游色彩》。核心创新：背景由持续移动的纯色色块组成，平台/地刺的显隐与背景颜色匹配实时联动（颜色相同→平台隐藏/地刺激活；颜色不同→平台显示/地刺安全）。目标平台：PC + Android。

**版本矛盾**：README 写的是 Unity 2017 + .NET 3.5，实际 `ProjectSettings/ProjectVersion.txt` 是 2022.3.2t13。请统一 README。

---

## 优先级定义

| 优先级 | 含义 |
|--------|------|
| **P0** | 必然触发的数据损坏/游戏崩溃 bug |
| **P1** | 功能异常、性能问题、架构隐患 |
| **P2** | 逻辑打磨、代码清理、正确的代码无需修改的确认 |

---

## P0 — 必须优先修复

### P0-1: 关卡编号映射必然误判，正在污染存档数据

**位置**: `Assets/Scripts/GameManager.cs:379-385` — `GetLevelIndexByName()`

**问题**: 使用 `Contains` 链判断关卡索引：
```csharp
if (sceneName.Contains("waterfall")) return 0;
if (sceneName.Contains("cave"))      return 1;   // ← "volcanocave" 在此被截获
if (sceneName.Contains("volcano"))   return 2;   // ← 永远到不了
```
实际场景名为：`waterfall`(第1关) / `cave`(第2关) / `volcanocave`(第3关)。`"volcanocave".Contains("cave") == true` 且 `cave` 判断在 `volcano` 之前 → **第3关被识别为 index 1**。

**后果**: `SaveBestScoreForCurrentLevel()`(:392-403) 把第3关最高分写入 `Level2Score`，第3关成绩永久无法记录，第2关成绩被污染。

**修复**: 改为精确相等匹配（`switch`/`==`），或调整判断顺序使 `volcano` 先于 `cave`。

---

### P0-2: 关卡解锁存在回退覆盖，会重新锁上已解锁关卡

**位置**: 
- `Assets/Scripts/GameManager.cs:364-376` — `UnlockNextLevel()`
- `Assets/Scripts/Common/SaveManager.cs:63-67` — `UnlockLevel()`

**问题**: `UnlockNextLevel()` 计算 `Mathf.Clamp(nextLevelIndex + 1, 1, 3)` 后调 `SaveManager.UnlockLevel()`，后者是 `PlayerPrefs.SetInt` **无条件覆盖写**。玩家已解锁第3关后重玩第1关通关，`UnlockedLevel` 被**回退写成 2，第3关重新锁上**。

对比 `LevelSelectUI.UnlockLevel()`(:196-205) 有 `if (levelNumber > _unlockedLevel)` 保护——同一语义两处实现不一致。

**修复**: `SaveManager.UnlockLevel()` 内加 `Mathf.Max(GetUnlockedLevel(), levelNumber)` 保护。

---

### P0-3: 五个核心 Manager 全部没有挂进任何场景

**证据**: GUID 反查 `Assets/**/*.unity` 与 `*.prefab`：
- `ComboSystem` / `TimeAttackManager` / `QuestSystem` / `SaveManager` / `GameManager` / `GameplayHUD` / `textshow` / `EventManager`
- **全部 0 匹配**（只有 `PlayerControl` 和 `time` 真实在场景中）

**后果**: 所有 `Instance` 访问走 lazy-create `new GameObject(...).AddComponent<...>()`，Inspector 序列化字段全为默认值。关键是 `ComboSystem` 的 4 个 `UnityEvent`(:79-82) 在此路径下**保持 null**（`UnityEvent` 只在 Unity 反序列化时实例化），而 `GameplayHUD.SetupEventListeners()`(:114-119) 里 `ComboSystem.Instance.OnComboChanged.AddListener(...)` 是**裸访问**→ 直接 `NullReferenceException`。

**修复**:
1. 建 `_Managers` Prefab，挂 `GameManager`/`SaveManager`/`ComboSystem`/`TimeAttackManager`/`QuestSystem`，在 `start` 场景实例化一次并 `DontDestroyOnLoad`
2. `ComboSystem.Awake()` 补 `DontDestroyOnLoad(gameObject)`，与其余四个 Manager 一致
3. `GameplayHUD` 的裸 `AddListener` 加 `?.` 作纵深防御

---

## P1 — 功能异常与架构隐患

### P1-1: `time.cs` 计时器永不归零 + static 字段问题

**位置**: `Assets/Scripts/UI/time.cs`

**问题**:
1. `:13` `static public float time_by` 跨场景累加，且**唯一清零入口 `reTimer()` 在全库 `*.cs` 与 `*.unity` 中 0 处调用**→ 计时器永不重置，玩到第三关显示的是从第一关开始的累计时间
2. `:11` `static public Text textTime` + `:15` 实例 `Start()` 中 `textTime = GetComponent<Text>()` → 每个实例抢占 static 字段。三个关卡各挂一个实例，同场景内只有一个所以不互抢，但靠运气而非设计
3. `:27` `hour` 赋值行被注释掉 → `hour` 永远为 0；`:28-29` `minute` 超过 60 分钟后不进位，显示为 `60:xx`、`61:xx`（`hour * 3600` 是死代码）
4. `:19-20` `Update()` 无条件访问 `GameManager.Instance` — 由于 GameManager 不在场景中，这是**首个触发其 lazy-create 的入口**，导致 GameManager 所有 Inspector 字段为默认值

**额外**: `textshow.cs:18` 在 `Update()` 中 `textTshow.text = time.textTime.text;` **裸解引用 static 字段无 null 检查**。两脚本 `executionOrder` 均为 0，若 `textshow.Update` 早于 `time.Start`，每帧抛 `NullReferenceException`。

**修复方案（最佳性价比—同时修掉 4 个问题）**:
- **删除 `time.time_by`**：`time.cs` 退化为纯显示层，数据源统一读 `GameManager.GameTime`（已有 `GetFormattedTime()` :411-416）
- `textTime` 从 `static public` 改为实例私有字段，`textshow` 改用 Inspector 引用或直接读 `GameManager`
- `reTimer()` 未接线的问题自动消失（重置由 `GameManager.ResetPlayerState()`(:290) 统一负责）；`TimeChallengeQuest` 的 30 秒判定自动与 UI 一致；`hour` 死代码与无进位问题一并解决

---

### P1-2: int 溢出 — 正反馈自引用才是根因

**位置**: 
- `Assets/Scripts/Common/TimeAttackManager.cs:309` — `CalculateBonus()` TargetScore 分支
- `Assets/Scripts/Common/ComboSystem.cs:221-225` — `CalculateComboBonus()` 平方增长
- `Assets/Scripts/GameManager.cs:116/340-344` — `TotalScore` int 无钳制

**问题**: `PlayerPrefs` 只有 int/float/string，没有 long 接口，所以改 `SaveManager` 的存储类型解决不了问题。溢出发生在写入**前**的累加。

**爆炸增长源头 —** `TimeAttackManager.CalculateBonus()`(:309) 的 TargetScore 分支：
```csharp
return baseBonus + GameManager.Instance.TotalScore / 2;
```
**奖励基于当前总分本身**，构成正反馈：每次完成挑战总分 ×1.5，指数增长，约 50 次即溢出 `int.MaxValue`。

**次要来源**: `ComboSystem.CalculateComboBonus()` = `combo² × 0.5 × multiplier`（最高 3x），combo=100 时约 15000 分，量级有限。

**后果**: C# 默认 `unchecked`，溢出**静默回绕成负数**，之后 `SaveManager.SaveLevelScore()` 的 `if (score > prev)`(:77) 与 `LevelSelectUI` 的 `_levelScores[i] > 0`(:96) 判断全部失效。

**修复**:
1. **移除 `CalculateBonus()`(:309) 的 `+ TotalScore / 2` 自引用**，改为基于 `_currentConfig.TargetValue` 的固定比例
2. `AddScore()`(`:340-344`) 加钳制：`TotalScore = (int)Mathf.Min((long)TotalScore + value, int.MaxValue)`
3. `CalculateBonus()` 的 SpeedRun 分支(:307) 加 `Mathf.Max(0, ...)` 下限

**注意**: 以下**不修**（经核实为正确代码）：
- `ComboSystem.cs:155` `Mathf.RoundToInt(baseScore * currentTier.ScoreMultiplier)` — 倍率(1.0~3.0)与 baseScore(5~15)均为小整数量级，float 24 位尾数精确可表示
- 单个 `Mathf.RoundToInt` 调用——本身不溢出

---

### P1-3: 存档无版本号/迁移机制 + 三处独立直写

**位置**: 
- `Assets/Scripts/Common/SaveManager.cs:30-34` — 键名硬编码，无版本号
- `Assets/Scripts/GameManager.cs:392-403` — `SaveBestScoreForCurrentLevel()` **绕过 SaveManager** 自己拼 `$"Level{levelNumber}Score"` 直写
- `Assets/Scripts/UI/LevelSelectUI.cs:206-215` — 第三份直写
- `Assets/Scripts/UI/LevelSelectUI.cs:49-66` — `LoadLevelData()` 也直读 `"UnlockedLevel"` 而非调 `GetUnlockedLevel()`

**后果**: `SaveManager` 的封装被完全架空，改键名即静默失效。`LevelSelectUI.cs:56` 有 `if (_unlockedLevel < 1) _unlockedLevel = 1;` 注释为 "older saves may have stored 0-based indices" — **说明历史上已经发生过一次数据格式事故**，正是缺少版本机制的代价。

**修复**:
1. `GameManager.SaveBestScoreForCurrentLevel()` 与 `LevelSelectUI` 全部改走 `SaveManager`
2. 键名常量只在 `SaveManager` 保留一份
3. 引入 `KEY_SAVE_VERSION`，在 `SaveManager` 中集中处理迁移，替掉 `LevelSelectUI.cs:56` 散落的兼容补丁
4. `SaveManager` 统一用 `GetInt(key, default)` 风格，去掉 `HasKey` 双查（`:46/:58`）

---

### P1-4: 写盘风暴（Android 端影响明显）

**位置**: `Assets/Scripts/GameManager.cs:332-343` — `CollectCoin()` / `AddScore()`

**问题**: `CollectCoin()`(:335) 与 `AddScore()`(:342) **每次都调 `SavePlayerData()`**，而 `SaveTotalScore`/`SaveCoins` 内部各自 `PlayerPrefs.Save()` → 一次收金币触发 **2 次同步磁盘 flush**。连击系统每个动作都走 `AddScore`，高频 combo 下每帧多次同步写盘。

**修复**: 改为标记 dirty，在场景切换、暂停、`OnApplicationPause`、`OnApplicationQuit` 时统一 flush；移除 `SaveTotalScore`/`SaveCoins` 内部的 `PlayerPrefs.Save()`，由调用方决定 flush 时机。

---

### P1-5: `PlayerControl` 与 `TwiceJump` 冲突

**位置**:
- `Assets/Scripts/Player/PlayerControl.cs:153-165/167-180/198-218/295-305`
- `Assets/Scripts/Player/TwiceJump.cs:13-19`

**问题**:
1. **字段名大小写可能不一致**: `PlayerControl` 读写 `GameManager.Instance.JumpTime` / `JumpFlag`，而 `TwiceJump` 读写 `GameManager.getInstance().jumptime` / `jumpFlag`。如果 GameManager 中同时存在 PascalCase 和 camelCase 两套字段，两者将维护完全独立的跳跃计数
2. **功能重复+执行竞态**: `PlayerControl` 已通过 `CheckGround()`（射线检测）+ `OnLand()` 在落地时统一重置 `JumpTime=0` / `JumpFlag=true`；`TwiceJump` 又通过 `OnTriggerEnter2D` 在接触 Ground 时重复做同样的事。若两个回调在同一帧触发，会产生冗余写入，且 `TwiceJump` 的触发器依赖 `Collider2D isTrigger`，覆盖范围与射线检测不一致
3. **`TwiceJump` 是僵尸脚本**: `Start()` 和 `Update()` 为空实现，与 `PlayerControl` 的状态机无任何同步或优先级约定

**修复**:
1. **移除 `TwiceJump.cs`**（功能已被 `PlayerControl` 完全覆盖）
2. 全项目搜索确认无场景/预制体引用该组件，如有则解除引用
3. **统一 GameManager 跳跃字段访问**: 在 `GameManager` 中将跳跃状态封装为方法（如 `ResetJump()`、`ConsumeJump()`），避免外部脚本直接修改字段

---

### P1-6: `ColorJudge` 新旧两版并存 + `OnPlatformGroundedChanged` 事件从未触发

**位置**:
- `Assets/Scripts/Background/ColorJudge.cs` — 新版（渐变+Collider控制）
- `Assets/Scripts/Background/ColorJudge_Pubu.cs` — 旧版（直接 `SetActive`，浮点 `==` 比较）

**问题**:
1. **`ColorJudge_Pubu.cs` 是孤立死代码**: 全项目搜索无任何 `.cs` / `.unity` / `.prefab` 引用它，可直接删除
2. **`OnPlatformGroundedChanged` 事件从未触发**: `ColorJudge.cs:34` 定义了 `public static event System.Action<bool> OnPlatformGroundedChanged`。`PlayerControl.cs:130` 订阅了该事件(:135 取消订阅)。但 `ColorJudge.cs` 中**没有任何地方调用 `?.Invoke(grounded)`** — 该事件机制完全失效
3. **旧版使用浮点直接相等比较**: `ColorJudge_Pubu.cs:33` 用 `==` 比较颜色，非 `Mathf.Approximately`，浮点精度可能误判
4. **旧版 `Awake()` 无 null 检查**: (`:17-20`) 直接 `.GetComponent<Renderer>().material.color`，挂载错误时抛异常

**修复**:
1. **删除 `ColorJudge_Pubu.cs`**
2. **修复事件触发**: 在 `ColorJudge.cs` 的 `FadeRoutine()` 末尾，平台显隐完成时调用 `OnPlatformGroundedChanged?.Invoke(visible)`（当前 `:254` 已有该行但被 `#if`/其他逻辑阻断？重新确认）
3. `CheckPlayerOnPlatform()` 中使用 `Physics2D.OverlapCircleAll` 的检测机制也可能触发该事件，统一入口

**注意**: Aion CLI 报告中提到的 `OnTriggerStay2D` 每帧开关 Collider 和 `MoveToNextPlatform` 缺少边界检查，这些内容**在 `ColorJudge.cs` 实际代码中不存在**（真实代码只有 `OnTriggerEnter2D`/`OnTriggerExit2D` 和 `FadeTo` 方法），请忽略。

---

### P1-7: `QuestSystem` 奖励发放绕过公开方法 + 无反订阅

**位置**: `Assets/Scripts/Common/QuestSystem.cs`

**问题**:
1. **`:77-81` `GiveReward()` 直接改字段**:
   ```csharp
   GameManager.Instance.AddScore(RewardScore);          // ← 会触发 SavePlayerData()
   GameManager.Instance.CoinsCollected += RewardCoins;  // ← 直接改字段，绕过 CollectCoin()
   ```
   两个后果：(a) 不触发 `OnCoinCollected` 事件——这反而**幸运地避免递归**；(b) `CoinsCollected` 变更**不落盘**，只有 `AddScore` 内部的 `SavePlayerData()` 恰好把它一起写进去——**顺序依赖**
2. **`:118-122` 无反订阅**: `SetupEventListeners()` 在 `Awake()` 订阅 `GameManager` 的 event，但全类无 `OnDestroy`/`OnDisable` 反订阅。当前靠双方都 `DontDestroyOnLoad` 掩盖
3. **`:185-188` `GetQuestProgressPercent()`**: `(float)CurrentCount / RequiredCount` 未防 `RequiredCount == 0` 除零，未 `Mathf.Clamp01` 防止 `UpdateProgress` 允许的超出

**修复**:
1. `GiveReward()` 改走 `GameManager` 公开方法（如有）或显式 `SavePlayerData`
2. 补 `OnDestroy()` 反订阅
3. `GetQuestProgressPercent()` 加除零防护 + `Clamp01`

---

## P2 — 逻辑打磨与代码清理

### P2-1: `ComboSystem` 多项逻辑缺陷

**位置**: `Assets/Scripts/Common/ComboSystem.cs`

**问题**:
1. **`:10` `ComboAction.EnemyDefeat` 死枚举**：全库无调用点，补上调用或删除
2. **`:54` `MaxComboDisplay = 999` 从未使用**：`GetComboText()`(:229-238) 不做截断
3. **`:180` `ResetCombo()` 设 `_lastActionTime = 0f`**：`Time.time` 单调增长，语义脆弱。建议改 `float.NegativeInfinity`
4. **`:159-174` `BreakCombo()` 在 `_currentCombo == 0` 时也执行**：`PlayerControl.cs:414/469` 无条件调用。开头加 `if (_currentCombo <= 0) return;`
5. **`:172` 日志打印 `_maxComboThisRun` 从不重置**：只有 `ResetRunStats()`(:186-191) 会重置，而它**全库 0 处调用**→ 日志误导
6. **`:200` `ComboTiers[0]` 无防御**: Inspector 中清空 List 即 `IndexOutOfRangeException`
7. **`:143` `prevTier == currentTier == tier[0]`**：归零后首次调用时，`OnComboTierChanged` 不触发 → HUD 文案残留上一次的 "LEGENDARY!"
8. **`:99-107` Awake 缺 `DontDestroyOnLoad`**：与其他 Manager 不一致

**修复**:
1. 删除 `EnemyDefeat` 或补调用点
2. `_lastActionTime` 改 `float.NegativeInfinity`
3. `BreakCombo()` 开头加 `if (_currentCombo <= 0) return;`
4. 把 `ResetRunStats()` 接线到关卡初始化
5. `GetTierForCount()` 加空列表防御
6. `ResetCombo()` 中主动清 HUD 文案
7. 补 `DontDestroyOnLoad`

---

### P2-2: `TimeAttackManager` 结构完整但无人启动 + 逻辑缺陷

**位置**: `Assets/Scripts/Common/TimeAttackManager.cs`

**问题**:
1. **四个模式结构上完整**（`InitializePresets` / `HandleTimeout` / `CheckConditions` / `CalculateBonus` / `GetStatusText` 五处 switch 均有分支）
2. **但 `StartChallenge` / `CompleteChallenge` 全库 0 处外部调用** — 系统从未被启动。**修复前请先确认这是"未完成功能"还是"已废弃代码"**
3. **`:239-241/245-247` 超时语义自相矛盾**: SpeedRun 超时无条件算成功，但 `CalculateBonus` SpeedRun 分支(:307) 在超时时返回负数；Survival 撑满时限反而算失败。**两者都错了且方向相反**
4. **`:160` `StartChallenge()` 无条件 `Time.timeScale = 1f`**: 若在 `GameState.PAUSE` 时启动会静默解除暂停，两系统争抢 `timeScale` 所有权
5. **`:92` `ElapsedTime` 用 `Time.time`**: 暂停后与 `_timeRemaining` 永久失配，`RecordSplit()` 记录偏大
6. **`:217/:221` Warning/Critical 区间重叠**: 每帧重复触发，无"已触发"标志位
7. **`:96` `IsCompleted`**: `!_isRunning && _currentMode != None`，与 `CompleteChallenge(false)` / `AbortChallenge()` 无法区分完成/失败/中止三态

**前提**: 先与产品确认 TimeAttack 是否为废弃功能。若废弃则整体删除。

---

### P2-3: 代码清理清单

| 文件 | 操作 | 原因 |
|------|------|------|
| `Assets/Scripts/Background/ColorJudge_Pubu.cs` | **删除** | 死代码，全项目无引用 |
| `Assets/Scripts/Player/TwiceJump.cs` | **删除** | 功能被 PlayerControl 完全覆盖 |
| `EventManager.cs`（具体路径请搜索） | **删除** | 已标 `[Obsolete]`，GUID 在所有 `*.unity` 中 0 匹配 |
| `README.md` 版本描述 | **更新** | 写 Unity 2017，实际是 2022.3.2t13 |
| `Assets/Scripts/GameManager.cs:2` `using System;` | **删除/检查** | `SaveManager.cs:2` 也有 `using System;` 未使用 |

---

## 明确不要修改的部分

以下代码经多位队友独立审查确认正确，**请勿"修复"**：

| 代码 | 位置 | 原因 |
|------|------|------|
| `Mathf.RoundToInt(baseScore * currentTier.ScoreMultiplier)` | `ComboSystem.cs:155` | 倍率(1.0~3.0)与 baseScore(5~15)均为小整数量级，float 24 位尾数精确可表示 |
| `Update()` 超时用 `Time.time`（timeScale=0 时停止） | `ComboSystem.cs:114-120` | `Time.time` 在暂停时本身停止推进，不存在"暂停误判超时"问题（但建议改为显式状态判断以降低隐式依赖） |
| `reTimer()` 未重置 `hour` | `time.cs:36` | `hour` 恒为 0（赋值已被注释），无行为影响。应修的是死代码+无小时进位，不是重置遗漏 |

---

---

# 第二轮检查补充问题（共 20 项）

> 第二轮检查覆盖了第一轮未触及的剩余系统，由 Aion CLI 完成。

## 修复顺序建议

```
Phase A — P0 紧急
  ├─ P0-A1: Login.cs — HTTPS + 输入验证 + UnityWebRequest [Login.cs:17,55-56]
  ├─ P0-A2: FireMonster.cs — 无效四元数 → Quaternion.identity [FireMonster.cs:67]
  ├─ (原有) P0-1: GetLevelIndexByName() 改用精确匹配
  ├─ (原有) P0-2: SaveManager.UnlockLevel() 加 Mathf.Max 保护
  └─ (原有) P0-3: 建 _Managers Prefab

Phase B — P1 数据与状态
  ├─ P1-A1: 统一音量键名 "MusicVolume"/"SFXVolume" [AudioManager.cs:31-32,
  │         GameplayHUD.cs:153-154, GameStateUI.cs:130-131]
  ├─ P1-A2: 统一分数系统为 ScoreManager，废弃 GameManager.score
  ├─ P1-A3: isDead 静态字段 → GameManager 属性 [PlayerControl + CheckPoint + DoorOpen]
  ├─ P1-A4: PowerUpManager 改用 unscaledTime 或 deltaTime 累加
  ├─ (原有) P1-2: 移除 CalculateBonus() 的 TotalScore 自引用 + AddScore 钳制
  ├─ (原有) P1-3: 存档统一 + 版本号
  └─ (原有) P1-4: 写盘节流

Phase C — P1～P2 架构与网络
  ├─ P1-A5: GameObject.Find() 统一替换为 Inspector 引用或 GameManager 缓存
  │         [GroundMonster, FireMonster, BulletMove_1, CheckPoint,
  │          DoorOpen, AutoPlatformMove, ParticleManager, PlayerProjectileSpawner]
  ├─ P1-A6: Renderer.material → Renderer.sharedMaterial（读取时）
  ├─ P1-A7: ScoreManager WWW → UnityWebRequest + HTTPS
  ├─ P1-A8: 删除 Login.cs 中的 WWW → UnityWebRequest + HTTPS
  ├─ P1-A9: idGet.cs WWW → UnityWebRequest + HTTPS + null 检查
  ├─ (原有) P1-1: 计时器统一（time.cs → GameManager.GameTime）
  ├─ (原有) P1-5: 删除 TwiceJump.cs + 统一跳跃状态访问
  ├─ (原有) P1-6: 删除 ColorJudge_Pubu.cs + 修复事件触发
  └─ (原有) P1-7: QuestSystem 奖励发放修复 + 反订阅

Phase D — P2 物理与逻辑
  ├─ P2-A1: WaterControl Update → FixedUpdate + 缓存 Rigidbody2D [WaterControl.cs:13]
  ├─ P2-A2: BGHorizConstentSpeed — 统一物理与 Transform 操作
  ├─ P2-A3: Grass.cs — 移除对 GameManager.jumpFlag/jumptime 直写
  ├─ P2-A4: EnemyChaseMovement — 废弃 NavMeshAgent API → isStopped
  ├─ P2-A5: CheckPoint.Reborn() — 时序修复（WaitForSecondsRealtime）
  ├─ (原有) P2-1: ComboSystem 多项逻辑打磨
  ├─ (原有) P2-2: TimeAttack 确认废弃/完成状态
  └─ (原有) P2-3: 代码清理清单

Phase E — P3 代码质量
  ├─ P3-A1: EnemyHealth — 移除空 if 块 + 重复 Destroy
  ├─ P3-A2: PlayerProjectileSpawner — 移动端输入 + Camera.main null 检查
  ├─ P3-A3: ParticleManager — 实时获取 player 位置而非缓存
  ├─ P3-A4: EventManager — 封装 RemoveListener + TryGetValue
  ├─ P3-A5: PlayerData — 移除无效的 DontDestroyOnLoad
  ├─ P3-A6: 删除 EventManager.cs（[Obsolete]，0 引用）
  └─ P3-A7: 删除 ComboAction.EnemyDefeat 或补调用点
```

---

## 第二轮问题细节

### P0-A1: Login.cs 安全漏洞

**文件**: `Assets/Scripts/Login/Login.cs`
- `:17` — `http://127.0.0.1/wicg/login.php` 使用 HTTP，密码明文传输 → 改为 `https://`
- `:55-56` — `form.AddField("username", username)` 无输入清洗 → 添加长度校验(3-32字符) + 空白/特殊字符拒绝
- 使用已废弃 `WWW` 类 → 替换为 `UnityWebRequest`
- 添加注释：密码哈希应在服务端完成

### P0-A2: FireMonster.cs 无效四元数

**文件**: `Assets/Scripts/Monster/FireMonster.cs:67`
- `new Quaternion(0, 0, 0, 0)` 是无效四元数（w=0 模=0）→ 替换为 `Quaternion.identity`

### P1-A1: 音量键名不统一

**文件**: 
- `AudioManager.cs:31-32` — 键 `"MusicVolume"`（默认 0.75f）/ `"SFXVolume"`（默认 0.75f）
- `GameplayHUD.cs:153-154` — 键 `"BGMVolume"`（默认 0.5f）/ `"SFXVolume"`（默认 0.5f）
- `GameStateUI.cs:130-131` — 同上
- **后果**: HUD 滑条与 AudioManager 互不同步，默认值也不一致

**修复**: 
1. 统一所有文件使用 `"MusicVolume"` / `"SFXVolume"`
2. 统一默认值 `0.75f`
3. HUD/GameStateUI 从 `AudioManager.Instance` 读取当前音量初始化滑条

### P1-A2: 双分数系统

**文件**:
- `GameManager.cs:10` — `public int score`（int）
- `ScoreManager.cs:12` — `public float currentScore`
- 怪物击杀加 `GameManager.score` 但 HUD/排行榜用 `ScoreManager.currentScore` → 两套互不同步

**修复**: 所有分数变更走 `ScoreManager.Instance.AddScore()`，`GameManager.score` 标记 `[Obsolete]` 或移除

### P1-A3: PlayerControl.isDead 静态字段被外部直写

**文件**:
- `CheckPoint.cs:24,30,67` — 读写 `PlayerControl.isDead`
- `DoorOpen.cs:20` — 读 `PlayerControl.isDead`
- 静态字段跨场景不重置，外部直接紧耦合

**修复**: `GameManager` 添加 `IsPlayerDead` 属性，外部通过它访问，PlayerControl 内部同步

### P1-A4: PowerUpManager 暂停时计时异常

**文件**: `PowerUpManager.cs:53-61`
- 使用 `Time.time` 计时，`timeScale=0` 时停止 → 暂停后剩余时间异常
- 推荐改用 `Time.unscaledTime` 或 `Time.deltaTime` 累加，添加注释说明设计意图

### P1-A5: GameObject.Find() 硬编码

**涉及 8 个文件**（GroundMonster, FireMonster, BulletMove_1, CheckPoint, DoorOpen, AutoPlatformMove, ParticleManager, PlayerProjectileSpawner）
- O(n) 性能 + 对象改名即 NullReferenceException

**修复**: 
- "Player" → 通过 `GameManager.getInstance().player` 或 `[SerializeField]`
- "Bgcolor_1" → GameManager 添加背景渲染器字段
- "Main Camera" → `Camera.main` 或 Inspector 引用
- 全部添加 null 检查 + `Debug.LogWarning`

### P1-A6: Renderer.material 实例化

**文件**: GroundMonster/FireMonster/BulletMove_1
- 访问 `.material` 创建材质副本 → 内存泄漏
- 读取比较时用 `.sharedMaterial`，创建独立颜色时在 `Start()` 缓存

### P1-A7 ~ A9: WWW 废弃类

**文件**: `ScoreManager.cs` / `Login.cs` / `idGet.cs`
- `WWW` 在 2018.3+ 废弃 → 替换为 `UnityWebRequest`
- 添加超时 + 错误处理 + URL 改为 `https://`

### P2-A1: WaterControl Update → FixedUpdate

**文件**: `WaterControl.cs:13`
- `Rigidbody2D.velocity` 应在 `FixedUpdate` 设置
- 每帧 `GetComponent<Rigidbody2D>()` 应缓存

### P2-A2: BGHorizConstentSpeed 物理与 Transform 混合

**文件**: `BGHorizConstentSpeed.cs`
- `:20` FixedUpdate 覆盖 velocity | `:27` OnTriggerEnter2D 用 `transform.Translate` 与物理冲突
- 缓存 Rigidbody2D，位置重置用 `rb.position`

### P2-A3: Grass.cs 写入 jumpFlag/jumptime

**文件**: `Grass.cs:24-25`
- 加剧"谁控制跳跃"的混乱。PlayerControl 添加 `ExternalBounce(float)` 供外部调用

### P2-A4: EnemyChaseMovement 废弃 NavMeshAgent API

**文件**: `EnemyChaseMovement.cs:63,70`
- `agent.Resume()`/`agent.Stop()` → `agent.isStopped = false/true`
- `public Collider player` → `public Transform player`

### P2-A5: CheckPoint 协程时序

**文件**: `CheckPoint.cs:34,57`
- `Reborn()` 中 `Pause()` → 应改为 `Resume()`
- `WaitForSeconds` → `WaitForSecondsRealtime`（避免 timeScale 干扰）

### P3-A1 ~ A6: 代码质量

- **EnemyHealth**: 空 `if (scoreText)` 块 + 重复 `Destroy(gameObject, 1f)`
- **PlayerProjectileSpawner**: `Camera.main` 无 null 检查 + 移动端输入
- **ParticleManager**: 缓存的 `playerTransform` 在玩家重建后失效
- **EventManager**: 无 `RemoveListener` 封装 + `ContainsKey`+索引器双重查找
- **PlayerData**: 非 MonoBehaviour 调用 `DontDestroyOnLoad(this)` 无效代码
- **EventManager.cs**: 已 `[Obsolete]`，GUID 0 引用，可安全删除

---

## 完整修复顺序总表

```
Phase A — P0 紧急: P0-A1 → P0-A2 → P0-1 → P0-2 → P0-3
Phase B — P1 数据: P1-A1 → P1-A2 → P1-A3 → P1-A4 → P1-2 → P1-3 → P1-4
Phase C — P1 架构: P1-A5 → P1-A6 → P1-A7 → P1-A8 → P1-A9 → P1-1 → P1-5 → P1-6 → P1-7
Phase D — P2 物理: P2-A1 → P2-A2 → P2-A3 → P2-A4 → P2-A5 → P2-1 → P2-2 → P2-3
Phase E — P3 质量: P3-A1 → P3-A2 → P3-A3 → P3-A4 → P3-A5 → P3-A6 → P3-A7
```

---

## 验证建议

当前项目**无测试框架**（`Packages/manifest.json` 无 test-framework 引用，无 `Assets/Tests` 目录），以上结论全部来自静态代码审查 + 场景文件 GUID 反查，未经运行时验证。

**修复后手动回归重点**：
1. 第3关（volcanocave）分数写入 `Level3Score` 而非 `Level2Score`
2. 解锁第3关后重玩第1关，`UnlockedLevel` 不回退
3. 跨关卡计时器归零
4. 高频 combo 下 Android 无掉帧
5. 颜色匹配平台正常淡入淡出
6. 二连跳/冲刺/护盾在四个关卡正常运行
7. 登录流程使用 HTTPS + 输入校验
8. 音量滑条与 AudioManager 实时同步
9. 暂停恢复后 buff 剩余时间正确
10. FireMonster 子弹方向正常