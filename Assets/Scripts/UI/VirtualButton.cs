using UnityEngine;
using System.Collections;

public class VirtualButton : MonoBehaviour {

    GameObject player;

	// Use this for initialization
	void Start () {
        player = GameManager.getInstance().player;
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                // Minimal mobile support: treat tap as jump.
                var pc = player != null ? player.GetComponent<PlayerControl>() : null;
                if (pc != null)
                {
                    pc.Jump();
                }
            }
        }
    }

    // Legacy UI button hooks were removed; the current game uses EasyTouch joystick
    // and PlayerControl's unified jump buffering.
}
