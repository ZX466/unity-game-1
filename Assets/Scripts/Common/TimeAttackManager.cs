using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public enum TimeAttackMode
{
    None,
    SpeedRun,
    TargetScore,
    Survival,
    Collection
}

[System.Serializable]
public class TimeAttackConfig
{
    public TimeAttackMode Mode;
    public float TimeLimit = 60f;
    public int TargetValue = 1000;
    public string DisplayName;
    public string Description;
    public Color TimerColor;
    public int BonusMultiplier = 2;
}

public class TimeAttackManager : MonoBehaviour
{
    #region 单例

    private static TimeAttackManager _instance;

    public static TimeAttackManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimeAttackManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TimeAttackManager");
                    _instance = go.AddComponent<TimeAttackManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 配置

    [Header("时间挑战配置")]
    public List<TimeAttackConfig> PresetConfigs;

    [Header("通用")]
    public bool EnableGlobalTimer = false;
    public float WarningThreshold = 10f;
    public float CriticalThreshold = 5f;

    #endregion

    #region 状态

    private TimeAttackMode _currentMode = TimeAttackMode.None;
    private TimeAttackConfig _currentConfig;
    private float _timeRemaining;
    private bool _isRunning = false;
    private bool _isPaused = false;
    private float _startTime;
    private List<float> _splitTimes = new List<float>();

    #endregion

    #region 事件

    public event System.Action<TimeAttackMode> OnTimeAttackStart;
    public event System.Action OnTimeAttackComplete;
    public event System.Action OnTimeAttackFailed;
    public event System.Action<float> OnTimeUpdated;
    public event System.Action<string> OnWarningTriggered;
    public event System.Action<int> OnTargetAchieved;

    #endregion

    #region 属性

