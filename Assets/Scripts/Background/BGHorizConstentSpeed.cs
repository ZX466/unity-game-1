using UnityEngine;
using System.Collections;

public class BGHorizConstentSpeed : MonoBehaviour {

    private float wait = 0.5f;
    private float rush = 0.5f;
    public float HorzDis;
    public float HorzSpeed;
    private Rigidbody2D _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (_rb != null)
            _rb.velocity = new Vector2(HorzSpeed, 0);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag == "HorizontalTrigger")
        {
            // 用 rb.position 重置而非 transform.Translate，避免与 Rigidbody2D 速度叠加产生抖动/穿模。
            if (_rb != null)
                _rb.position += new Vector2(HorzDis, 0);
            else
                this.transform.Translate(new Vector3(HorzDis, 0, 0));
        }
    }
}
