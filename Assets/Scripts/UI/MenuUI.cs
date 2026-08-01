using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuUI : MonoBehaviour {

	void Start () {
        
	}
	
	void Update () {
      
	}

    public void ChangeScene()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.PREPARING);
        LoadLevelSelectScene();
    }

    void LoadLevelSelectScene()
    {
        const string sceneName = "LevelSelect";

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[MenuUI] Scene is not in Build Settings: " + sceneName);
            return;
        }

        Debug.Log("[MenuUI] Loading scene: " + sceneName);

        // 使用异步加载
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        
        if (asyncLoad == null)
        {
            Debug.LogError("[MenuUI] Failed to start scene loading: " + sceneName);
            return;
        }

        // 显示加载进度（可选）
        StartCoroutine(MonitorSceneLoading(asyncLoad));
    }

    IEnumerator MonitorSceneLoading(AsyncOperation asyncLoad)
    {
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log("[MenuUI] Loading progress: " + (progress * 100f).ToString("F0") + "%");
            yield return null;
        }

        Debug.Log("[MenuUI] Scene loaded successfully!");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
