using UnityEngine;
using System.Collections;

public class ColorShield : MonoBehaviour
{
    #region 配置

    [Header("护盾参数")]
    public float ShieldDuration = 3f;
    public int MaxShieldCharges = 3;
    public float ChargeRecoveryTime = 5f;
    public float MatchColorBonusDuration = 1.5f;

    [Header("视觉效果")]
    public GameObject ShieldVisual;
    public SpriteRenderer ShieldSprite;
    public ParticleSystem ShieldActivateParticles;
    public ParticleSystem ShieldHitParticles;
    public AudioClip ShieldActivateSound;
    public AudioClip ShieldBreakSound;

    [Header("颜色")]
    public Color FullShieldColor = new Color(0.2f, 0.6f, 1f, 0.6f);
    public Color LowShieldColor = new Color(1f, 0.4f, 0.2f, 0.4f);
    public Color InvincibleColor = new Color(1f, 0.85f, 0.2f, 0.7f);

    #endregion

    #region 状态

    private int _currentCharges;
    private bool _isShieldActive = false;
    private float _shieldTimer = 0f;
    private float _chargeRecoveryTimer = 0f;
    private Coroutine _shieldCoroutine;
    private Coroutine _recoveryCoroutine;
    private PlayerControl _playerControl;
    private ColorJudge _colorJudge;

    #endregion

    #region 事件

    public event System.Action<int, int> OnChargeChanged;
    public event System.Action OnShieldActivated;
    public event System.Action OnShieldDeactivated;
    public event System.Action OnShieldHit;
    public event System.Action OnShieldBroken;

    #endregion

    #region 属性

    public bool IsShieldActive => _isShieldActive;
    public int CurrentCharges => _currentCharges;
    public int MaxCharges => MaxShieldCharges;
    public float ChargeRatio => (float)_currentCharges / MaxShieldCharges;
    public float RemainingTime => _isShieldActive ? _shieldTimer : 0f;

    #endregion

    #region 初始化

    void Awake()
    {
        _playerControl = GetComponent<PlayerControl>();
        if (ShieldVisual != null) ShieldVisual.SetActive(false);
        _currentCharges = MaxShieldCharges;
    }

    void Start()
    {
        FindColorJudge();
        UpdateShieldVisual();
    }

    void FindColorJudge()
    {
        ColorJudge[] judges = FindObjectsOfType<ColorJudge>(true);
        if (judges.Length > 0) _colorJudge = judges[0];
    }

    #endregion

    #region 护盾控制

    public void ActivateShield()
    {
        if (_currentCharges <= 0 || _isShieldActive) return;

        _isShieldActive = true;
        _currentCharges--;
        _shieldTimer = ShieldDuration;

        ShowShieldVisual(true);
        PlayActivateEffects();

        OnChargeChanged?.Invoke(_currentCharges, MaxShieldCharges);
        OnShieldActivated?.Invoke();

        ComboSystem.Instance?.RegisterAction(ComboAction.ColorMatch, 10);

        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
        _shieldCoroutine = StartCoroutine(ShieldRoutine());
    }

    public void ActivateShieldFromColorMatch(float bonusDuration = 0f)
    {
        float totalDuration = ShieldDuration + bonusDuration + MatchColorBonusDuration;
        if (_isShieldActive)
        {
            _shieldTimer += bonusDuration + MatchColorBonusDuration;
            FlashShield(InvincibleColor);
        }
        else
        {
            _shieldTimer = totalDuration;
            _isShieldActive = true;
            _currentCharges = Mathf.Min(_currentCharges + 1, MaxShieldCharges);

            ShowShieldVisual(true);
            PlayActivateEffects();
            SetShieldColor(InvincibleColor);

            OnChargeChanged?.Invoke(_currentCharges, MaxShieldCharges);
            OnShieldActivated?.Invoke();

            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            _shieldCoroutine = StartCoroutine(ShieldRoutine());
        }
    }

