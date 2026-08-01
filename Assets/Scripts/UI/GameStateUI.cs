using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 高级游戏状态UI控制器 - 重构版
/// 负责管理游戏状态转换、UI显示和用户交互
/// 支持动画过渡和状态监听
/// </summary>
public class GameStateUI : MonoBehaviour
{
    #region UI引用

    [Header("面板")]
    public GameObject MenuPanel;
    public GameObject GamePanel;
    public GameObject PausePanel;
    public GameObject GameOverPanel;
    public GameObject VictoryPanel;

    #endregion

    #region 菜单UI

    [Header("菜单UI")]
    public Button StartButton;
    public Button QuitButton;

    #endregion

    #region 游戏中UI

    [Header("游戏中UI")]
    public Text ScoreText;
    public Text CoinText;
    public Text TimeText;
    public Button PauseButton;
    public Image HealthBar;

    [Header("颜色机制提示")]
    public Text ColorHintText;
    public float HintRefreshInterval = 0.25f;
    private float _nextHintRefreshTime;

    #endregion

    #region 暂停UI

    [Header("暂停UI")]
    public Button ContinueButton;
    public Button RestartButton;
    public Button QuitToMenuButton;

    #endregion

    #region 游戏结束UI

    [Header("游戏结束UI")]
    public Text FinalScoreText;
    public Button RestartGameOverButton;
    public Button QuitGameOverButton;

    #endregion

    #region 胜利UI

    [Header("胜利UI")]
    public Text VictoryScoreText;
    public Button NextLevelButton;
    public Button QuitVictoryButton;

    #endregion

    #region 动画

    [Header("动画")]
    public Animator PanelAnimator;

    #endregion

    #region 初始化

    void Awake()
    {
        SetupUIElements();
        SetupEventListeners();
    }

