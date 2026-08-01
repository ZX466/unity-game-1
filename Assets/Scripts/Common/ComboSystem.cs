using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public enum ComboAction
{
    CollectCoin,
    CollectPowerUp,
    PerfectLanding,
    ColorMatch,
    DashThrough,
    ChainJump
}

[System.Serializable]
public class ComboTier
{
    public int MinCombo;
    public string DisplayName;
    public float ScoreMultiplier;
    public Color DisplayColor;
}

public class ComboSystem : MonoBehaviour
{
    #region 单例

    private static ComboSystem _instance;

    public static ComboSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ComboSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ComboSystem");
                    _instance = go.AddComponent<ComboSystem>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 配置

    [Header("连击配置")]
    public float ComboTimeout = 2f;
    public int MaxComboDisplay = 999;

    [Header("连击等级")]
    public List<ComboTier> ComboTiers = new List<ComboTier>
    {
        new ComboTier { MinCombo = 0, DisplayName = "", ScoreMultiplier = 1f, DisplayColor = Color.white },
        new ComboTier { MinCombo = 5, DisplayName = "NICE!", ScoreMultiplier = 1.2f, DisplayColor = Color.green },
        new ComboTier { MinCombo = 10, DisplayName = "GREAT!", ScoreMultiplier = 1.5f, DisplayColor = Color.cyan },
        new ComboTier { MinCombo = 20, DisplayName = "AWESOME!", ScoreMultiplier = 2f, DisplayColor = Color.magenta },
        new ComboTier { MinCombo = 35, DisplayName = "INCREDIBLE!", ScoreMultiplier = 2.5f, DisplayColor = Color.red },
        new ComboTier { MinCombo = 50, DisplayName = "LEGENDARY!", ScoreMultiplier = 3f, DisplayColor = Color.yellow }
    };

    #endregion

    #region 状态

    private int _currentCombo = 0;
    private float _lastActionTime = 0f;
    private int _maxComboThisRun = 0;
    private int _totalActionsThisRun = 0;
    private Queue<ComboAction> _recentActions = new Queue<ComboAction>();
    private const int MAX_RECENT_ACTIONS = 5;

    [Header("事件")]
    public UnityEvent<int> OnComboChanged;
    public UnityEvent<string, float, Color> OnComboTierChanged;
    public UnityEvent<int> OnMaxComboBroken;
    public UnityEvent<float> OnScoreMultiplierChanged;

    #endregion

    #region 属性

    public int CurrentCombo => _currentCombo;
    public int MaxComboThisRun => _maxComboThisRun;
    public float CurrentMultiplier => GetCurrentTier().ScoreMultiplier;
    public string CurrentTierName => GetCurrentTier().DisplayName;
    public Color CurrentTierColor => GetCurrentTier().DisplayColor;
    public bool IsComboActive => _currentCombo > 0 && Time.time - _lastActionTime < ComboTimeout;

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