    public void DeactivateShield(bool broken = false)
    {
        _isShieldActive = false;
        _shieldTimer = 0f;

        ShowShieldVisual(false);

        if (broken && ShieldBreakSound != null)
            AudioSource.PlayClipAtPoint(ShieldBreakSound, transform.position);

        if (broken)
            OnShieldBroken?.Invoke();
        else
            OnShieldDeactivated?.Invoke();

        StartChargeRecovery();
    }

    public bool ConsumeShield()
    {
        if (!_isShieldActive) return false;

        OnShieldHit?.Invoke();

        if (ShieldHitParticles != null)
            ShieldHitParticles.Emit(10);

        DeactivateShield(false);
        return true;
    }

    public bool HasShield => _currentCharges > 0 || _isShieldActive;

    #endregion

    #region 协程

    IEnumerator ShieldRoutine()
    {
        while (_shieldTimer > 0)
        {
            _shieldTimer -= Time.deltaTime;
            UpdateShieldColorByTime();
            yield return null;
        }
        DeactivateShield(false);
        _shieldCoroutine = null;
    }

    IEnumerator ChargeRecoveryRoutine()
    {
        while (_currentCharges < MaxShieldCharges)
        {
            _chargeRecoveryTimer += Time.deltaTime;
            if (_chargeRecoveryTimer >= ChargeRecoveryTime)
            {
                _chargeRecoveryTimer = 0f;
                _currentCharges++;
                OnChargeChanged?.Invoke(_currentCharges, MaxShieldCharges);
                UpdateShieldVisual();
            }
            yield return null;
        }
        _recoveryCoroutine = null;
    }

    void StartChargeRecovery()
    {
        if (_recoveryCoroutine == null)
        {
            _recoveryCoroutine = StartCoroutine(ChargeRecoveryRoutine());
        }
    }

    #endregion

    #region 视觉效果

    void ShowShieldVisual(bool show)
    {
        if (ShieldVisual != null) ShieldVisual.SetActive(show);
        if (show) UpdateShieldVisual();
    }

    void UpdateShieldVisual()
    {
        if (ShieldSprite == null) return;

        if (!_isShieldActive)
        {
            float ratio = (float)_currentCharges / MaxShieldCharges;
            ShieldSprite.color = Color.Lerp(LowShieldColor, FullShieldColor, ratio);
        }
    }

    void UpdateShieldColorByTime()
    {
        if (ShieldSprite == null || !_isShieldActive) return;

        float ratio = _shieldTimer / ShieldDuration;
        float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.15f;
        Color baseColor = _shieldTimer < 1f ? LowShieldColor : FullShieldColor;
        Color c = baseColor;
        c.a = Mathf.Lerp(0.2f, 0.7f, ratio) + pulse;
        ShieldSprite.color = c;
    }

    void SetShieldColor(Color color)
    {
        if (ShieldSprite != null) ShieldSprite.color = color;
    }

    void FlashShield(Color flashColor)
    {
        if (ShieldSprite == null) return;
        StartCoroutine(FlashRoutine(flashColor));
    }

    IEnumerator FlashRoutine(Color flashColor)
    {
        Color original = ShieldSprite.color;
        SetShieldColor(flashColor);
        yield return new WaitForSeconds(0.15f);
        SetShieldColor(original);
    }

    void PlayActivateEffects()
    {
        if (ShieldActivateParticles != null)
            ShieldActivateParticles.Emit(20);

        if (ShieldActivateSound != null)
            AudioSource.PlayClipAtPoint(ShieldActivateSound, transform.position);
    }

    #endregion

    #region 工具方法

    public void ResetShield()
    {
        if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
        if (_recoveryCoroutine != null) StopCoroutine(_recoveryCoroutine);

        _isShieldActive = false;
        _currentCharges = MaxShieldCharges;
        _shieldTimer = 0f;
        _chargeRecoveryTimer = 0f;

        ShowShieldVisual(false);
        OnChargeChanged?.Invoke(_currentCharges, MaxShieldCharges);
    }

    public void Recharge(int amount = 1)
    {
        _currentCharges = Mathf.Min(_currentCharges + amount, MaxShieldCharges);
        OnChargeChanged?.Invoke(_currentCharges, MaxShieldCharges);
        UpdateShieldVisual();
    }

    #endregion
}
