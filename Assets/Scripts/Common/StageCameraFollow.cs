using UnityEngine;
using System.Collections;

/// <summary>
/// 通用关卡相机跟随脚本
/// 替代原来四个关卡重复的CameraFollow脚本，支持边界限制和背景视差
/// </summary>
public class StageCameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("如果不赋值，自动通过Tag寻找Player")]
    public Transform followTarget;
    
    [Header("跟随参数")]
    public float followSpeed = 5f;
    public bool lockZ = true;
    
    [Header("相机边界限制（留空代表不限制）")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public float fixedY; // 如果固定Y值，填这个
    public bool useFixedY = false;

    [Header("背景视差效果")]
    public GameObject[] parallaxBackgrounds;
    public float[] parallaxSpeeds; // 和背景数组长度对应，每个层的视差速度

    private float originalZ;

    void Start()
    {
        // 如果没赋值目标，自动找Player
        if (followTarget == null)
        {
            followTarget = GameObject.FindGameObjectWithTag("Player").transform;
        }

        originalZ = transform.position.z;
    }

    void FixedUpdate()
    {
        if (followTarget == null) return;

        Vector3 oldPos = transform.position;
        // 平滑跟随
        Vector3 newPos = Vector3.Lerp(transform.position, followTarget.position, followSpeed * Time.deltaTime);

        // 应用边界限制
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        if (!useFixedY)
        {
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
        }
        else
        {
            newPos.y = fixedY;
        }

        // 保持Z轴不变
        if (lockZ)
        {
            newPos.z = originalZ;
        }

        // 应用位置
        transform.position = newPos;

        // 处理视差效果
        if (parallaxBackgrounds != null && parallaxBackgrounds.Length > 0)
        {
            Vector3 delta = oldPos - transform.position;
            
            for (int i = 0; i < parallaxBackgrounds.Length; i++)
            {
                if (i < parallaxSpeeds.Length)
                {
                    parallaxBackgrounds[i].transform.Translate(-delta * parallaxSpeeds[i]);
                }
                else
                {
                    // 默认系数
                    parallaxBackgrounds[i].transform.Translate(-delta * 0.5f);
                }
            }
        }
    }
}
