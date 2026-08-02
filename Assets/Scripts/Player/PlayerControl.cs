using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 高级玩家控制器 - 物理优化版
/// 支持精确实时输入、物理模拟优化、动画状态管理
/// 适配新的GameManager架构
/// </summary>
public class PlayerControl : MonoBehaviour
{
    #region 状态定义

    [Header("状态")]
    public bool IsGrounded;
    public bool IsJumping;
    public bool IsDoubleJumping;
    public bool IsFalling;

    #endregion

    #region 引用

    [Header("引用")]
    public GameObject DeadPic; // 死亡图片预制体
    public CheckPoint ActiveCheckpoint; // 当前激活的重生点
    public GameObject VictoryUI; // 通关胜利UI

    #endregion

    #region 动画

    [Header("动画")]
    private Animator _animator;

    #endregion

    #region 音效

    [Header("音效")]
    public AudioClip JumpClip;
    public AudioClip DeadClip;
    public AudioClip LandClip;
    public AudioClip CoinClip;

    #endregion

    #region 移动参数

    [Header("移动参数")]
    public float JumpSpeed = 14f;
    public float MoveSpeed = 5f;
    public float GravityScale = 1.5f;
    public float AirControl = 0.5f;
    public float GroundCheckDistance = 0.1f;

    [Header("视觉反馈")]
    public float CoyoteTime = 0.08f;
    public float JumpBufferTime = 0.08f;

    #endregion

    #region 粒子特效

    [Header("粒子特效")]
    public ParticleSystem JumpParticles_Floor;
    public ParticleSystem JumpParticles_DoubleJump;
    public ParticleSystem DeathParticles;
    public ParticleSystem PerfectLandParticles;

    #endregion

    #region 新功能组件

    [Header("冲刺能力")]
    public DashAbility DashAbility;

    [Header("颜色护盾")]
    public ColorShield ShieldAbility;

    #endregion

    #region 内部属性

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private LayerMask _groundLayer;
    private float _moveInput;
    private float _lastYPosition;

    private float _lastGroundedTime;
    private float _lastJumpPressedTime;

    #endregion

    #region 初始化

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();
        _groundLayer = LayerMask.GetMask("Ground");
        GameManager.Instance.JumpTime = 0;

        if (DashAbility == null) DashAbility = GetComponent<DashAbility>();
        if (ShieldAbility == null) ShieldAbility = GetComponent<ColorShield>();

