using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// 统一输入管理器 - 高级版
/// 负责管理键盘、触摸、摇杆等所有输入方式，提供统一的输入接口
/// 支持输入配置和调试
/// </summary>
public class InputManager : MonoBehaviour
{
    #region 输入类型枚举

    public enum InputType
    {
        Keyboard,
        Touch,
        Joystick
    }

    #endregion

    #region 配置属性

    [Header("输入配置")]
    public InputType CurrentInputType = InputType.Keyboard;
    public bool AllowInput = true;

    #endregion

    #region 移动输入

    [Header("移动输入")]
    public float MoveInput;
    public UnityEvent<float> OnMoveInputChanged;

    #endregion

    #region 跳跃输入

    [Header("跳跃输入")]
    public bool JumpInput;
    public UnityEvent OnJumpInput;

    #endregion

    #region 操作输入

    [Header("操作输入")]
    public bool PauseInput;
    public UnityEvent OnPauseInput;

    #endregion

    #region 内部属性

    private PlayerControl _player;
    private bool _jumpPressed;
    private bool _pausePressed;

    #endregion

    #region 初始化

    void Awake()
    {
        FindPlayer();
        SetupInputEvents();
    }

    void Start()
    {
        // 根据平台自动选择输入类型
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            CurrentInputType = InputType.Touch;
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.GetComponent<PlayerControl>();
        }
    }

    void SetupInputEvents()
    {
        if (_player != null)
        {
            OnMoveInputChanged.AddListener(_player.SetMoveInput);
            OnJumpInput.AddListener(_player.Jump);
        }
    }

    #endregion

    #region 更新循环

    void Update()
    {
        if (!AllowInput) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING)
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        switch (CurrentInputType)
        {
            case InputType.Keyboard:
                HandleKeyboardInput();
                break;
            case InputType.Touch:
                HandleTouchInput();
                break;
            case InputType.Joystick:
                HandleJoystickInput();
                break;
        }
    }

    #endregion

    #region 键盘输入处理

    void HandleKeyboardInput()
    {
        // 移动输入
        float rawInput = Input.GetAxis("Horizontal");
        MoveInput = rawInput;
        OnMoveInputChanged?.Invoke(MoveInput);

        // 跳跃输入
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        if (jumpPressed && !_jumpPressed)
        {
            JumpInput = true;
            OnJumpInput?.Invoke();
        }
        _jumpPressed = jumpPressed;

        // 暂停输入
        bool pausePressed = Input.GetKeyDown(KeyCode.Escape);
        if (pausePressed && !_pausePressed)
        {
            PauseInput = true;
            OnPauseInput?.Invoke();
        }
        _pausePressed = pausePressed;
    }

    #endregion

    #region 触摸输入配置

    [Header("触摸区域配置")]
    public Rect JumpButtonArea = new Rect(Screen.width * 0.7f, Screen.height * 0.1f, Screen.width * 0.25f, Screen.height * 0.2f);
    public Rect MoveArea = new Rect(0, Screen.height * 0.3f, Screen.width * 0.5f, Screen.height * 0.4f);

    #endregion

    #region 触摸输入处理

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Stationary)
                {
                    if (IsJumpButtonTouch(touch.position))
                    {
                        JumpInput = true;
                        OnJumpInput?.Invoke();
                    }

                    float moveValue = GetMoveInputFromTouch(touch.position);
                    if (Mathf.Abs(moveValue) > 0.1f)
                    {
                        MoveInput = moveValue;
                        OnMoveInputChanged?.Invoke(MoveInput);
                    }
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    MoveInput = 0;
                    OnMoveInputChanged?.Invoke(MoveInput);
                }
            }
        }
    }

    bool IsJumpButtonTouch(Vector2 screenPosition)
    {
        return JumpButtonArea.Contains(screenPosition);
    }

    float GetMoveInputFromTouch(Vector2 screenPosition)
    {
        if (!MoveArea.Contains(screenPosition)) return 0;

        float areaCenterX = MoveArea.x + MoveArea.width * 0.5f;
        float normalizedX = (screenPosition.x - areaCenterX) / (MoveArea.width * 0.5f);
        return Mathf.Clamp(normalizedX, -1f, 1f);
    }

    #endregion

    #region 摇杆输入处理

    void HandleJoystickInput()
    {
        // 通过EasyTouch插件处理摇杆输入
        // 在JoystickControl脚本中已经实现
    }

    #endregion

    #region 公共方法

    public void SetInputType(InputType type)
    {
        CurrentInputType = type;
        ResetInput();
    }

    public void ResetInput()
    {
        MoveInput = 0;
        JumpInput = false;
        PauseInput = false;
    }

    public void EnableInput()
    {
        AllowInput = true;
        ResetInput();
    }

    public void DisableInput()
    {
        AllowInput = false;
        ResetInput();
    }

    #endregion

    #region 调试方法

    void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUILayout.Label("Input Manager Debug:");
            GUILayout.Label("Input Type: " + CurrentInputType);
            GUILayout.Label("Allow Input: " + AllowInput);
            GUILayout.Label("Move Input: " + MoveInput);
            GUILayout.Label("Jump Input: " + JumpInput);
            GUILayout.Label("Pause Input: " + PauseInput);

            if (GUILayout.Button("Switch to Keyboard"))
                CurrentInputType = InputType.Keyboard;
            if (GUILayout.Button("Switch to Touch"))
                CurrentInputType = InputType.Touch;
            if (GUILayout.Button("Switch to Joystick"))
                CurrentInputType = InputType.Joystick;
        }
    }

    #endregion
}