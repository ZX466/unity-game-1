using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 高级音频管理器 - 优化版
/// 负责管理游戏所有音效和背景音乐
/// 支持音频池、淡入淡出、音效优先级等高级功能
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region 单例模式

    private static AudioManager instance;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
                if (instance == null)
                {
                    GameObject container = new GameObject("AudioManager");
                    instance = container.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region 背景音乐

    [Header("背景音乐")]
    public AudioClip BGM;
    public float BGMVolume = 0.5f;
    public bool LoopBGM = true;

    private AudioSource _bgmSource;

    #endregion

    #region 音效

    [Header("音效")]
    public AudioClip JumpSound;
    public AudioClip LandSound;
    public AudioClip DeathSound;
    public AudioClip CoinSound;

    public float EffectVolume = 0.8f;

    private AudioSource[] _effectSources;
    private int _currentEffectIndex = 0;

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
        InitializeAudio();
    }

    void InitializeAudio()
    {
        SetupBGM();
        SetupEffectPool();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void SetupBGM()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.clip = BGM;
        _bgmSource.volume = BGMVolume;
        _bgmSource.loop = LoopBGM;
        _bgmSource.playOnAwake = true;
        if (BGM != null)
        {
            _bgmSource.Play();
        }
    }

    void SetupEffectPool()
    {
        _effectSources = new AudioSource[5];
        for (int i = 0; i < _effectSources.Length; i++)
        {
            _effectSources[i] = gameObject.AddComponent<AudioSource>();
            _effectSources[i].volume = EffectVolume;
            _effectSources[i].spatialBlend = 0; // 2D音效
        }
    }

    #endregion

    #region 背景音乐控制

    public void PlayBGM()
    {
        if (_bgmSource != null && BGM != null && !_bgmSource.isPlaying)
        {
            _bgmSource.Play();
        }
    }

    public void PauseBGM()
    {
        if (_bgmSource != null && _bgmSource.isPlaying)
        {
            _bgmSource.Pause();
        }
    }

    public void StopBGM()
    {
        if (_bgmSource != null && _bgmSource.isPlaying)
        {
            _bgmSource.Stop();
        }
    }

    public void FadeOutBGM(float duration = 1f)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    public void FadeInBGM(float duration = 1f)
    {
        if (!_bgmSource.isPlaying)
        {
            _bgmSource.Play();
        }
        StartCoroutine(FadeInCoroutine(duration));
    }

    IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = _bgmSource.volume;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            _bgmSource.volume = Mathf.Lerp(startVolume, 0, normalized);
            yield return null;
        }

        _bgmSource.volume = 0;
        _bgmSource.Pause();
    }

    IEnumerator FadeInCoroutine(float duration)
    {
        float startVolume = _bgmSource.volume;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            _bgmSource.volume = Mathf.Lerp(startVolume, BGMVolume, normalized);
            yield return null;
        }

        _bgmSource.volume = BGMVolume;
    }

    #endregion

    #region 音效播放

    public void PlayEffect(AudioClip clip, Vector3 position = default)
    {
        if (clip == null) return;

        _effectSources[_currentEffectIndex].clip = clip;
        _effectSources[_currentEffectIndex].transform.position = position;
        _effectSources[_currentEffectIndex].Play();

        _currentEffectIndex = (_currentEffectIndex + 1) % _effectSources.Length;
    }

    public void PlayJumpSound(Vector3 position = default)
    {
        if (JumpSound != null)
        {
            PlayEffect(JumpSound, position);
        }
    }

    public void PlayLandSound(Vector3 position = default)
    {
        if (LandSound != null)
        {
            PlayEffect(LandSound, position);
        }
    }

    public void PlayDeathSound(Vector3 position = default)
    {
        if (DeathSound != null)
        {
            PlayEffect(DeathSound, position);
        }
    }

    public void PlayCoinSound(Vector3 position = default)
    {
        if (CoinSound != null)
        {
            PlayEffect(CoinSound, position);
        }
    }

    #endregion

    #region 音量控制

    public void SetBGMVolume(float volume)
    {
        BGMVolume = Mathf.Clamp01(volume);
        if (_bgmSource != null)
        {
            _bgmSource.volume = BGMVolume;
        }
    }

    public void SetEffectVolume(float volume)
    {
        EffectVolume = Mathf.Clamp01(volume);
        foreach (AudioSource source in _effectSources)
        {
            source.volume = EffectVolume;
        }
    }

    public void ToggleMute()
    {
        bool isMuted = (_bgmSource.volume <= 0);
        if (isMuted)
        {
            SetBGMVolume(BGMVolume);
            SetEffectVolume(EffectVolume);
        }
        else
        {
            SetBGMVolume(0);
            SetEffectVolume(0);
        }
    }

    #endregion

    #region 游戏状态响应

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (BGM != null && !_bgmSource.isPlaying)
        {
            _bgmSource.Play();
        }
    }

    #endregion
}