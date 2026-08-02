using UnityEngine;
using System.Collections;

/// <summary>
/// 高级颜色匹配组件 - 优化版
/// 负责平台/地刺的颜色匹配和显示逻辑，支持玩家站在平台上时的安全处理
/// </summary>
public class ColorJudge : MonoBehaviour
{
    #region 公开属性

    [Header("引用")]
    public GameObject Block;
    public GameObject BG;

    [Header("颜色配置")]
    public Color BlockColor;

    [Header("参数")]
    public float FadeDuration = 0.2f;
    public bool IsPlatform = true;
    public float PlayerDetectionRadius = 0.5f;

    [Header("玩法反馈")]
    [Range(0f, 1f)]
    public float HiddenAlpha = 0.15f;
    public bool DisableColliderWhenHidden = true;

    #endregion

    #region 事件

    // 当平台显隐影响玩家站立状态时触发，由 PlayerControl 监听处理
    public static event System.Action<bool> OnPlatformGroundedChanged;

    #endregion

    #region 内部属性

    private Renderer _blockRenderer;
    private Renderer _bgRenderer;
    private Collider2D _blockCollider;
    private float _currentAlpha = 1f;
    private bool _isFading = false;
    private Coroutine _fadeRoutine;

    #endregion

    #region 初始化

    void Awake()
    {
        InitializeComponents();
        SetBlockColor();
        // Ensure we start visible.
        SetVisibleImmediate(true);
    }

    void Start()
    {
        // 确保颜色设置正确
        SetBlockColor();
    }

    void InitializeComponents()
    {
        if (Block != null)
        {
            _blockRenderer = Block.GetComponent<Renderer>();
            _blockCollider = Block.GetComponent<Collider2D>();
        }

        if (BG != null)
        {
            _bgRenderer = BG.GetComponent<Renderer>();
        }
    }

    void SetBlockColor()
    {
        if (_blockRenderer != null)
        {
            Color color = _blockRenderer.material.color;
            color.r = BlockColor.r;
            color.g = BlockColor.g;
            color.b = BlockColor.b;
            _blockRenderer.material.color = color;
        }

        if (_bgRenderer != null)
        {
            Color color = _bgRenderer.material.color;
            color.r = BlockColor.r;
            color.g = BlockColor.g;
            color.b = BlockColor.b;
            _bgRenderer.material.color = color;
        }
    }

    #endregion

    #region 触发检测

    // 颜色相同时一旦触碰，使平台消失
    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "BackColor")
        {
            if (IsColorMatch(collider))
            {
                if (IsPlatform)
                {
                    CheckPlayerOnPlatform();
                }
                else
                {
                    HideBlock();
                }
            }
        }
    }

    // 颜色不同时，平台出现
    void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.tag == "BackColor")
        {
            if (!IsColorMatch(collider))
            {
                ShowBlock();
            }
        }
    }

    bool IsColorMatch(Collider2D collider)
    {
        Renderer otherRenderer = collider.GetComponent<Renderer>();
        Renderer blockRenderer = Block.GetComponent<Renderer>();
        if (otherRenderer == null || blockRenderer == null) return false;

        // Compare RGB only; alpha can be animated for visibility feedback.
        Color a = otherRenderer.material.color;
        Color b = blockRenderer.material.color;
        return Mathf.Approximately(a.r, b.r) && Mathf.Approximately(a.g, b.g) && Mathf.Approximately(a.b, b.b);
    }

    void CheckPlayerOnPlatform()
    {
        Collider2D[] overlappingColliders = Physics2D.OverlapCircleAll(transform.position, PlayerDetectionRadius);

        foreach (Collider2D col in overlappingColliders)
        {
            if (col.CompareTag("Player"))
            {
                // 玩家站在平台上，先解除父级关系，等待一小段时间再隐藏
                GameObject player = col.gameObject;
                player.transform.parent = null;
                StartCoroutine(HideWithDelay(0.1f));
                return;
            }
        }

        // 没有玩家在平台上，直接隐藏
        HideBlock();
    }

    IEnumerator HideWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideBlock();
    }

    #endregion

    #region 显示/隐藏控制

    void HideBlock()
    {
        if (_isFading) return;
        FadeTo(false);
    }

    void ShowBlock()
    {
        FadeTo(true);
    }

    void FadeTo(bool visible)
    {
        if (Block == null || _blockRenderer == null) return;

        if (!isActiveAndEnabled)
        {
            ApplyAlphaImmediate(visible);
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        if (!gameObject.activeInHierarchy)
        {
            ApplyAlphaImmediate(visible);
            return;
        }
        _fadeRoutine = StartCoroutine(FadeRoutine(visible));
    }

    void ApplyAlphaImmediate(bool visible)
    {
        if (_blockRenderer == null) return;
        Color c = _blockRenderer.material.color;
        c.a = visible ? 1f : HiddenAlpha;
        _blockRenderer.material.color = c;
        if (visible && Block != null && !Block.activeSelf) Block.SetActive(true);
    }

    IEnumerator FadeRoutine(bool visible)
    {
        _isFading = true;

        // Ensure object is active while fading in/out (so renderer updates).
        if (!Block.activeSelf) Block.SetActive(true);

        float startAlpha = _blockRenderer.material.color.a;
        float endAlpha = visible ? 1f : HiddenAlpha;

        float t = 0f;
        float duration = Mathf.Max(0.01f, FadeDuration);
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            ApplyAlpha(a);
            yield return null;
        }

        ApplyAlpha(endAlpha);

        if (!visible && DisableColliderWhenHidden && _blockCollider != null)
        {
            _blockCollider.enabled = false;
        }
        else if (visible && _blockCollider != null)
        {
            _blockCollider.enabled = true;
        }

        // We intentionally keep Block active even when hidden (alpha+collider) to avoid
        // side effects with scripts/child objects relying on active state.
        OnPlatformGroundedChanged?.Invoke(visible);

        _isFading = false;
        _fadeRoutine = null;
    }

    void ApplyAlpha(float alpha)
    {
        _currentAlpha = alpha;

        if (_blockRenderer != null)
        {
            var c = _blockRenderer.material.color;
            c.a = alpha;
            _blockRenderer.material.color = c;
        }
    }

    void SetVisibleImmediate(bool visible)
    {
        if (Block == null) return;
        if (!Block.activeSelf) Block.SetActive(true);
        ApplyAlpha(visible ? 1f : HiddenAlpha);
        if (_blockCollider != null)
        {
            _blockCollider.enabled = visible || !DisableColliderWhenHidden;
        }
    }

    #endregion

    #region 调试绘制

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, PlayerDetectionRadius);
    }

    #endregion
}
