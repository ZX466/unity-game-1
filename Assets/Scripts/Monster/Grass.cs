using UnityEngine;
using System.Collections;

public class Grass : MonoBehaviour {

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Player")//判断小人碰到的是否为小草顶部
        {
            if (coll.contacts[0].normal.x > -1f && coll.contacts[0].normal.x < 1f && coll.contacts[0].normal.y < -0.8f && coll.contacts[0].normal.y > -1.8f)
            {
                // 走 PlayerControl 公开接口，不再直写 GameManager.jumpFlag/jumptime，
                // 也不再直接操作玩家 Rigidbody2D。
                PlayerControl pc = coll.gameObject.GetComponent<PlayerControl>();
                if (pc != null)
                {
                    pc.ExternalBounce(25);
                }
            }
        }
    }
}
