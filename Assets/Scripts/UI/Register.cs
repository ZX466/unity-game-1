using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[Obsolete("登录系统已废弃，相关场景 register 不存在")]
public class Register : MonoBehaviour {

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(ChangeScene);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void ChangeScene()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.PREPARING);
        const string sceneName = "register";
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[Register] Scene is not in Build Settings: " + sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
