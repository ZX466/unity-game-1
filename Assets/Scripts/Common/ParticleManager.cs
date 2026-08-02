using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 高级粒子特效管理器 - 重构版
/// 负责管理游戏中所有粒子特效的播放和管理
/// 支持对象池和特效优化
/// </summary>
public class ParticleManager : MonoBehaviour
{
    #region 单例模式

    private static ParticleManager instance;

    public static ParticleManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ParticleManager>();
                if (instance == null)
                {
                    GameObject container = new GameObject("ParticleManager");
                    instance = container.AddComponent<ParticleManager>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region 特效类型定义

    public enum ParticleType
    {
        Jump,
        DoubleJump,
        Landing,
        Death,
        CollectCoin,
        Explosion
    }

    #endregion

    #region 特效引用

    [Header("粒子特效模板")]
    public ParticleSystem JumpEffect;
    public ParticleSystem DoubleJumpEffect;
    public ParticleSystem LandingEffect;
    public ParticleSystem DeathEffect;
    public ParticleSystem CollectCoinEffect;
    public ParticleSystem ExplosionEffect;

    [Header("对象池配置")]
    public int InitialPoolSize = 5;

    #endregion

    #region 内部属性

    private GameObject _particleContainer;
    private GameObject[] _effectPool;

    #endregion

    #region 初始化

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeParticleSystem();
    }

    void InitializeParticleSystem()
    {
        CreateParticleContainer();
        SetupParticlePool();
    }

    void CreateParticleContainer()
    {
        _particleContainer = new GameObject("ParticleContainer");
        _particleContainer.transform.SetParent(transform);
        DontDestroyOnLoad(_particleContainer);
    }

    void SetupParticlePool()
    {
        int poolSize = Mathf.Max(1, InitialPoolSize);
        _effectPool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            _effectPool[i] = new GameObject($"ParticleEffect_{i}");
            _effectPool[i].transform.SetParent(_particleContainer.transform);
            _effectPool[i].SetActive(false);
        }
    }

    #endregion

    #region 特效播放

    public void PlayParticle(ParticleType type, Vector3 position, Quaternion rotation = default)
    {
        ParticleSystem effect = GetParticleSystem(type);
        if (effect != null)
        {
            PlayParticleEffect(effect, position, rotation);
        }
    }

    ParticleSystem GetParticleSystem(ParticleType type)
    {
        switch (type)
        {
            case ParticleType.Jump:
                return JumpEffect;
            case ParticleType.DoubleJump:
                return DoubleJumpEffect;
            case ParticleType.Landing:
                return LandingEffect;
            case ParticleType.Death:
                return DeathEffect;
            case ParticleType.CollectCoin:
                return CollectCoinEffect;
            case ParticleType.Explosion:
                return ExplosionEffect;
            default:
                return null;
        }
    }

    void PlayParticleEffect(ParticleSystem templateSystem, Vector3 position, Quaternion rotation = default)
    {
        if (templateSystem == null) return;

        GameObject particleInstance = GetAvailableParticle();

        if (particleInstance == null)
        {
            particleInstance = CreateNewParticle();
        }

        particleInstance.transform.position = position;
        particleInstance.transform.rotation = rotation;

        ParticleSystem ps = particleInstance.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = particleInstance.AddComponent<ParticleSystem>();
        }

        CopyParticleSettings(templateSystem, ps);

        ps.Play();
        particleInstance.SetActive(true);

        StartCoroutine(WaitForParticleEnd(ps, particleInstance));
    }

    GameObject GetAvailableParticle()
    {
        foreach (GameObject particle in _effectPool)
        {
            if (!particle.activeSelf)
            {
                return particle;
            }
        }
        return null;
    }

    GameObject CreateNewParticle()
    {
        int newIndex = _effectPool.Length;
        Array.Resize(ref _effectPool, newIndex + 1);
        _effectPool[newIndex] = new GameObject($"ParticleEffect_{newIndex}");
        _effectPool[newIndex].transform.SetParent(_particleContainer.transform);
        return _effectPool[newIndex];
    }

    void CopyParticleSettings(ParticleSystem source, ParticleSystem destination)
    {
        if (source == null || destination == null) return;

        var srcMain = source.main;
        var dstMain = destination.main;

        dstMain.startSize = srcMain.startSize;
        dstMain.startSpeed = srcMain.startSpeed;
        dstMain.startColor = srcMain.startColor;
        dstMain.duration = srcMain.duration;
        dstMain.loop = false;
        dstMain.playOnAwake = false;
        dstMain.startLifetime = srcMain.startLifetime;
        dstMain.maxParticles = srcMain.maxParticles;

        var srcEmission = source.emission;
        var dstEmission = destination.emission;
        dstEmission.rateOverTime = srcEmission.rateOverTime;
        dstEmission.rateOverDistance = srcEmission.rateOverDistance;

        var srcShape = source.shape;
        var dstShape = destination.shape;
        dstShape.shapeType = srcShape.shapeType;
    }

    IEnumerator WaitForParticleEnd(ParticleSystem ps, GameObject particleInstance)
    {
        if (ps == null || particleInstance == null) yield break;

        float waitTime = ps.main.duration + ps.main.startLifetime.constant + 0.1f;
        yield return new WaitForSeconds(waitTime);
        particleInstance.SetActive(false);
    }

    #endregion

    #region 常用特效快捷方法

    public void PlayJumpEffect(Vector3 position)
    {
        PlayParticle(ParticleType.Jump, position);
    }

    public void PlayDoubleJumpEffect(Vector3 position)
    {
        PlayParticle(ParticleType.DoubleJump, position);
    }

    public void PlayLandingEffect(Vector3 position)
    {
        PlayParticle(ParticleType.Landing, position);
    }

    public void PlayDeathEffect(Vector3 position)
    {
        PlayParticle(ParticleType.Death, position);
    }

    public void PlayCollectCoinEffect(Vector3 position)
    {
        PlayParticle(ParticleType.CollectCoin, position);
    }

    public void PlayExplosionEffect(Vector3 position)
    {
        PlayParticle(ParticleType.Explosion, position);
    }

    #endregion
}