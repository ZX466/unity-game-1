using UnityEngine;
using System.Collections;

/// <summary>
/// 高级任务系统 - 重构版
/// 负责管理游戏中的任务和成就系统
/// 支持任务跟踪和进度管理
/// </summary>
public class QuestSystem : MonoBehaviour
{
    #region 单例模式

    private static QuestSystem instance;

    public static QuestSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<QuestSystem>();
                if (instance == null)
                {
                    GameObject container = new GameObject("QuestSystem");
                    instance = container.AddComponent<QuestSystem>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region 任务定义

    [System.Serializable]
    public class Quest
    {
        public string QuestName;
        public string QuestDescription;
        public int RequiredCount;
        public int CurrentCount;
        public bool IsCompleted;
        public int RewardScore;
        public int RewardCoins;

        public Quest(string name, string description, int requiredCount, int rewardScore, int rewardCoins)
        {
            QuestName = name;
            QuestDescription = description;
            RequiredCount = requiredCount;
            CurrentCount = 0;
            IsCompleted = false;
            RewardScore = rewardScore;
            RewardCoins = rewardCoins;
        }

        public void UpdateProgress(int amount)
        {
            if (!IsCompleted)
            {
                CurrentCount += amount;
                if (CurrentCount >= RequiredCount)
                {
                    CompleteQuest();
                }
            }
        }

        public void CompleteQuest()
        {
            CurrentCount = RequiredCount;
            IsCompleted = true;
            GiveReward();
        }

        void GiveReward()
        {
            // 走 GameManager 公开方法：原代码 CoinsCollected 直接改字段不落盘，
            // 只靠 AddScore 内部 SavePlayerData 顺手写入，顺序依赖且脆弱。
            GameManager.Instance.GrantReward(RewardScore, RewardCoins);
        }
    }

    #endregion

    #region 任务列表

    [Header("任务")]
    public Quest CollectCoinsQuest;
    public Quest ReachDestinationQuest;
    public Quest TimeChallengeQuest;

    #endregion

    #region 初始化

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeQuests();
        SetupEventListeners();
    }

    void InitializeQuests()
    {
        CollectCoinsQuest = new Quest("收集金币", "收集10个金币", 10, 50, 10);
        ReachDestinationQuest = new Quest("到达终点", "到达关卡终点", 1, 100, 5);
        TimeChallengeQuest = new Quest("快速通关", "在30秒内完成关卡", 1, 200, 15);
    }

    private GameManager _gm;

    void SetupEventListeners()
    {
        // 缓存引用，OnDestroy 反订阅时不再触达 Instance（避免 GM 已销毁时被重建）。
        _gm = GameManager.Instance;
        _gm.OnCoinCollected += OnCoinCollected;
        _gm.OnLevelComplete += OnLevelComplete;
    }

    void OnDestroy()
    {
        if (_gm != null)
        {
            _gm.OnCoinCollected -= OnCoinCollected;
            _gm.OnLevelComplete -= OnLevelComplete;
        }
    }

    #endregion

    #region 事件监听

    void OnCoinCollected()
    {
        CollectCoinsQuest.UpdateProgress(1);
    }

    void OnLevelComplete()
    {
        ReachDestinationQuest.UpdateProgress(1);
        if (GameManager.Instance.GetGameTime() <= 30)
        {
            TimeChallengeQuest.UpdateProgress(1);
        }
    }

    #endregion

    #region 任务查询

    public Quest[] GetAllQuests()
    {
        return new Quest[] { CollectCoinsQuest, ReachDestinationQuest, TimeChallengeQuest };
    }

    public int GetCompletedQuestCount()
    {
        int count = 0;
        if (CollectCoinsQuest.IsCompleted) count++;
        if (ReachDestinationQuest.IsCompleted) count++;
        if (TimeChallengeQuest.IsCompleted) count++;
        return count;
    }

    public int GetTotalQuestCount()
    {
        return 3;
    }

    #endregion

    #region 任务重置

    public void ResetQuests()
    {
        CollectCoinsQuest = new Quest("收集金币", "收集10个金币", 10, 50, 10);
        ReachDestinationQuest = new Quest("到达终点", "到达关卡终点", 1, 100, 5);
        TimeChallengeQuest = new Quest("快速通关", "在30秒内完成关卡", 1, 200, 15);
    }

    #endregion

    #region 任务进度

    public string GetQuestProgressText(Quest quest)
    {
        return $"{quest.CurrentCount}/{quest.RequiredCount}";
    }

    public float GetQuestProgressPercent(Quest quest)
    {
        if (quest == null || quest.RequiredCount <= 0) return 0f;
        return Mathf.Clamp01((float)quest.CurrentCount / quest.RequiredCount);
    }

    #endregion
}