    void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        GameManager.Instance.OnPlayerDeath += OnPlayerDeath;
        GameManager.Instance.OnLevelComplete += OnLevelComplete;
    }

    void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        GameManager.Instance.OnPlayerDeath -= OnPlayerDeath;
        GameManager.Instance.OnLevelComplete -= OnLevelComplete;
    }

    #endregion

    #region UI设置

    void SetupUIElements()
    {
        if (MenuPanel != null) MenuPanel.SetActive(false);
        if (GamePanel != null) GamePanel.SetActive(false);
        if (PausePanel != null) PausePanel.SetActive(false);
        if (GameOverPanel != null) GameOverPanel.SetActive(false);
        if (VictoryPanel != null) VictoryPanel.SetActive(false);

        if (HealthBar != null) HealthBar.fillAmount = 1;
    }

    void SetupEventListeners()
    {
        // 菜单按钮
        if (StartButton != null) StartButton.onClick.AddListener(OnStartButtonClick);
        if (QuitButton != null) QuitButton.onClick.AddListener(OnQuitButtonClick);

        // 游戏中按钮
        if (PauseButton != null) PauseButton.onClick.AddListener(OnPauseButtonClick);

        // 暂停面板按钮
        if (ContinueButton != null) ContinueButton.onClick.AddListener(OnContinueButtonClick);
        if (RestartButton != null) RestartButton.onClick.AddListener(OnRestartButtonClick);
        if (QuitToMenuButton != null) QuitToMenuButton.onClick.AddListener(OnQuitToMenuButtonClick);

        // 游戏结束面板按钮
        if (RestartGameOverButton != null) RestartGameOverButton.onClick.AddListener(OnRestartButtonClick);
        if (QuitGameOverButton != null) QuitGameOverButton.onClick.AddListener(OnQuitButtonClick);

        // 胜利面板按钮
        if (NextLevelButton != null) NextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
        if (QuitVictoryButton != null) QuitVictoryButton.onClick.AddListener(OnQuitButtonClick);
    }

    #endregion

    #region 状态响应

    void OnGameStateChanged(GameManager.GameState oldState, GameManager.GameState newState)
    {
        Debug.Log($"状态变化: {oldState} -> {newState}");
        ShowPanelForState(newState);
    }

    void ShowPanelForState(GameManager.GameState state)
    {
        // 隐藏所有面板
        HideAllPanels();

        // 显示对应状态的面板
        switch (state)
        {
            case GameManager.GameState.MENU:
                ShowMenu();
                break;
            case GameManager.GameState.PLAYING:
                ShowGame();
                break;
            case GameManager.GameState.PAUSE:
                ShowPause();
                break;
            case GameManager.GameState.GAMEOVER:
                ShowGameOver();
                break;
            case GameManager.GameState.VICTORY:
                ShowVictory();
                break;
        }
    }

    void HideAllPanels()
    {
        if (MenuPanel != null) MenuPanel.SetActive(false);
        if (GamePanel != null) GamePanel.SetActive(false);
        if (PausePanel != null) PausePanel.SetActive(false);
        if (GameOverPanel != null) GameOverPanel.SetActive(false);
        if (VictoryPanel != null) VictoryPanel.SetActive(false);
    }

    #endregion

    #region 面板显示

    void ShowMenu()
    {
        if (MenuPanel != null) MenuPanel.SetActive(true);
        Time.timeScale = 0;
    }

    void ShowGame()
    {
        if (GamePanel != null) GamePanel.SetActive(true);
        Time.timeScale = 1;
        UpdateGameUI();
    }

    void ShowPause()
    {
        if (PausePanel != null) PausePanel.SetActive(true);
        Time.timeScale = 0;
    }

    void ShowGameOver()
    {
        if (GameOverPanel != null) GameOverPanel.SetActive(true);
        if (FinalScoreText != null)
        {
            FinalScoreText.text = $"最终得分: {GameManager.Instance.TotalScore}";
        }
        Time.timeScale = 0;
    }

    void ShowVictory()
    {
        if (VictoryPanel != null) VictoryPanel.SetActive(true);
        if (VictoryScoreText != null)
        {
            VictoryScoreText.text = $"得分: {GameManager.Instance.TotalScore}";
        }
        Time.timeScale = 0;
    }

    #endregion

    #region 游戏中UI更新

    void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.PLAYING)
        {
            UpdateGameUI();
            UpdateColorHint();
        }
    }

    void UpdateGameUI()
    {
        if (ScoreText != null) ScoreText.text = $"得分: {GameManager.Instance.TotalScore}";
        if (CoinText != null) CoinText.text = $"金币: {GameManager.Instance.CoinsCollected}";
        if (TimeText != null) TimeText.text = GameManager.Instance.GetFormattedTime();
    }

    void UpdateColorHint()
    {
        if (ColorHintText == null) return;
        if (Time.time < _nextHintRefreshTime) return;
        _nextHintRefreshTime = Time.time + Mathf.Max(0.05f, HintRefreshInterval);

        // Find a BackColor object in scene (there can be multiple).
        var backColor = GameObject.FindGameObjectWithTag("BackColor");
        if (backColor == null)
        {
            ColorHintText.text = "";
            return;
        }

        var r = backColor.GetComponent<Renderer>();
        if (r == null)
        {
            ColorHintText.text = "";
            return;
        }

        Color c = r.material.color;
        // Compact readable label; avoids needing custom sprites.
        ColorHintText.text = $"背景色: R{Mathf.RoundToInt(c.r * 255)} G{Mathf.RoundToInt(c.g * 255)} B{Mathf.RoundToInt(c.b * 255)}";
    }

    #endregion

    #region 事件处理

    void OnPlayerDeath()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.GAMEOVER);
    }

    void OnLevelComplete()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.VICTORY);
    }

    #endregion

    #region 按钮点击

    public void OnStartButtonClick()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.PLAYING);
        HideAllPanels();
        GameManager.Instance.StartCoroutine(GameManager.Instance.GameStartCountdown());
    }

    public void OnQuitButtonClick()
    {
        Application.Quit();
    }

    public void OnPauseButtonClick()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.PAUSE);
    }

    public void OnContinueButtonClick()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.PLAYING);
    }

    public void OnRestartButtonClick()
    {
        GameManager.Instance.RestartLevel();
        Time.timeScale = 1;
    }

    public void OnQuitToMenuButtonClick()
    {
        GameManager.Instance.ReturnToMenu();
    }

    public void OnNextLevelButtonClick()
    {
        int nextLevel = GameManager.Instance.GetLevelIndexByName(SceneManager.GetActiveScene().name) + 1;
        if (nextLevel >= 1 && nextLevel <= 3)
        {
            string[] levelNames = { "waterfall", "cave", "volcanocave" };
            GameManager.Instance.LoadLevel(levelNames[nextLevel]);
        }
        else
        {
            GameManager.Instance.ReturnToMenu();
        }
    }

    #endregion
}
