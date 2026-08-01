using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    #region 配置

    [Header("道具类型")]
    public PowerUpType PowerUpType = PowerUpType.SpeedBoost;

    [Header("效果")]
    public float DurationOverride = 0f;
    public int ScoreBonus = 50;
    public ParticleSystem CollectEffect;
    public AudioClip CollectSound;

    [Header("动画")]
    public float FloatSpeed = 1f;
    public float FloatAmplitude = 0.3f;
    public float RotateSpeed = 90f;

    [Header("发光")]
    public SpriteRenderer GlowSprite;
    public Color GlowColor = Color.yellow;
    public float GlowPulseSpeed = 2f;

    #endregion

    #region 内部

    private Vector3 _startPosition;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;

    #endregion

    #region 生命周期

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _startPosition = transform.position;
    }

    void Start()
    {
        if (_spriteRenderer != null)
        {
            PowerUpData config = PowerUpManager.Instance.GetConfig(PowerUpType);
            _spriteRenderer.color = config.EffectColor;
        }
    }

    void Update()
    {
        Animate();
    }

    #endregion

    #region 动画

    void Animate()
    {
        transform.position = _startPosition + Vector3.up * Mathf.Sin(Time.time * FloatSpeed) * FloatAmplitude;
        transform.Rotate(0, 0, RotateSpeed * Time.deltaTime);

        if (GlowSprite != null)
        {
            float pulse = (Mathf.Sin(Time.time * GlowPulseSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(Color.white, GlowColor, pulse * 0.6f);
            GlowSprite.color = c;
        }
    }

    #endregion

    #region 触发检测

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
        }
    }

    void Collect(GameObject player)
    {
        float duration = DurationOverride > 0 ? DurationOverride : PowerUpManager.Instance.GetConfig(PowerUpType).Duration;
        PowerUpManager.Instance.ActivatePowerUp(PowerUpType, duration);

        GameManager.Instance.AddScore(ScoreBonus);

        if (CollectEffect != null)
        {
            Instantiate(CollectEffect, transform.position, Quaternion.identity);
        }

        if (CollectSound != null)
        {
            AudioSource.PlayClipAtPoint(CollectSound, transform.position);
        }

        ComboSystem.Instance?.RegisterAction(ComboAction.CollectPowerUp);

        gameObject.SetActive(false);
    }

    #endregion

    #region 调试

    void OnDrawGizmosSelected()
    {
        Gizmos.color = PowerUpManager.Instance.GetConfig(PowerUpType).EffectColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    #endregion
}
