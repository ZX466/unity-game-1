using UnityEngine;
using UnityEngine.UI;

public class PowerUpStatusUI : MonoBehaviour
{
    #region 引用

    public Text NameText;
    public Image FillImage;
    public Image IconImage;
    public CanvasGroup CanvasGroup;

    #endregion

    private float _duration = 0f;
    private float _startTime = 0f;

    public void Initialize(PowerUpType type, float duration)
    {
        _duration = duration;
        _startTime = Time.time;

        var config = PowerUpManager.Instance?.GetConfig(type);
        if (config != null)
        {
            if (NameText != null) NameText.text = config.DisplayName;
            if (IconImage != null) IconImage.color = config.EffectColor;
            if (FillImage != null) FillImage.color = config.EffectColor;
        }

        gameObject.name = $"PowerUp_{type}";
    }

    public void Refresh(float remaining)
    {
        if (_duration <= 0) return;

        float ratio = Mathf.Clamp01(remaining / _duration);
        if (FillImage != null) FillImage.fillAmount = ratio;

        if (CanvasGroup != null && remaining < 1.5f)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            CanvasGroup.alpha = pulse;
        }
        else if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 1f;
        }
    }
}
