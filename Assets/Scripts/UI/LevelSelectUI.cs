using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 关卡选择UI控制器 - 高级版
/// 负责管理关卡选择界面的显示和交互
/// 支持关卡锁定/解锁状态显示
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    #region UI引用

    [Header("关卡按钮")]
    public Button Level1Button;
    public Button Level2Button;
    public Button Level3Button;

    [Header("关卡锁定状态")]
    public Image Level1Lock;
    public Image Level2Lock;
    public Image Level3Lock;

    [Header("关卡分数显示")]
    public Text Level1ScoreText;
    public Text Level2ScoreText;
    public Text Level3ScoreText;

    [Header("其他")]
    public Button BackButton;

    #endregion

    #region 内部属性

    private int _unlockedLevel = 1;
    private int[] _levelScores = new int[3];

    #endregion

    #region 初始化

    void Awake()
    {
        LoadLevelData();
        SetupEventListeners();
        UpdateUI();
    }

    void LoadLevelData()
    {
        // 统一走 SaveManager，键名与版本迁移集中在一处（0-based 旧存档兜底在 SaveManager 内处理）。
        _unlockedLevel = SaveManager.Instance.GetUnlockedLevel();

        for (int i = 0; i < 3; i++)
        {
            _levelScores[i] = SaveManager.Instance.GetLevelScore(i + 1);
        }
    }

    void SetupEventListeners()
    {
        if (Level1Button != null) Level1Button.onClick.AddListener(() => LoadLevel(1));
        if (Level2Button != null) Level2Button.onClick.AddListener(() => LoadLevel(2));
        if (Level3Button != null) Level3Button.onClick.AddListener(() => LoadLevel(3));
        if (BackButton != null) BackButton.onClick.AddListener(BackToMenu);
    }

    #endregion

    #region UI更新

    void UpdateUI()
    {
        UpdateLevelButtons();
        UpdateLevelScores();
        UpdateLevelLockStates();
    }

    void UpdateLevelButtons()
    {
        if (Level1Button != null) Level1Button.interactable = _unlockedLevel >= 1;
        if (Level2Button != null) Level2Button.interactable = _unlockedLevel >= 2;
        if (Level3Button != null) Level3Button.interactable = _unlockedLevel >= 3;
    }

    void UpdateLevelScores()
    {
        if (Level1ScoreText != null)
            Level1ScoreText.text = _levelScores[0] > 0 ? $"分数: {_levelScores[0]}" : "";
        if (Level2ScoreText != null)
            Level2ScoreText.text = _levelScores[1] > 0 ? $"分数: {_levelScores[1]}" : "";
        if (Level3ScoreText != null)
            Level3ScoreText.text = _levelScores[2] > 0 ? $"分数: {_levelScores[2]}" : "";
    }

    void UpdateLevelLockStates()
    {
        if (Level1Lock != null) Level1Lock.gameObject.SetActive(false);
        if (Level2Lock != null) Level2Lock.gameObject.SetActive(_unlockedLevel < 2);
        if (Level3Lock != null) Level3Lock.gameObject.SetActive(_unlockedLevel < 3);
    }

    #endregion

    #region 关卡加载

    public void LoadLevel(int levelNumber)
    {
        if (levelNumber > _unlockedLevel)
        {
            Debug.LogWarning("[LevelSelectUI] Level " + levelNumber + " is locked!");
            return;
        }

        string levelName = GetLevelName(levelNumber);
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("[LevelSelectUI] Invalid level number: " + levelNumber);
            return;
        }

        Debug.Log("[LevelSelectUI] Loading level " + levelNumber + ": " + levelName);
        SafeLoadScene(levelName);
    }

    string GetLevelName(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1: return "waterfall";
            case 2: return "cave";
            case 3: return "volcanocave";
            default: return "";
        }
    }

    void SafeLoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[LevelSelectUI] Scene is not in Build Settings: " + sceneName);
            return;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        
        if (asyncLoad == null)
        {
            Debug.LogError("[LevelSelectUI] Failed to load scene: " + sceneName);
            return;
        }

        StartCoroutine(MonitorLoading(asyncLoad, sceneName));
    }

    IEnumerator MonitorLoading(AsyncOperation asyncLoad, string sceneName)
    {
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        Debug.Log("[LevelSelectUI] Scene loaded: " + sceneName);
    }

    public void BackToMenu()
    {
        Debug.Log("[LevelSelectUI] Returning to main menu");
        const string menuSceneName = "start";
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError("[LevelSelectUI] Scene is not in Build Settings: " + menuSceneName);
            return;
        }

        SceneManager.LoadSceneAsync(menuSceneName, LoadSceneMode.Single);
    }

    #endregion

    #region 场景按钮桥接

    public void OnButtonClick(int levelNumber) => LoadLevel(levelNumber);
    public void OnButtonClickWater() => LoadLevel(1);
    public void OnButtonClickcave() => LoadLevel(2);
    public void OnButtonClickvolcanocave() => LoadLevel(3);

    #endregion

    #region 关卡解锁

    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber > _unlockedLevel)
        {
            _unlockedLevel = levelNumber;
            SaveManager.Instance.UnlockLevel(levelNumber);
            UpdateUI();
        }
    }

    public void SetLevelScore(int levelNumber, int score)
    {
        if (levelNumber >= 1 && levelNumber <= 3 && score > _levelScores[levelNumber - 1])
        {
            _levelScores[levelNumber - 1] = score;
            SaveManager.Instance.SaveLevelScore(levelNumber, score);
            UpdateUI();
        }
    }

    #endregion

    #region 重置数据

    public void ResetLevelData()
    {
        _unlockedLevel = 1;
        _levelScores = new int[3];
        SaveManager.Instance.ResetLevelProgress();
        UpdateUI();
    }

    #endregion
}
