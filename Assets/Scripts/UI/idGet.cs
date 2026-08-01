using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Obsolete("登录系统已废弃")]
public class idGet : MonoBehaviour {

    Text idText;
	// Use this for initialization
	void Start () {
        
        idText=  GetComponent<Text>();
        
    }
	
	// Update is called once per frame
	void Update () {
        idText.text= GameManager.getInstance().id_Login;

    }
}
