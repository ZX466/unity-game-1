using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 镜像显示计时文本。原实现裸解引用 static time.textTime 无 null 检查，
/// 且与 time 脚本的执行顺序耦合（textshow.Update 早于 time.Start 即每帧抛 NRE）。
/// 现直接读 GameManager，消除对 time 实例的隐式依赖与执行顺序耦合。
/// </summary>
public class textshow : MonoBehaviour
{
    private Text _text;

    void Start()
    {
        _text = GetComponent<Text>();
    }

    void Update()
    {
        if (_text == null) return;
        _text.text = GameManager.Instance.GetFormattedTime();
    }
}