        FindCheckpoint();
    }

    void FindCheckpoint()
    {
        if (ActiveCheckpoint == null)
        {
            CheckPoint[] checkpoints = FindObjectsOfType<CheckPoint>(true);
            if (checkpoints.Length > 0)
            {
                ActiveCheckpoint = checkpoints[0];
            }
        }
    }

    void Start()
    {
        _rb.gravityScale = GravityScale;
        _lastYPosition = transform.position.y;
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        ColorJudge.OnPlatformGroundedChanged += OnPlatformGroundedChanged;
    }

    void OnDestroy()
    {
        ColorJudge.OnPlatformGroundedChanged -= OnPlatformGroundedChanged;
    }

    void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        if (newState == GameManager.GameState.PLAYING)
        {
            _rb.isKinematic = false;
        }
        else
        {
            _rb.isKinematic = true;
        }
    }

    void OnPlatformGroundedChanged(bool isGrounded)
    {
        IsGrounded = isGrounded;
    }

    #endregion

    #region 更新循环

    void Update()
    {
        HandleInput();
        UpdatePlayerState();
        HandleMovement();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        CheckGround();
    }

    #endregion

    #region 输入处理

    void HandleInput()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING && GameManager.Instance.CanMove)
        {
            if (DashAbility != null && DashAbility.IsDashing) return;

            _moveInput = Input.GetAxis("Horizontal");
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _lastJumpPressedTime = Time.time;
            }
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.C))
            {
                float dashDir = _moveInput != 0 ? _moveInput : (transform.localScale.x > 0 ? 1 : -1);
                DashAbility?.TryDash(new Vector2(dashDir, 0));
            }
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.V))
            {
                ShieldAbility?.ActivateShield();
            }
        }
        else
        {
            _moveInput = 0f;
        }
    }

    void TryConsumeBufferedJump()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.PLAYING || !GameManager.Instance.CanMove)
        {
            return;
        }

        // Jump buffer: if pressed recently, allow jump as soon as grounded.
        bool hasBufferedJump = Time.time - _lastJumpPressedTime <= JumpBufferTime;
        if (!hasBufferedJump) return;

        bool canUseCoyote = Time.time - _lastGroundedTime <= CoyoteTime;

        if (IsGrounded || canUseCoyote)
        {
            _lastJumpPressedTime = -999f;
            DoJump();
        }
        else if (!IsDoubleJumping && GameManager.Instance.JumpTime < 2)
        {
            _lastJumpPressedTime = -999f;
            DoDoubleJump();
        }
    }

    void AttemptJump()
    {
        if (IsGrounded)
        {
            DoJump();
        }
        else if (!IsDoubleJumping && GameManager.Instance.JumpTime < 2)
        {
            DoDoubleJump();
        }
    }

    void DoJump()
    {
        IsJumping = true;
        IsDoubleJumping = false;
        GameManager.Instance.JumpTime = 1;
        GameManager.Instance.JumpFlag = true;

        float jumpMult = PowerUpManager.Instance?.GetMultiplier(PowerUpType.JumpBoost) ?? 1f;

        PlayJumpSound();
        EmitJumpParticles();

        _rb.velocity = new Vector2(_rb.velocity.x, JumpSpeed * jumpMult);

        ComboSystem.Instance?.RegisterAction(ComboAction.ChainJump, 5);
    }

    void DoDoubleJump()
    {
        IsDoubleJumping = true;
        GameManager.Instance.JumpTime = 2;
        GameManager.Instance.JumpFlag = false;

        float jumpMult = PowerUpManager.Instance?.GetMultiplier(PowerUpType.JumpBoost) ?? 1f;

        PlayJumpSound();
        EmitDoubleJumpParticles();

        _rb.velocity = new Vector2(_rb.velocity.x, JumpSpeed * 0.8f * jumpMult);

        ComboSystem.Instance?.RegisterAction(ComboAction.ChainJump, 8);
    }

    #endregion

    #region 移动处理

    void HandleMovement()
    {
        if (DashAbility != null && DashAbility.IsDashing) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING && GameManager.Instance.CanMove)
        {
            float speedMult = PowerUpManager.Instance?.GetMultiplier(PowerUpType.SpeedBoost) ?? 1f;
            float effectiveMoveSpeed = MoveSpeed * speedMult;
            float effectiveAirControl = AirControl * speedMult;

            if (IsGrounded)
            {
                float targetVelocityX = _moveInput * effectiveMoveSpeed;
                _rb.velocity = new Vector2(targetVelocityX, _rb.velocity.y);
            }
            else
            {
                float targetVelocityX = _rb.velocity.x + _moveInput * effectiveAirControl;
                targetVelocityX = Mathf.Clamp(targetVelocityX, -effectiveMoveSpeed, effectiveMoveSpeed);
                _rb.velocity = new Vector2(targetVelocityX, _rb.velocity.y);
            }

            if (_moveInput != 0)
            {
                transform.rotation = Quaternion.AngleAxis(_moveInput < 0 ? 180 : 0, Vector3.up);
            }
        }
        else if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING)
        {
            if (!(DashAbility != null && DashAbility.IsDashing))
                _rb.velocity = new Vector2(0, _rb.velocity.y);
        }
    }

    #endregion

    #region 状态管理

    void UpdatePlayerState()
    {
        // 检测下落状态
        if (_rb.velocity.y < 0 && !IsGrounded)
        {
            IsFalling = true;
        }
        else
        {
            IsFalling = false;
        }

        // 检测跳跃状态
        if (_rb.velocity.y > 0 && !IsGrounded)
        {
            IsJumping = true;
        }

        _lastYPosition = transform.position.y;

        // Jump attempt after state updates.
        TryConsumeBufferedJump();
    }

    void CheckGround()
    {
        // 从玩家底部向下发射射线检测地面
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + Vector3.down * (_collider.bounds.extents.y),
            Vector2.down,
            GroundCheckDistance,
            _groundLayer
        );

        bool wasGrounded = IsGrounded;
        IsGrounded = hit.collider != null;

        if (IsGrounded)
        {
            _lastGroundedTime = Time.time;
        }

        if (IsGrounded && !wasGrounded)
        {
            OnLand();
        }
    }

    void OnLand()
    {
        bool wasFalling = IsFalling;
        IsJumping = false;
        IsDoubleJumping = false;
        GameManager.Instance.JumpTime = 0;
        GameManager.Instance.JumpFlag = true;

        if (wasFalling)
        {
            ComboSystem.Instance?.RegisterAction(ComboAction.PerfectLanding, 10);
            if (PerfectLandParticles != null)
                PerfectLandParticles.Emit(10);
        }

        if (LandClip != null)
        {
            AudioSource.PlayClipAtPoint(LandClip, transform.position);
        }
    }

    #endregion

    #region 碰撞检测

    void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Dead"))
        {
            if (ShieldAbility != null && ShieldAbility.ConsumeShield())
            {
                return;
            }

            IsGrounded = false;
            Die();
        }
        if (other.gameObject.CompareTag("Ground"))
        {
            GameManager.Instance.CanMove = true;
        }
        if (other.gameObject.CompareTag("Pass"))
        {
            GameManager.Instance.ChangeState(GameManager.GameState.VICTORY);
            if (VictoryUI != null)
            {
                VictoryUI.SetActive(true);
            }
            Time.timeScale = 0;

            ComboSystem.Instance?.BreakCombo();

            GameManager.Instance.UnlockNextLevel();
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            IsGrounded = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coin"))
        {
            CollectCoin(other.gameObject);
        }
    }

    #endregion

    #region 交互方法

    void CollectCoin(GameObject coin)
    {
        float scoreMult = PowerUpManager.Instance?.GetMultiplier(PowerUpType.DoubleScore) ?? 1f;
        int baseValue = Mathf.RoundToInt(10 * scoreMult);

        GameManager.Instance.CollectCoin(baseValue);
        PlayCoinSound();
        Destroy(coin);

        ComboSystem.Instance?.RegisterAction(ComboAction.CollectCoin, baseValue);
    }

    public void Die()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING)
        {
            PlayDeathSound();
            EmitDeathParticles();

            if (DeadPic != null)
            {
                Instantiate(DeadPic, transform.position, transform.rotation);
            }

            _rb.velocity = Vector2.zero;
            GameManager.Instance.JumpTime = 0;
            GameManager.Instance.JumpFlag = true;
            GameManager.Instance.CanMove = false;

            ComboSystem.Instance?.BreakCombo();
            PowerUpManager.Instance?.DeactivateAllPowerUps();
            DashAbility?.ResetDash();
            ShieldAbility?.ResetShield();

            StartCoroutine(RespawnAfterDelay(0.5f));
        }
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Respawn();
    }

    void Respawn()
    {
        if (ActiveCheckpoint != null)
        {
            transform.position = ActiveCheckpoint.transform.position;
        }
        else
        {
            // 如果没有重生点，重置到关卡起点
            transform.position = Vector3.zero;
        }

        GameManager.Instance.ChangeState(GameManager.GameState.PLAYING);
        GameManager.Instance.CanMove = true;
    }

    #endregion

    #region 动画管理

    void UpdateAnimation()
    {
        float speedX = _rb.velocity.x;
        float speedY = _rb.velocity.y;

        _animator.SetFloat("HorizontalSpeed", speedX * speedX);
        _animator.SetFloat("VerticalSpeed", speedY);
        _animator.SetBool("Grounded", IsGrounded);
        if (AnimatorHasParameter(_animator, "IsJumping"))
            _animator.SetBool("IsJumping", IsJumping);
        if (AnimatorHasParameter(_animator, "IsDoubleJumping"))
            _animator.SetBool("IsDoubleJumping", IsDoubleJumping);
        if (AnimatorHasParameter(_animator, "IsFalling"))
            _animator.SetBool("IsFalling", IsFalling);

        if (AnimatorHasParameter(_animator, "IsDashing"))
            _animator.SetBool("IsDashing", DashAbility != null && DashAbility.IsDashing);
        if (AnimatorHasParameter(_animator, "HasShield"))
            _animator.SetBool("HasShield", ShieldAbility != null && ShieldAbility.IsShieldActive);
    }

    #endregion

    #region 音效和粒子

    void PlayJumpSound()
    {
        if (JumpClip != null)
        {
            AudioSource.PlayClipAtPoint(JumpClip, transform.position);
        }
    }

    void PlayDeathSound()
    {
        if (DeadClip != null)
        {
            AudioSource.PlayClipAtPoint(DeadClip, transform.position);
        }
    }

    void PlayCoinSound()
    {
        if (CoinClip != null)
        {
            AudioSource.PlayClipAtPoint(CoinClip, transform.position);
        }
    }

    void EmitJumpParticles()
    {
        if (JumpParticles_Floor != null)
        {
            JumpParticles_Floor.Emit(20);
        }
    }

    void EmitDoubleJumpParticles()
    {
        if (JumpParticles_DoubleJump != null)
        {
            JumpParticles_DoubleJump.Emit(10);
        }
    }

    void EmitDeathParticles()
    {
        if (DeathParticles != null)
        {
            DeathParticles.Play();
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 安卓摇杆调用 - 设置移动输入
    /// </summary>
    public void SetMoveInput(float input)
    {
        _moveInput = input;
    }

    /// <summary>
    /// 跳跃输入接口
    /// </summary>
    public void Jump()
    {
        _lastJumpPressedTime = Time.time;
    }

    /// <summary>
    /// 外部弹跳（如弹簧草）：设置向上速度并重置跳跃状态。
    /// 供 Grass 等调用，避免外部脚本直写 GameManager 的 jumpFlag/jumptime 字段。
    /// </summary>
    public void ExternalBounce(float force)
    {
        if (_rb != null)
            _rb.velocity = new Vector2(_rb.velocity.x, force);
        IsJumping = false;
        IsDoubleJumping = false;
        GameManager.Instance.JumpTime = 0;
        GameManager.Instance.JumpFlag = true;
    }

    #endregion

    #region 工具方法

    static bool AnimatorHasParameter(Animator animator, string paramName)
    {
        if (animator == null) return false;
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    #endregion
}
