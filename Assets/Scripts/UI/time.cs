using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 计时器显示层：纯展示 GameManager.GetFormattedTime()。
/// 原实现用 static time_by 跨场景累加，且唯一清零入口 reTimer() 全库 0 处调用，
/// 导致玩到第三关显示的是从第一关起的累计时间；hour 赋值被注释使 60 分钟后不进位。
/// 现统一以 GameManager.GameTime 为唯一数据源，重置由 GameManager.ResetPlayerState() 负责，
/// TimeChallengeQuest 的 30 秒判定也随之与 UI 一致。
/// </summary>
public class time : MonoBehaviour
{
    private Text _textTime;

    void Start()
    {
        _textTime = GetComponent<Text>();
    }

    void Update()
    {
        if (_textTime == null) return;
        _textTime.text = GameManager.Instance.GetFormattedTime();
    }
}