        // UnityEvent 字段仅在 Unity 反序列化时实例化；Manager 未挂入任何场景时
        // 走 lazy-create 路径（new GameObject().AddComponent），此路径下这些字段
        // 保持 null，GameplayHUD.AddListener 会抛 NullReferenceException。这里兜底初始化。
        if (OnComboChanged == null) OnComboChanged = new UnityEvent<int>();
        if (OnComboTierChanged == null) OnComboTierChanged = new UnityEvent<string, float, Color>();
        if (OnMaxComboBroken == null) OnMaxComboBroken = new UnityEvent<int>();
        if (OnScoreMultiplierChanged == null) OnScoreMultiplierChanged = new UnityEvent<float>();
    }

    void Start()
    {
        ResetCombo();
    }

    void Update()
    {
        if (_currentCombo > 0 && Time.time - _lastActionTime >= ComboTimeout)
        {
            BreakCombo();
        }
    }

    #endregion

    #region 公共方法

    public void RegisterAction(ComboAction action, int baseScore = 0)
    {
        _recentActions.Enqueue(action);
        if (_recentActions.Count > MAX_RECENT_ACTIONS) _recentActions.Dequeue();

        _currentCombo++;
        _totalActionsThisRun++;
        _lastActionTime = Time.time;

        if (_currentCombo > _maxComboThisRun)
        {
            _maxComboThisRun = _currentCombo;
        }

        ComboTier prevTier = GetTierForCount(_currentCombo - 1);
        ComboTier currentTier = GetCurrentTier();

        OnComboChanged?.Invoke(_currentCombo);

        if (prevTier.MinCombo != currentTier.MinCombo)
        {
            OnComboTierChanged?.Invoke(currentTier.DisplayName, currentTier.ScoreMultiplier, currentTier.DisplayColor);
        }

        OnScoreMultiplierChanged?.Invoke(currentTier.ScoreMultiplier);

        if (baseScore > 0)
        {
            int finalScore = Mathf.RoundToInt(baseScore * currentTier.ScoreMultiplier);
            GameManager.Instance.AddScore(finalScore);
        }
    }

    public void BreakCombo()
    {
        // 无连击时无需中断：PlayerControl 无条件调用，避免空跑加分与误导日志。
        if (_currentCombo <= 0) return;

        if (_currentCombo >= 20)
        {
            OnMaxComboBroken?.Invoke(_currentCombo);
        }

        int bonusScore = CalculateComboBonus();
        if (bonusScore > 0)
        {
            GameManager.Instance.AddScore(bonusScore);
        }

        ResetCombo();

        Debug.Log($"[Combo] 连击中断! 本次最高: {_maxComboThisRun}, 连击奖励: +{bonusScore}");
    }

    public void ResetCombo()
    {
        _currentCombo = 0;
        // Time.time 单调增长，0f 在游戏运行一段时间后语义脆弱；用 NegativeInfinity 保证下次动作必定不被判为超时。
        _lastActionTime = float.NegativeInfinity;
        _recentActions.Clear();

        // 主动用基础档清空 HUD 文案：否则归零后 OnComboTierChanged 不触发，残留上一次的 "LEGENDARY!"。
        ComboTier baseTier = GetTierForCount(0);
        OnComboChanged?.Invoke(0);
        OnComboTierChanged?.Invoke(baseTier.DisplayName, baseTier.ScoreMultiplier, baseTier.DisplayColor);
        OnScoreMultiplierChanged?.Invoke(1f);
    }

    public void ResetRunStats()
    {
        ResetCombo();
        _maxComboThisRun = 0;
        _totalActionsThisRun = 0;
    }

    public ComboTier GetCurrentTier()
    {
        return GetTierForCount(_currentCombo);
    }

    public ComboTier GetTierForCount(int count)
    {
        if (ComboTiers == null || ComboTiers.Count == 0)
        {
            return new ComboTier { MinCombo = 0, DisplayName = "", ScoreMultiplier = 1f, DisplayColor = Color.white };
        }
        ComboTier best = ComboTiers[0];
        foreach (var tier in ComboTiers)
        {
            if (count >= tier.MinCombo && tier.MinCombo >= best.MinCombo)
            {
                best = tier;
            }
        }
        return best;
    }

    public Queue<ComboAction> GetRecentActions()
    {
        return _recentActions;
    }

    public bool HasActionInRecent(ComboAction action)
    {
        return _recentActions.Contains(action);
    }

    public int CalculateComboBonus()
    {
        if (_currentCombo < 5) return 0;
        return Mathf.RoundToInt(_currentCombo * _currentCombo * 0.5f * GetCurrentTier().ScoreMultiplier);
    }

    public string GetComboText()
    {
        if (_currentCombo <= 0) return "";
        int displayCombo = Mathf.Min(_currentCombo, MaxComboDisplay);
        string tierText = GetCurrentTier().DisplayName;
        if (!string.IsNullOrEmpty(tierText))
        {
            return $"{displayCombo}x {tierText}";
        }
        return $"{displayCombo}x COMBO";
    }

    #endregion
}
