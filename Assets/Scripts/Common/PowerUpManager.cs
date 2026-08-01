using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PowerUpType
{
    SpeedBoost,
    JumpBoost,
    Shield,
    Magnet,
    DoubleScore,
    ColorImmunity
}

[System.Serializable]
public class PowerUpData
{
    public PowerUpType Type;
    public float Duration = 5f;
    public float Multiplier = 1.5f;
    public Color EffectColor = Color.white;
    public string DisplayName;
    public string Description;
}

public class PowerUpManager : MonoBehaviour
{
    #region 单例

    private static PowerUpManager _instance;

    public static PowerUpManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PowerUpManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PowerUpManager");
                    _instance = go.AddComponent<PowerUpManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 配置

    [Header("道具配置")]
    public List<PowerUpData> PowerUpConfigs;

    [Header("默认配置")]
    public float DefaultDuration = 5f;
    public float DefaultMultiplier = 1.5f;

    #endregion

    #region 状态

    private Dictionary<PowerUpType, bool> _activePowerUps = new Dictionary<PowerUpType, bool>();
    private Dictionary<PowerUpType, float> _powerUpTimers = new Dictionary<PowerUpType, float>();
    private Dictionary<PowerUpType, Coroutine> _powerUpCoroutines = new Dictionary<PowerUpType, Coroutine>();

    public event System.Action<PowerUpType, float> OnPowerUpActivated;
    public event System.Action<PowerUpType> OnPowerUpExpired;
    public event System.Action<PowerUpType, float> OnPowerUpTick;

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
        InitializeDefaults();
    }

    void InitializeDefaults()
    {
        foreach (PowerUpType type in System.Enum.GetValues(typeof(PowerUpType)))
        {
            if (!_activePowerUps.ContainsKey(type))
            {
                _activePowerUps[type] = false;
                _powerUpTimers[type] = 0f;
            }
        }
    }

    #endregion

    #region 公共方法

    public void ActivatePowerUp(PowerUpType type)
    {
        PowerUpData config = GetConfig(type);
        ActivatePowerUp(type, config.Duration);
    }

    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        if (_powerUpCoroutines.ContainsKey(type) && _powerUpCoroutines[type] != null)
        {
            StopCoroutine(_powerUpCoroutines[type]);
        }

        _activePowerUps[type] = true;
        _powerUpTimers[type] = duration;
        _powerUpCoroutines[type] = StartCoroutine(PowerUpRoutine(type, duration));

        OnPowerUpActivated?.Invoke(type, duration);

        Debug.Log($"[PowerUp] 激活: {type}, 持续时间: {duration}秒");
    }

    public void DeactivatePowerUp(PowerUpType type)
    {
        _activePowerUps[type] = false;
        _powerUpTimers[type] = 0f;

        if (_powerUpCoroutines.ContainsKey(type) && _powerUpCoroutines[type] != null)
        {
            StopCoroutine(_powerUpCoroutines[type]);
            _powerUpCoroutines[type] = null;
        }

        OnPowerUpExpired?.Invoke(type);

        Debug.Log($"[PowerUp] 过期: {type}");
    }

    public void DeactivateAllPowerUps()
    {
        List<PowerUpType> activeTypes = new List<PowerUpType>();
        foreach (var kvp in _activePowerUps)
        {
            if (kvp.Value) activeTypes.Add(kvp.Key);
        }
        foreach (var type in activeTypes)
        {
            DeactivatePowerUp(type);
        }
    }

    public bool IsPowerUpActive(PowerUpType type)
    {
        return _activePowerUps.TryGetValue(type, out bool active) && active;
    }

    public float GetRemainingTime(PowerUpType type)
    {
        return _powerUpTimers.TryGetValue(type, out float time) ? time : 0f;
    }

    public float GetMultiplier(PowerUpType type)
    {
        PowerUpData config = GetConfig(type);
        return IsPowerUpActive(type) ? config.Multiplier : 1f;
    }

    public PowerUpData GetConfig(PowerUpType type)
    {
        if (PowerUpConfigs != null)
        {
            foreach (var config in PowerUpConfigs)
            {
                if (config.Type == type) return config;
            }
        }
        return new PowerUpData
        {
            Type = type,
            Duration = DefaultDuration,
            Multiplier = DefaultMultiplier,
            DisplayName = type.ToString(),
            Description = ""
        };
    }

    public int ActivePowerUpCount
    {
        get
        {
            int count = 0;
            foreach (var kvp in _activePowerUps)
            {
                if (kvp.Value) count++;
            }
            return count;
        }
    }

    #endregion

    #region 协程

    IEnumerator PowerUpRoutine(PowerUpType type, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _powerUpTimers[type] = duration - elapsed;
            OnPowerUpTick?.Invoke(type, _powerUpTimers[type]);
            yield return null;
        }
        DeactivatePowerUp(type);
    }

    #endregion
}
