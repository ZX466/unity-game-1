using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

/// <summary>
/// 游戏管理器 - 负责全局游戏状态、关卡管理和事件分发
/// 数据存储已迁移到 SaveManager
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例模式

    private static GameManager instance;

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                if (instance == null)
                {
                    GameObject container = new GameObject("GameManager");
                    instance = container.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    public static GameManager getInstance() => Instance;

    #endregion

    #region 常量

    public const string SCENE_START = "start";
    public const string SCENE_LEVEL_SELECT = "LevelSelect";
    public const string SCENE_LOGIN = "login";
    public const string SCENE_REGISTER = "register";
    private const float DEATH_THRESHOLD_Y = -10f;

    #endregion

    #region 游戏状态管理

    public enum GameState
    {
        MENU = 0,
        PREPARING = 1,
        PLAYING = 2,
        PAUSE = 3,
        GAMEOVER = 4,
        VICTORY = 5
    }

    public const int MENU = 0;
    public const int PREPARING = 1;
    public const int PLAYING = 2;
    public const int PAUSE = 3;
    public const int GAMEOVER = 4;
    public const int VICTORY = 5;

    public GameState CurrentState { get; private set; }
    public string CurrentLevel { get; private set; }

    public int GAMESTATE
    {
        get => (int)CurrentState;
        set
        {
            var next = (GameState)Mathf.Clamp(value, MENU, VICTORY);
            SetStateInternal(next);
        }
    }

    #endregion

    #region 玩家属性

    [Header("玩家属性")]
    public GameObject player;
    public bool IsFirstTime = true;
    public bool CanMove = false;
    public bool JumpFlag = true;
    public int JumpTime = 0;

    public bool IfCanMove
    {
        get => CanMove;
        set => CanMove = value;
    }

    public bool jumpFlag
    {
        get => JumpFlag;
        set => JumpFlag = value;
    }

    public int jumptime
    {
        get => JumpTime;
        set => JumpTime = value;
    }

    public string id_Login = "";

    #endregion

    #region 游戏数据

    [Header("游戏数据")]
    public int TotalScore = 0;
    public int CoinsCollected = 0;
    public float GameTime = 0f;

    #endregion

    #region 事件系统

    public delegate void GameStateChangedEventHandler(GameState oldState, GameState newState);
    public event GameStateChangedEventHandler OnGameStateChanged;

    public delegate void PlayerDeathEventHandler();
    public event PlayerDeathEventHandler OnPlayerDeath;

    public delegate void LevelCompleteEventHandler();
    public event LevelCompleteEventHandler OnLevelComplete;

    public delegate void CoinCollectedEventHandler();
    public event CoinCollectedEventHandler OnCoinCollected;

    #endregion

    #region 初始化

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeGame();
    }

    void InitializeGame()
    {
        CurrentState = GameState.MENU;
        JumpFlag = true;
        JumpTime = 0;
        TotalScore = 0;
        CoinsCollected = 0;
        GameTime = 0f;
        LoadPlayerData();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentLevel = scene.name;
        FindPlayer();

        if (IsMenuLikeScene(CurrentLevel))
        {
            SetStateInternal(GameState.MENU);
            return;
        }

        SetStateInternal(GameState.PREPARING);
        // 新关卡开始时重置本次连击统计（原 ResetRunStats 全库 0 处调用，最高连击与日志永不重置）。
        ComboSystem.Instance?.ResetRunStats();
        StartCoroutine(GameStartCountdown());
    }

    bool IsMenuLikeScene(string sceneName)
    {
        return sceneName == SCENE_START || sceneName == SCENE_LEVEL_SELECT;
    }

    void FindPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    public IEnumerator GameStartCountdown()
    {
        // 使用实时等待，避免 Time.timeScale 为 0 时倒计时卡住。
        yield return new WaitForSecondsRealtime(2f);
        SetStateInternal(GameState.PLAYING);
        CanMove = true;
    }

    #endregion

    #region 状态管理

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState oldState = CurrentState;
        CurrentState = newState;

        ApplyStateRuntimeFlags(newState, true);

        OnGameStateChanged?.Invoke(oldState, newState);
    }

    void SetStateInternal(GameState newState)
    {
        if (CurrentState == newState) return;
        var old = CurrentState;
        CurrentState = newState;
        ApplyStateRuntimeFlags(newState, false);
        OnGameStateChanged?.Invoke(old, newState);
    }

    void ApplyStateRuntimeFlags(GameState state, bool emitTerminalEvents)
    {
        switch (state)
        {
            case GameState.MENU:
            case GameState.PREPARING:
                Time.timeScale = 1f;
                CanMove = false;
                break;
            case GameState.PLAYING:
                Time.timeScale = 1f;
                break;
            case GameState.PAUSE:
                Time.timeScale = 0f;
                SaveManager.Instance.Flush();
                break;
            case GameState.GAMEOVER:
                Time.timeScale = 0f;
                if (emitTerminalEvents) OnPlayerDeath?.Invoke();
                break;
            case GameState.VICTORY:
                Time.timeScale = 0f;
                if (emitTerminalEvents) OnLevelComplete?.Invoke();
                break;
        }
    }

    public void RestartLevel()
    {
        SetStateInternal(GameState.PREPARING);
        ResetPlayerState();
        SafeLoadScene(CurrentLevel);
    }

    public void LoadLevel(string levelName)
    {
        SetStateInternal(GameState.PREPARING);
        ResetPlayerState();
        SafeLoadScene(levelName);
    }

    public void ReturnToMenu()
    {
        SetStateInternal(GameState.MENU);
        ResetPlayerState();
        SafeLoadScene("start");
    }

    void SafeLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[GameManager] Cannot load empty scene name!");
            return;
        }

        // 场景切换前 flush 当前场景的积分/金币缓存（SaveTotalScore/SaveCoins 不再自动 flush）。
        SaveManager.Instance.Flush();

        Debug.Log("[GameManager] Loading scene: " + sceneName);
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

    private void ResetPlayerState()
    {
        CanMove = false;
        JumpFlag = true;
        JumpTime = 0;
        GameTime = 0f;
    }

    #endregion

    #region 游戏逻辑

    void Update()
    {
        if (CurrentState == GameState.PLAYING)
        {
            GameTime += Time.deltaTime;
            CheckGameOverConditions();
        }
    }

    void CheckGameOverConditions()
    {
        if (player == null || player.transform.position.y < DEATH_THRESHOLD_Y)
        {
            ChangeState(GameState.GAMEOVER);
        }
    }

    public void PlayerDie()
    {
        if (CurrentState == GameState.PLAYING)
        {
            ChangeState(GameState.GAMEOVER);
        }
    }

    public void LevelComplete()
    {
        if (CurrentState == GameState.PLAYING)
        {
            ChangeState(GameState.VICTORY);
            SaveBestScoreForCurrentLevel();
            SavePlayerData();
        }
    }

    public void CollectCoin(int value = 10)
    {
        CoinsCollected++;
        TotalScore += value;
        SavePlayerData();
        OnCoinCollected?.Invoke();
    }

    public void AddScore(int value)
    {
        // 钳制到 int.MaxValue：连击/挑战奖励的正反馈累加会静默溢出成负数
        //（C# 默认 unchecked），使 SaveLevelScore 的 score > prev 判断失效。
        TotalScore = (int)Mathf.Min((long)TotalScore + value, int.MaxValue);
        SavePlayerData();
    }

    /// <summary>任务奖励发放：同时加总分与金币并落盘。不触发 OnCoinCollected，避免与 QuestSystem 形成递归。</summary>
    public void GrantReward(int score, int coins)
    {
        TotalScore = (int)Mathf.Min((long)TotalScore + score, int.MaxValue);
        CoinsCollected += coins;
        SavePlayerData();
    }

    #endregion

    #region 数据存储

    void LoadPlayerData()
    {
        TotalScore = SaveManager.Instance.LoadTotalScore();
        CoinsCollected = SaveManager.Instance.LoadCoins();
        int unlockedLevel = SaveManager.Instance.GetUnlockedLevel();
        Debug.Log("已解锁关卡: " + unlockedLevel);
    }

    void SavePlayerData()
    {
        SaveManager.Instance.SaveTotalScore(TotalScore);
        SaveManager.Instance.SaveCoins(CoinsCollected);
    }

    // 暂停/退出时统一 flush，配合 SaveManager 的高频写只入缓存策略（P1-4 写盘节流）。
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveManager.Instance.Flush();
    }

    void OnApplicationQuit()
    {
        SaveManager.Instance.Flush();
    }

    public void UnlockNextLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        int currentLevelIndex = GetLevelIndexByName(currentSceneName);

        if (currentLevelIndex > -1)
        {
            int nextLevelIndex = currentLevelIndex + 1;
            int nextUnlockedLevelNumber = Mathf.Clamp(nextLevelIndex + 1, 1, 3);
            SaveManager.Instance.UnlockLevel(nextUnlockedLevelNumber);
            Debug.Log("已解锁下一关: " + nextUnlockedLevelNumber);
        }
    }

    public int GetLevelIndexByName(string sceneName)
    {
        // 精确匹配场景名。旧的 Contains 链有顺序 bug："volcanocave".Contains("cave")
        // 为 true，导致第3关被识别为 index 1，最高分写错键、污染第2关存档。
        if (string.IsNullOrEmpty(sceneName)) return -1;
        switch (sceneName)
        {
            case "waterfall": return 0;
            case "cave": return 1;
            case "volcanocave": return 2;
            default: return -1;
        }
    }

    int GetLevelNumberBySceneName(string sceneName)
    {
        int idx = GetLevelIndexByName(sceneName);
        return idx < 0 ? -1 : idx + 1;
    }

    void SaveBestScoreForCurrentLevel()
    {
        int levelNumber = GetLevelNumberBySceneName(SceneManager.GetActiveScene().name);
        if (levelNumber < 1 || levelNumber > 3) return;

        // 统一走 SaveManager，避免键名散落（原代码绕过 SaveManager 直拼 $"Level{n}Score"）。
        SaveManager.Instance.SaveLevelScore(levelNumber, TotalScore);
    }

    #endregion

    #region 工具方法

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(GameTime / 60);
        int seconds = Mathf.FloorToInt(GameTime % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public float GetGameTime()
    {
        return GameTime;
    }

    #endregion
}
