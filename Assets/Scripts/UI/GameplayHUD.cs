using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameplayHUD : MonoBehaviour
{
    #region UI引用 - 连击

    [Header("连击显示")]
    public GameObject ComboPanel;
    public Text ComboText;
    public Text ComboTierText;
    public Image ComboFillBar;
    public Animator ComboAnimator;

    #endregion

    #region UI引用 - 冲刺

    [Header("冲刺显示")]
    public GameObject DashPanel;
    public Image DashCooldownImage;
    public Text DashCountText;
    public Image DashIcon;

    #endregion

    #region UI引用 - 护盾

    [Header("护盾显示")]
    public GameObject ShieldPanel;
    public Image[] ShieldChargeIcons;
    public Image ShieldActiveBar;
    public Text ShieldCountText;

    #endregion

    #region UI引用 - 道具

    [Header("道具状态显示")]
    public GameObject PowerUpContainer;
    public PowerUpStatusUI PowerUpStatusPrefab;

    #endregion

    #region UI引用 - 时间挑战

    [Header("时间挑战显示")]
    public GameObject TimeAttackPanel;
    public Text TimeAttackTimer;
    public Image TimeAttackFillBar;
    public Text TimeAttackModeName;
    public Text TimeAttackObjective;

    #endregion

    #region 配置

    [Header("动画配置")]
    public float ComboPopupDuration = 0.5f;
    public float ComboFadeSpeed = 3f;
    public Color DashReadyColor = Color.green;
    public Color DashCooldownColor = Color.gray;
    public float PulseSpeed = 2f;

    #endregion

    #region 内部

    private CanvasGroup _comboCanvasGroup;
    private float _comboDisplayAlpha = 0f;
    private bool _comboVisible = false;
    private float _lastComboTime = 0f;

    private System.Collections.Generic.Dictionary<PowerUpType, PowerUpStatusUI> _activePowerUpUIs =
        new System.Collections.Generic.Dictionary<PowerUpType, PowerUpStatusUI>();

    #endregion

    #region 生命周期

    void Awake()
    {
        if (ComboPanel != null)
        {
            _comboCanvasGroup = ComboPanel.GetComponent<CanvasGroup>();
            if (_comboCanvasGroup == null) _comboCanvasGroup = ComboPanel.AddComponent<CanvasGroup>();
            _comboCanvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        SetupEventListeners();
        HideAllPanels();
    }

    void OnEnable()
    {
        SetupEventListeners();
    }

    void OnDisable()
    {
        RemoveEventListeners();
    }

    #endregion

    #region 事件绑定

    void SetupEventListeners()
    {
        if (ComboSystem.Instance != null)
        {
            // ?. 纵深防御：即使 UnityEvent 未初始化也不会抛 NRE。
            ComboSystem.Instance.OnComboChanged?.AddListener(OnComboChanged);
            ComboSystem.Instance.OnComboTierChanged?.AddListener(OnComboTierChanged);
            ComboSystem.Instance.OnMaxComboBroken?.AddListener(OnComboBroken);
        }

        var player = FindPlayerObject();
        if (player != null)
        {
            var dash = player.GetComponent<DashAbility>();
            if (dash != null)
            {
                dash.OnDashCountChanged += OnDashCountChanged;
                dash.OnCooldownChanged += OnCooldownChanged;
            }

            var shield = player.GetComponent<ColorShield>();
            if (shield != null)
            {
                shield.OnChargeChanged += OnShieldChargeChanged;
                shield.OnShieldActivated += OnShieldActivated;
                shield.OnShieldDeactivated += OnShieldDeactivated;
            }
        }

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpActivated += OnPowerUpActivated;
            PowerUpManager.Instance.OnPowerUpExpired += OnPowerUpExpired;
        }

        if (TimeAttackManager.Instance != null)
        {
            TimeAttackManager.Instance.OnTimeAttackStart += OnTimeAttackStart;
            TimeAttackManager.Instance.OnTimeUpdated += OnTimeAttackUpdated;
            TimeAttackManager.Instance.OnTimeAttackComplete += OnTimeAttackEnded;
            TimeAttackManager.Instance.OnTimeAttackFailed += OnTimeAttackEnded;
        }
    }

    void RemoveEventListeners()
    {
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.OnComboChanged.RemoveListener(OnComboChanged);
            ComboSystem.Instance.OnComboTierChanged.RemoveListener(OnComboTierChanged);
            ComboSystem.Instance.OnMaxComboBroken.RemoveListener(OnComboBroken);
        }

        var player = FindPlayerObject();
        if (player != null)
        {
            var dash = player.GetComponent<DashAbility>();
            if (dash != null)
            {
                dash.OnDashCountChanged -= OnDashCountChanged;
                dash.OnCooldownChanged -= OnCooldownChanged;
            }

            var shield = player.GetComponent<ColorShield>();
            if (shield != null)
            {
                shield.OnChargeChanged -= OnShieldChargeChanged;
                shield.OnShieldActivated -= OnShieldActivated;
                shield.OnShieldDeactivated -= OnShieldDeactivated;
            }
        }

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpActivated -= OnPowerUpActivated;
            PowerUpManager.Instance.OnPowerUpExpired -= OnPowerUpExpired;
        }

        if (TimeAttackManager.Instance != null)
        {
            TimeAttackManager.Instance.OnTimeAttackStart -= OnTimeAttackStart;
            TimeAttackManager.Instance.OnTimeUpdated -= OnTimeAttackUpdated;
            TimeAttackManager.Instance.OnTimeAttackComplete -= OnTimeAttackEnded;
            TimeAttackManager.Instance.OnTimeAttackFailed -= OnTimeAttackEnded;
        }
    }

    #endregion

    #region 更新循环

    void Update()
    {
        UpdateComboVisibility();
        UpdateDashVisual();
        UpdatePowerUpVisuals();
    }

    #endregion

    #region 连击UI

    void OnComboChanged(int combo)
    {
        if (combo <= 0)
        {
            HideCombo();
            return;
        }

        ShowCombo();

        if (ComboText != null)
            ComboText.text = combo.ToString() + "x";

        _lastComboTime = Time.time;
        _comboDisplayAlpha = 1f;
    }

    void OnComboTierChanged(string tierName, float multiplier, Color color)
    {
        if (ComboTierText != null)
        {
            ComboTierText.text = tierName;
            ComboTierText.color = color;
        }

        if (ComboFillBar != null)
            ComboFillBar.fillAmount = Mathf.Min(1f, multiplier / 3f);

        if (ComboAnimator != null)
            ComboAnimator.SetTrigger("TierUp");
    }

    void OnComboBroken(int maxCombo)
    {
        StartCoroutine(FadeOutCombo());
    }

    void ShowCombo()
    {
        if (ComboPanel != null) ComboPanel.SetActive(true);
        _comboVisible = true;
        _comboDisplayAlpha = 1f;
    }

    void HideCombo()
    {
        _comboVisible = false;
        if (_comboCanvasGroup != null) _comboCanvasGroup.alpha = 0f;
    }

    IEnumerator FadeOutCombo()
    {
        float elapsed = 0f;
        while (elapsed < ComboPopupDuration && _comboCanvasGroup != null)
        {
            elapsed += Time.deltaTime;
            _comboCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / ComboPopupDuration);
            yield return null;
        }
        HideCombo();
    }

    void UpdateComboVisibility()
    {
        if (!_comboVisible || ComboSystem.Instance == null) return;

        if (Time.time - _lastComboTime > 1f)
        {
            _comboDisplayAlpha -= Time.deltaTime * ComboFadeSpeed;
            _comboDisplayAlpha = Mathf.Max(0.15f, _comboDisplayAlpha);

            if (_comboCanvasGroup != null)
                _comboCanvasGroup.alpha = _comboDisplayAlpha;
        }
    }

    #endregion

    #region 冲刺UI

    void OnDashCountChanged(int count)
    {
        if (DashPanel != null) DashPanel.SetActive(true);
        if (DashCountText != null) DashCountText.text = count.ToString();
        UpdateDashVisual();
    }

    void OnCooldownChanged(float remaining)
    {
        UpdateDashVisual();
    }

    void UpdateDashVisual()
    {
        var player = FindPlayerObject();
        if (player == null) return;
        var dash = player.GetComponent<DashAbility>();
        if (dash == null) return;

        if (DashCooldownImage != null)
        {
            float ratio = dash.CooldownRatio;
            DashCooldownImage.fillAmount = 1f - ratio;
            DashCooldownImage.color = ratio <= 0.01f ? DashReadyColor : DashCooldownColor;
        }

        if (DashIcon != null)
        {
            float pulse = dash.CanDash ? 1f : 0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed);
            var c = DashIcon.color;
            c.a = pulse;
            DashIcon.color = c;
        }
    }

    #endregion

    #region 护盾UI

    void OnShieldChargeChanged(int current, int max)
    {
        if (ShieldPanel != null) ShieldPanel.SetActive(true);
        UpdateShieldVisual(current, max);
    }

    void OnShieldActivated()
    {
        if (ShieldActiveBar != null)
        {
            ShieldActiveBar.gameObject.SetActive(true);
            ShieldActiveBar.fillAmount = 1f;
        }
    }

    void OnShieldDeactivated()
    {
        if (ShieldActiveBar != null)
            ShieldActiveBar.gameObject.SetActive(false);
    }

    void UpdateShieldVisual(int current, int max)
    {
        if (ShieldCountText != null)
            ShieldCountText.text = $"{current}/{max}";

        for (int i = 0; i < ShieldChargeIcons.Length; i++)
        {
            if (ShieldChargeIcons[i] != null)
                ShieldChargeIcons[i].gameObject.SetActive(i < current);
        }
    }

    #endregion

    #region 道具UI

    void OnPowerUpActivated(PowerUpType type, float duration)
    {
        ShowPowerUpStatus(type, duration);
    }

    void OnPowerUpExpired(PowerUpType type)
    {
        RemovePowerUpStatus(type);
    }

    void ShowPowerUpStatus(PowerUpType type, float duration)
    {
        if (PowerUpContainer == null || PowerUpStatusPrefab == null) return;

        if (_activePowerUpUIs.ContainsKey(type))
        {
            var existing = _activePowerUpUIs[type];
            if (existing != null) existing.Refresh(duration);
            return;
        }

        var go = Instantiate(PowerUpStatusPrefab, PowerUpContainer.transform);
        go.Initialize(type, duration);
        _activePowerUpUIs[type] = go;
    }

    void RemovePowerUpStatus(PowerUpType type)
    {
        if (_activePowerUpUIs.ContainsKey(type))
        {
            var ui = _activePowerUpUIs[type];
            if (ui != null) Destroy(ui.gameObject);
            _activePowerUpUIs.Remove(type);
        }
    }

    void UpdatePowerUpVisuals()
    {
        foreach (var kvp in _activePowerUpUIs)
        {
            if (kvp.Value == null) continue;
            float remaining = PowerUpManager.Instance?.GetRemainingTime(kvp.Key) ?? 0f;
            kvp.Value.Refresh(remaining);
        }
    }

    #endregion

    #region 时间挑战UI

    void OnTimeAttackStart(TimeAttackMode mode)
    {
        if (TimeAttackPanel != null) TimeAttackPanel.SetActive(true);
        var config = TimeAttackManager.Instance?.CurrentConfig;
        if (config != null)
        {
            if (TimeAttackModeName != null) TimeAttackModeName.text = config.DisplayName;
            if (TimeAttackObjective != null)
            {
                switch (mode)
                {
                    case TimeAttackMode.SpeedRun:
                        TimeAttackObjective.text = "尽快到达终点!";
                        break;
                    case TimeAttackMode.TargetScore:
                        TimeAttackObjective.text = $"目标: {config.TargetValue}分";
                        break;
                    case TimeAttackMode.Survival:
                        TimeAttackObjective.text = "存活到时间结束!";
                        break;
                    case TimeAttackMode.Collection:
                        TimeAttackObjective.text = $"收集 {config.TargetValue} 个金币";
                        break;
                }
            }
        }
    }

    void OnTimeAttackUpdated(float remaining)
    {
        if (TimeAttackTimer != null)
            TimeAttackTimer.text = FormatTime(remaining);

        if (TimeAttackFillBar != null && TimeAttackManager.Instance != null)
        {
            TimeAttackFillBar.fillAmount = TimeAttackManager.Instance.TimeRatio;

            if (TimeAttackManager.Instance.IsCritical)
                TimeAttackFillBar.color = Color.red;
            else if (TimeAttackManager.Instance.IsWarning)
                TimeAttackFillBar.color = Color.yellow;
            else
                TimeAttackFillBar.color = TimeAttackManager.Instance?.CurrentConfig?.TimerColor ?? Color.white;
        }
    }

    void OnTimeAttackEnded()
    {
        if (TimeAttackPanel != null)
            StartCoroutine(DelayedHide(TimeAttackPanel, 2f));
    }

    #endregion

    #region 工具方法

    GameObject FindPlayerObject()
    {
        return GameManager.Instance?.player ?? GameObject.FindGameObjectWithTag("Player");
    }

    string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }

    void HideAllPanels()
    {
        if (ComboPanel != null) ComboPanel.SetActive(false);
        if (DashPanel != null) DashPanel.SetActive(false);
        if (ShieldPanel != null) ShieldPanel.SetActive(false);
        if (TimeAttackPanel != null) TimeAttackPanel.SetActive(false);
    }

    IEnumerator DelayedHide(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.SetActive(false);
    }

    #endregion
}
