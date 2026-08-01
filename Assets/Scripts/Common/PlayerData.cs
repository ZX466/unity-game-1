using UnityEngine;

public class PlayerData : MonoBehaviour
{
    #region 单例

    private static PlayerData _instance;

    public static PlayerData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerData>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerData");
                    _instance = go.AddComponent<PlayerData>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 玩家属性

    public GameObject player;
    public bool IsFirstTime = true;
    public bool CanMove = false;
    public bool JumpFlag = true;
    public int JumpTime = 0;

    #endregion

    #region 初始化

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void Reset()
    {
        JumpFlag = true;
        JumpTime = 0;
        CanMove = false;
    }

    #endregion
}
