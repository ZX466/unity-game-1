using UnityEngine;

public class SaveManager : MonoBehaviour
{
    #region 单例

    private static SaveManager _instance;

    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 常量

    private const string KEY_TOTAL_SCORE = "TotalScore";
    private const string KEY_COINS = "CoinsCollected";
    private const string KEY_UNLOCKED_LEVEL = "UnlockedLevel";
    private const string KEY_LEVEL_PREFIX = "Level";
    private const string KEY_LEVEL_SUFFIX = "Score";
    private const string KEY_SAVE_VERSION = "SaveVersion";
    private const int CURRENT_SAVE_VERSION = 1;

    #endregion

    #region 公共方法

    public void SaveTotalScore(int score)
    {
        // 高频写入（收金币/加分）：只写缓存，不立即 flush。
        // 由调用方在场景切换/暂停/退出时统一 Flush()，避免每帧同步写盘（Android 端掉帧）。
        PlayerPrefs.SetInt(KEY_TOTAL_SCORE, score);
    }

    public int LoadTotalScore()
    {
        return PlayerPrefs.GetInt(KEY_TOTAL_SCORE, 0);
    }

    public void SaveCoins(int coins)
    {
        PlayerPrefs.SetInt(KEY_COINS, coins);
    }

    public int LoadCoins()
    {
        return PlayerPrefs.GetInt(KEY_COINS, 0);
    }

    public void UnlockLevel(int levelNumber)
    {
        // 仅向前推进解锁进度：重玩低关卡通关时 nextLevelIndex 较小，
        // 不能把已解锁的高关卡重新锁上（旧 SetInt 无条件覆盖会回退）。
        int newValue = Mathf.Max(GetUnlockedLevel(), levelNumber);
        PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, newValue);
        Flush();
    }

    public int GetUnlockedLevel()
    {
        // Mathf.Max(1, ...) 兜底历史上 0-based 索引的旧存档
        //（原散落在 LevelSelectUI 的兼容补丁集中到这里，由 MigrateSaveData 统一处理）。
        return Mathf.Max(1, PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1));
    }

    public void SaveLevelScore(int levelNumber, int score)
    {
        string key = GetLevelScoreKey(levelNumber);
        int prev = PlayerPrefs.GetInt(key, 0);
        if (score > prev)
        {
            PlayerPrefs.SetInt(key, score);
            Flush();
        }
    }

    public int GetLevelScore(int levelNumber)
    {
        return PlayerPrefs.GetInt(GetLevelScoreKey(levelNumber), 0);
    }

    /// <summary>统一 flush 入口：把 PlayerPrefs 缓存写入磁盘。在场景切换/暂停/退出时调用。</summary>
    public void Flush()
    {
        PlayerPrefs.Save();
    }

    /// <summary>仅清空关卡解锁与关卡分数（保留全局总分/金币）。</summary>
    public void ResetLevelProgress()
    {
        PlayerPrefs.DeleteKey(KEY_UNLOCKED_LEVEL);
        for (int i = 1; i <= 3; i++)
        {
            PlayerPrefs.DeleteKey(GetLevelScoreKey(i));
        }
        Flush();
    }

    private static string GetLevelScoreKey(int levelNumber)
    {
        return $"{KEY_LEVEL_PREFIX}{levelNumber}{KEY_LEVEL_SUFFIX}";
    }

    #endregion

    #region 初始化

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        MigrateSaveData();
    }

    /// <summary>集中处理存档版本迁移，替掉 LevelSelectUI 里散落的兼容补丁。</summary>
    void MigrateSaveData()
    {
        int version = PlayerPrefs.GetInt(KEY_SAVE_VERSION, 0);
        if (version < CURRENT_SAVE_VERSION)
        {
            // v0 -> v1: 旧存档可能存了 0-based 的 UnlockedLevel，钳制到 >=1。
            int unlocked = PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1);
            if (unlocked < 1)
            {
                PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, 1);
            }
            PlayerPrefs.SetInt(KEY_SAVE_VERSION, CURRENT_SAVE_VERSION);
            Flush();
        }
    }

    #endregion
}
