using UnityEngine;

/// <summary>
/// 一覧に登録されたBGM・効果音を再生する。
/// 作成者 : 酒井康佑
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("音の一覧")]
    [SerializeField] private SoundTableData _soundTable;

    [Header("音の再生元")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _seSource;

    [Header("全体の音量")]
    [SerializeField, Range(0f, 1f)]
    private float _bgmVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float _seVolume = 1f;

    // 現在のBGMに設定された個別音量
    private float _currentBgmVolume = 1f;

    // 倍速音だけを途中停止できる専用の再生元。
    private AudioSource _speedSource;
    private float _speedClipVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;

        _seSource.playOnAwake = false;
        _seSource.loop = false;
        _seSource.spatialBlend = 0f;

        ApplyVolumes();
    }

    /// <summary>
    /// 一覧からIDを指定してBGMを再生する。
    /// </summary>
    public void PlayBGMById(SoundId id)
    {
        if (!TryGetSound(id, out var sound))
            return;

        StartBGM(sound.clip, sound.volume);
    }

    /// <summary>
    /// 一覧からIDを指定して効果音を再生する。
    /// 個別音量を適用して、ほかの効果音と重ねて鳴らす。
    /// </summary>
    public void PlaySEById(SoundId id)
    {
        if (!TryGetSound(id, out var sound))
            return;

        _seSource.PlayOneShot(sound.clip, sound.volume);
    }

    /// <summary>倍速音を専用のAudioSourceで一度再生する。</summary>
    public void PlaySpeedSE()
    {
        if (!TryGetSound(SoundId.DoubleSpeed, out var sound))
            return;

        if (_speedSource == null)
        {
            GameObject sourceObject = new GameObject("DoubleSpeedSource");
            sourceObject.transform.SetParent(transform, false);
            _speedSource = sourceObject.AddComponent<AudioSource>();
            _speedSource.playOnAwake = false;
            _speedSource.loop = false;
            _speedSource.spatialBlend = 0f;
        }

        // 通常SEと同じMixerへ出力し、一覧の個別音量も反映する。
        _speedSource.outputAudioMixerGroup = _seSource.outputAudioMixerGroup;
        _speedClipVolume = sound.volume;
        _speedSource.Stop();
        _speedSource.clip = sound.clip;
        ApplyVolumes();
        _speedSource.Play();
    }

    /// <summary>ほかの効果音に影響を与えず、倍速音だけを停止する。</summary>
    public void StopSpeedSE()
    {
        if (_speedSource != null)
        {
            _speedSource.Stop();
            _speedSource.clip = null;
        }
    }

    // 既存のAudioClip直接指定にも対応する
    public void PlayBGM(AudioClip clip)
    {
        StartBGM(clip, 1f);
    }

    public void PlaySE(AudioClip clip)
    {
        if (clip != null)
            _seSource.PlayOneShot(clip);
    }

    private void StartBGM(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        _currentBgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();

        // 同じBGMなら再生位置を戻さない
        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
            return;

        _bgmSource.Stop();
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSEVolume(float volume)
    {
        _seVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        // BGM全体の音量 × 一覧の個別音量
        if (_bgmSource != null)
            _bgmSource.volume = _bgmVolume * _currentBgmVolume;

        // 効果音の個別音量はPlayOneShotで適用する
        if (_seSource != null)
            _seSource.volume = _seVolume;

        if (_speedSource != null)
            _speedSource.volume = _seVolume * _speedClipVolume;
    }

    private bool TryGetSound(SoundId id, out SoundTableData.SoundEntry sound)
    {
        sound = null;

        if (_soundTable == null)
        {
            Debug.LogWarning("Sound Tableが未設定です。", this);
            return false;
        }

        // 一覧から取得でき、音声も設定されている場合だけ成功を返す。
        if (!_soundTable.TryGetSound(id, out sound)
            || sound == null || sound.clip == null)
        {
            Debug.LogWarning($"音声が未登録です：{id}", this);
            return false;
        }

        return true;
    }

    private void OnValidate()
    {
        // Inspectorで全体音量を変更したときにも反映する
        ApplyVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
