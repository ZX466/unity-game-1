using UnityEngine;

/// <summary>
/// 检查点/重生点组件
/// 玩家触碰后，会将重生点更新到这里
/// </summary>
public class CheckPoint : MonoBehaviour
{
    [Header("激活时粒子效果")]
    public ParticleSystem activateEffect;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControl player = other.GetComponent<PlayerControl>();
            if (player != null)
            {
                player.ActiveCheckpoint = this;
                // 播放激活特效
                if (activateEffect != null && !activateEffect.isPlaying)
                {
                    activateEffect.Play();
                }
            }
        }
    }
}
