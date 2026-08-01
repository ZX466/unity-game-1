using UnityEngine;
using System.Collections;

public class WaterControl : MonoBehaviour {

    private Rigidbody2D _rb;

    // Use this for initialization
    void Start () {
        _rb = GetComponent<Rigidbody2D>();
    }

    // Rigidbody2D.velocity 应在 FixedUpdate 设置（与物理步长一致）；原 Update 写法帧率不匹配会抖动，
    // 且每帧 GetComponent 开销大。
    void FixedUpdate () {
        if (_rb != null)
            _rb.velocity = new Vector2(0, -7f);//水层下落
    }

    //水层循环
    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "VerticalTrigger")
        {
            this.transform.Translate(new Vector3(0, 60, 0));
        }
    }
}