    public TimeAttackMode CurrentMode => _currentMode;
    public bool IsRunning => _isRunning && !_isPaused;
    public float TimeRemaining => _timeRemaining;
    public float ElapsedTime => Time.time - _startTime;
    public float TimeRatio => _currentConfig != null && _currentConfig.TimeLimit > 0 ? _timeRemaining / _currentConfig.TimeLimit : 1f;
    public bool IsWarning => _timeRemaining <= WarningThreshold && _timeRemaining > CriticalThreshold;
    public bool IsCritical => _timeRemaining <= CriticalThreshold;
    public bool IsCompleted => _isRunning == false && _currentMode != TimeAttackMode.None;
    public int SplitCount => _splitTimes.Count;
    public TimeAttackConfig CurrentConfig => _currentConfig;

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
        InitializePresets();
    }

    void InitializePresets()
    {
        if (PresetConfigs == null || PresetConfigs.Count == 0)
        {
            PresetConfigs = new List<TimeAttackConfig>
            {
                new TimeAttackConfig { Mode = TimeAttackMode.SpeedRun, TimeLimit = 120f, TargetValue = 0, DisplayName = "竞速模式", Description = "以最快速度通关", TimerColor = Color.green, BonusMultiplier = 3 },
                new TimeAttackConfig { Mode = TimeAttackMode.TargetScore, TimeLimit = 90f, TargetValue = 2000, DisplayName = "目标得分", Description = "在限时内达到目标分数", TimerColor = Color.yellow, BonusMultiplier = 2 },
                new TimeAttackConfig { Mode = TimeAttackMode.Survival, TimeLimit = 60f, TargetValue = 0, DisplayName = "生存挑战", Description = "在限定时间内存活", TimerColor = Color.red, BonusMultiplier = 4 },
                new TimeAttackConfig { Mode = TimeAttackMode.Collection, TimeLimit = 45f, TargetValue = 30, DisplayName = "收集挑战", Description = "收集足够数量的金币", TimerColor = Color.cyan, BonusMultiplier = 2 }
            };
        }
    }

    void Update()
    {
        if (!_isRunning || _isPaused) return;

        UpdateTimer();
        CheckConditions();
    }

    #endregion

    #region 模式控制

    public void StartChallenge(TimeAttackMode mode)
    {
        TimeAttackConfig config = GetConfig(mode);
        StartChallenge(config);
    }

    public void StartChallenge(TimeAttackConfig config)
    {
        if (config == null) return;

        _currentMode = config.Mode;
        _currentConfig = config;
        _timeRemaining = config.TimeLimit;
        _isRunning = true;
        _isPaused = false;
        _startTime = Time.time;
        _splitTimes.Clear();

        Time.timeScale = 1f;

        OnTimeAttackStart?.Invoke(_currentMode);

        Debug.Log($"[TimeAttack] 开始挑战: {_currentMode}, 时限: {_timeRemaining}秒");
    }

    public void PauseChallenge()
    {
        if (!_isRunning) return;
        _isPaused = true;
    }

    public void ResumeChallenge()
    {
        if (!_isRunning) return;
        _isPaused = false;
    }

    public void CompleteChallenge(bool success = true)
    {
        if (!_isRunning) return;

        _isRunning = false;
        RecordSplit();

        if (success)
        {
            int bonusScore = CalculateBonus();
            GameManager.Instance.AddScore(bonusScore);
            OnTargetAchieved?.Invoke(bonusScore);
            OnTimeAttackComplete?.Invoke();
            Debug.Log($"[TimeAttack] 挑战完成! 奖励分数: +{bonusScore}");
        }
        else
        {
            OnTimeAttackFailed?.Invoke();
            Debug.Log("[TimeAttack] 挑战失败!");
        }
    }

    public void AbortChallenge()
    {
        _isRunning = false;
        _currentMode = TimeAttackMode.None;
        _timeRemaining = 0f;
        _splitTimes.Clear();
    }

    #endregion

    #region 计时器

    void UpdateTimer()
    {
        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= WarningThreshold && _timeRemaining > CriticalThreshold - 0.5f && !IsCritical)
        {
            OnWarningTriggered?.Invoke("warning");
        }
        if (_timeRemaining <= CriticalThreshold + 0.5f && IsCritical)
        {
            OnWarningTriggered?.Invoke("critical");
        }

        OnTimeUpdated?.Invoke(_timeRemaining);

        if (_timeRemaining <= 0)
        {
            _timeRemaining = 0f;
            HandleTimeout();
        }
    }

    void HandleTimeout()
    {
        switch (_currentMode)
        {
            case TimeAttackMode.SpeedRun:
                // 超时 = 未在时限内到达终点 = 失败（原代码误判为成功）。
                CompleteChallenge(false);
                break;
            case TimeAttackMode.TargetScore:
                CompleteChallenge(GameManager.Instance.TotalScore >= _currentConfig.TargetValue);
                break;
            case TimeAttackMode.Survival:
                // 撑满时限 = 存活成功（原代码误判为失败，与 SpeedRun 方向相反）。
                CompleteChallenge(true);
                break;
            case TimeAttackMode.Collection:
                CompleteChallenge(GameManager.Instance.CoinsCollected >= _currentConfig.TargetValue);
                break;
            default:
                CompleteChallenge(false);
                break;
        }
    }

    #endregion

    #region 条件检查

    void CheckConditions()
    {
        switch (_currentMode)
        {
            case TimeAttackMode.TargetScore:
                if (GameManager.Instance.TotalScore >= _currentConfig.TargetValue)
                    CompleteChallenge(true);
                break;
            case TimeAttackMode.Collection:
                if (GameManager.Instance.CoinsCollected >= _currentConfig.TargetValue)
                    CompleteChallenge(true);
                break;
        }
    }

    #endregion

    #region 计分点

    public void RecordSplit()
    {
        _splitTimes.Add(ElapsedTime);
    }

    public float GetSplitTime(int index)
    {
        if (index < 0 || index >= _splitTimes.Count) return -1f;
        return _splitTimes[index];
    }

    public List<float> GetAllSplits() => new List<float>(_splitTimes);

    #endregion

    #region 分数计算

    public int CalculateBonus()
    {
        if (_currentConfig == null) return 0;

        float timeRatio = _timeRemaining / _currentConfig.TimeLimit;
        int baseBonus = Mathf.RoundToInt(timeRatio * 500 * _currentConfig.BonusMultiplier);

        switch (_currentMode)
        {
            case TimeAttackMode.SpeedRun:
                // Mathf.Max(0, ...) 防止超时(ElapsedTime>100s)时奖励变负。
                return baseBonus + Mathf.Max(0, Mathf.RoundToInt((100f - ElapsedTime) * 10));
            case TimeAttackMode.TargetScore:
                // 旧代码 + TotalScore/2 基于当前总分，构成正反馈（每次挑战总分×1.5，
                // 约50次即溢出 int.MaxValue 静默回绕成负数）。改为基于目标值的固定比例，不再自引用。
                return baseBonus + _currentConfig.TargetValue / 2;
            case TimeAttackMode.Survival:
                return Mathf.RoundToInt(ElapsedTime * _currentConfig.BonusMultiplier * 20);
            case TimeAttackMode.Collection:
                return baseBonus + GameManager.Instance.CoinsCollected * 25;
            default:
                return baseBonus;
        }
    }

    #endregion

    #region 工具方法

    public TimeAttackConfig GetConfig(TimeAttackMode mode)
    {
        foreach (var config in PresetConfigs)
        {
            if (config.Mode == mode) return config;
        }
        return null;
    }

    public string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int ms = Mathf.FloorToInt((seconds % 1f) * 100f);
        return $"{mins:00}:{secs:00}.{ms:00}";
    }

    public string GetStatusText()
    {
        if (!_isRunning && _currentMode == TimeAttackMode.None) return "";

        switch (_currentMode)
        {
            case TimeAttackMode.SpeedRun:
                return $"用时: {FormatTime(ElapsedTime)}";
            case TimeAttackMode.TargetScore:
                return $"{GameManager.Instance.TotalScore}/{_currentConfig.TargetValue}";
            case TimeAttackMode.Survival:
                return $"存活: {FormatTime(ElapsedTime)}";
            case TimeAttackMode.Collection:
                return $"{GameManager.Instance.CoinsCollected}/{_currentConfig.TargetValue}";
            default:
                return FormatTime(_timeRemaining);
        }
    }

    #endregion
}
