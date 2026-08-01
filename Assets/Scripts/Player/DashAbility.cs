using UnityEngine;
using System.Collections;

public class DashAbility : MonoBehaviour
{
    #region 配置

    [Header("冲刺参数")]
    public float DashSpeed = 20f;
    public float DashDuration = 0.15f;
    public float DashCooldown = 0.8f;
    public int MaxDashCount = 1;
    public float DashRefreshTime = 1.5f;

    [Header("方向")]
    public bool SnapToCardinal = false;
    public float MinDashDistance = 0.5f;

    [Header("视觉效果")]
    public ParticleSystem DashStartParticles;
    public ParticleSystem DashTrailParticles;
    public AudioClip DashSound;
    public SpriteRenderer PlayerRenderer;
    public Color DashColor = new Color(1f, 1f, 1f, 0.5f);
    public Color AfterImageColor = new Color(1f, 1f, 1f, 0.3f);
    public float AfterImageLifetime = 0.3f;
    public int AfterImageCount = 3;

    #endregion

    #region 状态

    private bool _isDashing = false;
    private float _dashCooldownTimer = 0f;
    private int _dashCount = 0;
    private Vector2 _dashDirection;
    private Rigidbody2D _rb;
    private PlayerControl _playerControl;
    private Color _originalColor;
    private Coroutine _dashCoroutine;
    private Coroutine _cooldownCoroutine;

    #endregion

    #region 事件

    public event System.Action OnDashStart;
    public event System.Action OnDashEnd;
    public event System.Action<float> OnCooldownChanged;
    public event System.Action<int> OnDashCountChanged;

    #endregion

    #region 属性

    public bool IsDashing => _isDashing;
    public bool CanDash => !_isDashing && _dashCooldownTimer <= 0f && _dashCount < MaxDashCount && GameManager.Instance.CanMove;
    public float CooldownRemaining => Mathf.Max(0f, _dashCooldownTimer);
    public float CooldownRatio => DashCooldown > 0 ? CooldownRemaining / DashCooldown : 0f;
    public int CurrentDashCount => _dashCount;

    #endregion

    #region 初始化

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerControl = GetComponent<PlayerControl>();
        if (PlayerRenderer == null)
            PlayerRenderer = GetComponent<SpriteRenderer>();
        if (PlayerRenderer != null)
            _originalColor = PlayerRenderer.color;
    }

    void Start()
    {
        _dashCount = MaxDashCount;
    }

    void Update()
    {
        UpdateCooldown();
    }

    #endregion

    #region 冲刺核心

    public bool TryDash(Vector2 direction)
    {
        if (!CanDash) return false;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }
        direction.Normalize();

        if (SnapToCardinal)
        {
            direction = SnapTo8Direction(direction);
        }

        _dashDirection = direction;
        _isDashing = true;
        _dashCount--;

        OnDashCountChanged?.Invoke(_dashCount);

        if (_dashCoroutine != null) StopCoroutine(_dashCoroutine);
        _dashCoroutine = StartCoroutine(DashRoutine());

        return true;
    }

    public bool TryDashTowardTarget(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        return TryDash(direction);
    }

    IEnumerator DashRoutine()
    {
        OnDashStart?.Invoke();

        PlayDashEffects();
        SetDashVisuals(true);

        float elapsed = 0f;
        Vector2 startPos = _rb.position;
        Vector2 targetPos = startPos + _dashDirection * DashSpeed * DashDuration;

        _rb.velocity = Vector2.zero;
        _rb.gravityScale = 0f;

        while (elapsed < DashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DashDuration);

            if (float.IsNaN(t) || float.IsInfinity(t)) t = 1f;

            _rb.position = Vector2.Lerp(startPos, targetPos, EaseOutQuad(t));

            CreateAfterImage(elapsed);
            yield return null;
        }

        _rb.position = targetPos;
        EndDash();
    }

    void EndDash()
    {
        _isDashing = false;
        _rb.gravityScale = _playerControl != null ? 1.5f : 1f;
        _rb.velocity = new Vector2(_dashDirection.x * 3f, _rb.velocity.y);

        SetDashVisuals(false);

        OnDashEnd?.Invoke();

        ComboSystem.Instance?.RegisterAction(ComboAction.DashThrough, 15);

        if (_cooldownCoroutine != null) StopCoroutine(_cooldownCoroutine);
        _cooldownCoroutine = StartCoroutine(CooldownRoutine());

        _dashCoroutine = null;
    }

    IEnumerator CooldownRoutine()
    {
        _dashCooldownTimer = DashCooldown;
        while (_dashCooldownTimer > 0)
        {
            _dashCooldownTimer -= Time.deltaTime;
            OnCooldownChanged?.Invoke(CooldownRemaining);
            yield return null;
        }
        _dashCooldownTimer = 0f;

        if (_dashCount < MaxDashCount)
        {
            yield return new WaitForSeconds(DashRefreshTime - DashCooldown);
            _dashCount++;
            OnDashCountChanged?.Invoke(_dashCount);
        }
    }

    #endregion

    #region 视觉效果

    void PlayDashEffects()
    {
        if (DashStartParticles != null)
            DashStartParticles.Emit(15);

        if (DashTrailParticles != null)
            DashTrailParticles.Play();

        if (DashSound != null)
            AudioSource.PlayClipAtPoint(DashSound, transform.position);
    }

    void SetDashVisuals(bool dashing)
    {
        if (PlayerRenderer != null)
        {
            PlayerRenderer.color = dashing ? DashColor : _originalColor;
        }
    }

    void CreateAfterImage(float dashElapsed)
    {
        if (AfterImageCount <= 0) return;

        float interval = DashDuration / (AfterImageCount + 1);
        int imageIndex = Mathf.FloorToInt(dashElapsed / interval);
        if (imageIndex >= AfterImageCount || imageIndex < 0) return;

        GameObject afterImage = new GameObject("DashAfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;

        SpriteRenderer sr = afterImage.AddComponent<SpriteRenderer>();
        sr.sprite = PlayerRenderer != null ? PlayerRenderer.sprite : null;
        sr.color = AfterImageColor;
        sr.sortingOrder = PlayerRenderer != null ? PlayerRenderer.sortingOrder - 1 : 0;

        StartCoroutine(FadeOutAfterImage(afterImage));
    }

    IEnumerator FadeOutAfterImage(GameObject afterImage)
    {
        SpriteRenderer sr = afterImage.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < AfterImageLifetime && sr != null)
        {
            elapsed += Time.deltaTime;
            Color c = sr.color;
            c.a = Mathf.Lerp(AfterImageColor.a, 0f, elapsed / AfterImageLifetime);
            sr.color = c;
            yield return null;
        }
        if (afterImage != null) Destroy(afterImage);
    }

    #endregion

    #region 工具方法

    void UpdateCooldown()
    {
    }

    Vector2 SnapTo8Direction(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle = Mathf.Round(angle / 45f) * 45f;
        return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    }

    static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    public void ResetDash()
    {
        if (_dashCoroutine != null) StopCoroutine(_dashCoroutine);
        if (_cooldownCoroutine != null) StopCoroutine(_cooldownCoroutine);

        _isDashing = false;
        _dashCooldownTimer = 0f;
        _dashCount = MaxDashCount;
        SetDashVisuals(false);
        _rb.gravityScale = _playerControl != null ? 1.5f : 1f;
    }

    #endregion
}
