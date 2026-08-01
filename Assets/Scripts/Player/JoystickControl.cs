using UnityEngine;
using System.Collections;

/// <summary>
/// 高级摇杆控制 - 重构版
/// 支持EasyTouch插件的摇杆输入处理
/// 适配新的GameManager架构
/// </summary>
public class JoystickControl : MonoBehaviour
{
    #region 引用

    [Header("动画")]
    private Animator AnimatorController;

    #endregion

    #region 移动参数

    [Header("移动参数")]
    public float JumpSpeed;
    public float MoveSpeed;

    #endregion

    #region 音效

    [Header("音效")]
    public AudioClip JumpSound;
    public AudioClip DeathSound;

    #endregion

    #region 粒子特效

    [Header("粒子特效")]
    public ParticleSystem JumpParticles_Floor;
    public ParticleSystem JumpParticles_DoubleJump;
    public ParticleSystem DeathParticles;

    #endregion

    #region 初始化

    void Awake()
    {
        AnimatorController = GetComponent<Animator>();
    }

    void OnEnable()
    {
        // 绑定EasyTouch事件
        EasyJoystick.On_JoystickMove += OnJoystickMove;
        EasyJoystick.On_JoystickMoveEnd += OnJoystickMoveEnd;
        EasyButton.On_ButtonDown += OnButtonDown;
    }

    void OnDisable()
    {
        // 取消绑定EasyTouch事件
        EasyJoystick.On_JoystickMove -= OnJoystickMove;
        EasyJoystick.On_JoystickMoveEnd -= OnJoystickMoveEnd;
        EasyButton.On_ButtonDown -= OnButtonDown;
    }

    #endregion

    #region 摇杆移动

    // 移动摇杆结束  
    void OnJoystickMoveEnd(MovingJoystick move)
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING && move.joystickName == "MoveJoystick")
        {
            // 停止时，角色恢复速度0
            GetComponent<Rigidbody2D>().velocity = new Vector2(0, GetComponent<Rigidbody2D>().velocity.y);
        }
    }

    // 移动摇杆中  
    void OnJoystickMove(MovingJoystick move)
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING && GameManager.Instance.CanMove)
        {
            if (move.joystickName != "MoveJoystick")
            {
                return;
            }

            // 获取摇杆中心偏移的坐标  
            float joyPositionX = move.joystickAxis.x;

            // 摇杆偏左向左移并转向，同理向右
            if (joyPositionX > 0)
            {
                transform.rotation = Quaternion.AngleAxis(0, Vector3.up);
                GetComponent<Rigidbody2D>().velocity = new Vector2(MoveSpeed, GetComponent<Rigidbody2D>().velocity.y);
            }
            else if (joyPositionX < 0)
            {
                transform.rotation = Quaternion.AngleAxis(180, Vector3.up);
                GetComponent<Rigidbody2D>().velocity = new Vector2(-MoveSpeed, GetComponent<Rigidbody2D>().velocity.y);
            }
        }
    }

    #endregion

    #region 按钮输入

    // 按下button时
    void OnButtonDown(string buttonName)
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING && GameManager.Instance.CanMove)
        {
            // 如果按下的是JumpButton  
            if (buttonName == "JumpButton")
            {
                // 二连跳判断
                if (GameManager.Instance.JumpFlag && GameManager.Instance.JumpTime < 2)
                {
                    GameManager.Instance.JumpTime++;
                    PlayJumpSound();
                    EmitJumpParticles();

                    // 第二下起跳粒子效果
                    if (GameManager.Instance.JumpTime == 2)
                    {
                        EmitDoubleJumpParticles();
                        GameManager.Instance.JumpFlag = false;
                    }

                    GetComponent<Rigidbody2D>().velocity = new Vector2(GetComponent<Rigidbody2D>().velocity.x, JumpSpeed);
                }
            }
        }
    }

    #endregion

    #region 辅助方法

    void PlayJumpSound()
    {
        if (JumpSound != null)
        {
            AudioSource.PlayClipAtPoint(JumpSound, this.transform.position);
        }
    }

    void EmitJumpParticles()
    {
        if (GameManager.Instance.JumpTime == 1 && JumpParticles_Floor != null)
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

    #endregion
}