using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

/// <summary>
/// �ΰ��Ϳ� ����ڸ� ���� ���� ���ļ� ���� ����� �Ŵ���
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // AudioManager.cs ���� �ڵ忡 ���� �κе��� �߰�

    [Header("SoundManager ���� ���")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup voiceMixerGroup;

    [Header("3D ���� ����")]
    [SerializeField] private bool enable3DSound = false;
    [SerializeField] private float dopplerLevel = 1.0f;
    [SerializeField] private float soundSpeed = 343.3f;

    // SoundManager���� �̰��� ��ɵ�
    private Dictionary<string, AudioClip> preloadedClips;
    private Queue<AudioSource> audioSourcePool;
    private int poolSize = 10;

    [Header("����� �ҽ� ����")]
    [SerializeField] private AudioSource characterVoiceSource;
    [SerializeField] private AudioSource gameAudioSource;
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("���ļ� ���� ����")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float pitchAdjustmentMultiplier = 1.0f;
    [SerializeField] private float defaultMaxFrequency = 20000f;
    private float userMaxFrequency = 20000f;

    [Header("���� ����")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float voiceVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float gameAudioVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float backgroundMusicVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f;

    [Header("����� ĳ�� ����")]
    [SerializeField] private int maxCacheSize = 100;
    private Dictionary<string, AudioClip> audioClipCache;
    private Queue<string> cacheKeys;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
            InitializeSoundManagerFeatures(); // �� ���� �߰�
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioManager()
    {
        audioClipCache = new Dictionary<string, AudioClip>();
        cacheKeys = new Queue<string>();

        LoadUserAudioSettings();
        SetupAudioSources();
    }

    private void LoadUserAudioSettings()
    {
        // DataManager���� ������� ����� ���� �ε�
        if (DataManager.Instance != null && DataManager.Instance.gameData != null)
        {
            if (DataManager.Instance.gameData.isCalibrated)
            {
                userMaxFrequency = DataManager.Instance.gameData.audibleFrequencyMax;
            }

            var globalSettings = DataManager.Instance.gameData.globalSettings;
            if (globalSettings != null)
            {
                masterVolume = globalSettings.masterVolume;
                backgroundMusicVolume = globalSettings.bgmVolume;
                sfxVolume = globalSettings.sfxVolume;
            }
        }
    }

    private void SetupAudioSources()
    {
        // �� ����� �ҽ��� �⺻ ����
        if (characterVoiceSource != null)
        {
            characterVoiceSource.playOnAwake = false;
            characterVoiceSource.volume = voiceVolume * masterVolume;
        }

        if (gameAudioSource != null)
        {
            gameAudioSource.playOnAwake = false;
            gameAudioSource.volume = gameAudioVolume * masterVolume;
        }

        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.volume = backgroundMusicVolume * masterVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume * masterVolume;
        }
    }

    /// <summary>
    /// ĳ���� ��Ʈ�� ����մϴ�.
    /// </summary>
    public void PlayCharacterVoice(string audioClipName)
    {
        if (string.IsNullOrEmpty(audioClipName) || characterVoiceSource == null)
            return;

        AudioClip clip = LoadAudioClip($"Audio/Character/{audioClipName}");
        if (clip != null)
        {
            characterVoiceSource.clip = clip;
            characterVoiceSource.pitch = CalculatePitchAdjustment(440f); // �⺻ ���� ���ļ�
            characterVoiceSource.Play();
        }
    }

    /// <summary>
    /// ���� ������� ����մϴ� (���ļ� ���� ����).
    /// </summary>
    public void PlayGameAudio(string audioClipName, float originalFrequency = 440f)
    {
        if (string.IsNullOrEmpty(audioClipName) || gameAudioSource == null)
            return;

        string adjustedClipName = GetFrequencyAdjustedClipName(audioClipName, originalFrequency);
        AudioClip clip = LoadGameAudioClip(adjustedClipName);

        if (clip != null)
        {
            gameAudioSource.clip = clip;
            gameAudioSource.pitch = CalculatePitchAdjustment(originalFrequency);
            gameAudioSource.volume = gameAudioVolume * masterVolume;
            gameAudioSource.Play();
        }
    }

    /// <summary>
    /// ��������� ����մϴ�.
    /// </summary>
    public void PlayBackgroundMusic(string musicName)
    {
        if (string.IsNullOrEmpty(musicName) || backgroundMusicSource == null)
            return;

        AudioClip clip = LoadAudioClip($"Audio/BGM/{musicName}");
        if (clip != null)
        {
            backgroundMusicSource.clip = clip;
            backgroundMusicSource.volume = backgroundMusicVolume * masterVolume;
            backgroundMusicSource.Play();
        }
    }

    /// <summary>
    /// ȿ������ ����մϴ�.
    /// </summary>
    public void PlaySFX(string sfxName)
    {
        if (string.IsNullOrEmpty(sfxName) || sfxSource == null)
            return;

        AudioClip clip = LoadAudioClip($"Audio/SFX/{sfxName}");
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    /// <summary>
    /// ���������� ������� ����մϴ� (���� ������).
    /// </summary>
    public void PlaySequentialAudio(string[] audioClipNames, float[] frequencies, float interval = 1.0f, System.Action<int> onEachPlay = null, System.Action onComplete = null)
    {
        StartCoroutine(PlaySequentialAudioCoroutine(audioClipNames, frequencies, interval, onEachPlay, onComplete));
    }

    private IEnumerator PlaySequentialAudioCoroutine(string[] audioClipNames, float[] frequencies, float interval, System.Action<int> onEachPlay, System.Action onComplete)
    {
        for (int i = 0; i < audioClipNames.Length; i++)
        {
            PlayGameAudio(audioClipNames[i], frequencies[i]);
            onEachPlay?.Invoke(i);

            yield return new WaitForSeconds(interval);
        }

        onComplete?.Invoke();
    }

    private AudioClip LoadGameAudioClip(string clipName)
    {
        // ���� ��ο��� ����� Ŭ�� �˻�
        string[] searchPaths = {
            $"Audio/Notes/{clipName}",
            $"Audio/Instruments/Strings/{clipName}",
            $"Audio/Instruments/Winds/{clipName}",
            $"Audio/Instruments/Brass/{clipName}",
            $"Audio/Instruments/Percussion/{clipName}",
            $"Audio/Instruments/Traditional/{clipName}",
            $"Audio/Rhythm/{clipName}",
            $"Audio/Melody/{clipName}"
        };

        foreach (string path in searchPaths)
        {
            AudioClip clip = LoadAudioClip(path);
            if (clip != null)
            {
                return clip;
            }
        }

        Debug.LogWarning($"����� Ŭ���� ã�� �� �����ϴ�: {clipName}");
        return null;
    }

    private AudioClip LoadAudioClip(string path)
    {
        // ĳ�ÿ��� ���� Ȯ��
        if (audioClipCache.ContainsKey(path))
        {
            return audioClipCache[path];
        }

        // Resources���� �ε�
        AudioClip clip = Resources.Load<AudioClip>(path);

        if (clip != null)
        {
            // ĳ�ÿ� �߰�
            AddToCache(path, clip);
        }

        return clip;
    }

    private void AddToCache(string path, AudioClip clip)
    {
        // ĳ�� ũ�� ����
        if (audioClipCache.Count >= maxCacheSize)
        {
            string oldestKey = cacheKeys.Dequeue();
            audioClipCache.Remove(oldestKey);
        }

        audioClipCache[path] = clip;
        cacheKeys.Enqueue(path);
    }


    // AudioManager Awake()�� �߰��� �ڵ�
    private void InitializeSoundManagerFeatures()
    {
        preloadedClips = new Dictionary<string, AudioClip>();
        audioSourcePool = new Queue<AudioSource>();
        
        // ����� �ҽ� Ǯ ����
        for (int i = 0; i < poolSize; i++)
        {
            GameObject audioObj = new GameObject($"PooledAudioSource_{i}");
            audioObj.transform.SetParent(transform);
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Enqueue(source);
        }

        // 3D ���� ����
        if (enable3DSound)
        {
            AudioSettings.dopplerLevel = dopplerLevel;
            AudioSettings.speedOfSound = soundSpeed;
        }
    }

    /// <summary>
    /// ����� Ŭ���� �̸� �ε��մϴ� (SoundManager ���)
    /// </summary>
    public void PreloadAudioClip(string clipName, string path)
    {
        if (!preloadedClips.ContainsKey(clipName))
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                preloadedClips[clipName] = clip;
                Debug.Log($"����� Ŭ�� �̸� �ε�: {clipName}");
            }
        }
    }

    /// <summary>
    /// ���� ����� Ŭ���� ��ġ�� �̸� �ε��մϴ�
    /// </summary>
    public void PreloadAudioClips(string[] clipNames, string[] paths)
    {
        for (int i = 0; i < clipNames.Length && i < paths.Length; i++)
        {
            PreloadAudioClip(clipNames[i], paths[i]);
        }
    }

    /// <summary>
    /// Ǯ���� ����� �ҽ��� �����ɴϴ�
    /// </summary>
    private AudioSource GetPooledAudioSource()
    {
        if (audioSourcePool.Count > 0)
        {
            return audioSourcePool.Dequeue();
        }
        
        // Ǯ�� ��������� ���� ����
        GameObject audioObj = new GameObject($"TempAudioSource_{Time.time}");
        audioObj.transform.SetParent(transform);
        return audioObj.AddComponent<AudioSource>();
    }

    /// <summary>
    /// ����� �ҽ��� Ǯ�� ��ȯ�մϴ�
    /// </summary>
    private void ReturnToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        audioSourcePool.Enqueue(source);
    }

    /// <summary>
    /// 3D ��ġ���� ȿ������ ����մϴ�
    /// </summary>
    public void PlaySFXAtPosition(string sfxName, Vector3 position, float volume = 1.0f)
    {
        AudioSource source = GetPooledAudioSource();
        source.transform.position = position;
        
        AudioClip clip = GetAudioClip(sfxName);
        if (clip != null)
        {
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.spatialBlend = enable3DSound ? 1.0f : 0.0f;
            source.Play();
            
            StartCoroutine(ReturnSourceAfterPlayback(source, clip.length));
        }
    }

    /// <summary>
    /// ����� Ŭ�� ��� �� �ҽ��� Ǯ�� ��ȯ�ϴ� �ڷ�ƾ
    /// </summary>
    private IEnumerator ReturnSourceAfterPlayback(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        ReturnToPool(source);
    }

    /// <summary>
    /// ����� Ŭ���� �����ɴϴ� (ĳ�� �� �̸� �ε�� Ŭ�� �켱)
    /// </summary>
    private AudioClip GetAudioClip(string clipName)
    {
        // �̸� �ε�� Ŭ�� Ȯ��
        if (preloadedClips.ContainsKey(clipName))
        {
            return preloadedClips[clipName];
        }
        
        // ���� LoadAudioClip �޼��� ���
        return LoadAudioClip($"Audio/SFX/{clipName}");
    }

    /// <summary>
    /// ��� �̸� �ε�� Ŭ���� �����մϴ�
    /// </summary>
    public void UnloadAllPreloadedClips()
    {
        foreach (var clip in preloadedClips.Values)
        {
            if (clip != null)
            {
                Resources.UnloadAsset(clip);
            }
        }
        preloadedClips.Clear();
        Debug.Log("��� �̸� �ε�� ����� Ŭ�� ���� �Ϸ�");
    }

    /// <summary>
    /// ����� �ͼ� �׷� ����
    /// </summary>
    public void SetMixerGroups()
    {
        if (characterVoiceSource != null && voiceMixerGroup != null)
            characterVoiceSource.outputAudioMixerGroup = voiceMixerGroup;
            
        if (gameAudioSource != null && sfxMixerGroup != null)
            gameAudioSource.outputAudioMixerGroup = sfxMixerGroup;
            
        if (backgroundMusicSource != null && musicMixerGroup != null)
            backgroundMusicSource.outputAudioMixerGroup = musicMixerGroup;
            
        if (sfxSource != null && sfxMixerGroup != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private string GetFrequencyAdjustedClipName(string originalName, float frequency)
    {
        float frequencyRatio = userMaxFrequency / defaultMaxFrequency;

        if (frequencyRatio < 0.4f)
        {
            return originalName + "_low";
        }
        else if (frequencyRatio < 0.8f)
        {
            return originalName + "_mid";
        }
        else
        {
            return originalName;
        }
    }

    private float CalculatePitchAdjustment(float originalFrequency)
    {
        if (userMaxFrequency >= defaultMaxFrequency)
        {
            return 1.0f; // ���� ��ġ
        }

        // ����� ��û ������ �°� ��ġ ����
        float targetFrequency = Mathf.Min(originalFrequency, userMaxFrequency * 0.8f);
        float pitchAdjustment = targetFrequency / originalFrequency;

        return Mathf.Clamp(pitchAdjustment * pitchAdjustmentMultiplier, 0.5f, 2.0f);
    }

    /// <summary>
    /// ��� ������� �����մϴ�.
    /// </summary>
    public void StopAllAudio()
    {
        characterVoiceSource?.Stop();
        gameAudioSource?.Stop();
        backgroundMusicSource?.Stop();
        sfxSource?.Stop();
    }

    /// <summary>
    /// ������Ǹ� �����մϴ�.
    /// </summary>
    public void StopBackgroundMusic()
    {
        backgroundMusicSource?.Stop();
    }

    /// <summary>
    /// ������ ������ �����մϴ�.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// ���� ����� ������ �����մϴ�.
    /// </summary>
    public void SetGameAudioVolume(float volume)
    {
        gameAudioVolume = Mathf.Clamp01(volume);
        if (gameAudioSource != null)
        {
            gameAudioSource.volume = gameAudioVolume * masterVolume;
        }
    }

    /// <summary>
    /// ������� ������ �����մϴ�.
    /// </summary>
    public void SetBackgroundMusicVolume(float volume)
    {
        backgroundMusicVolume = Mathf.Clamp01(volume);
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = backgroundMusicVolume * masterVolume;
        }
    }

    /// <summary>
    /// ����� ��û ���ļ��� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateUserFrequencyRange(float maxFrequency)
    {
        userMaxFrequency = maxFrequency;
        Debug.Log($"����� ��û ���ļ� ������Ʈ: {maxFrequency}Hz");
    }

    private void UpdateAllVolumes()
    {
        if (characterVoiceSource != null)
            characterVoiceSource.volume = voiceVolume * masterVolume;

        if (gameAudioSource != null)
            gameAudioSource.volume = gameAudioVolume * masterVolume;

        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = backgroundMusicVolume * masterVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume * masterVolume;
    }

    /// <summary>
    /// ����� ������ �����մϴ�.
    /// </summary>
    public void SaveAudioSettings()
    {
        if (DataManager.Instance != null && DataManager.Instance.gameData != null)
        {
            var globalSettings = DataManager.Instance.gameData.globalSettings;
            if (globalSettings != null)
            {
                globalSettings.masterVolume = masterVolume;
                globalSettings.bgmVolume = backgroundMusicVolume;
                globalSettings.sfxVolume = sfxVolume;

                DataManager.Instance.UpdateGlobalSettings(globalSettings);
            }
        }
    }

    /// <summary>
    /// ����� ĳ�ø� �����մϴ�.
    /// </summary>
    public void ClearAudioCache()
    {
        audioClipCache.Clear();
        cacheKeys.Clear();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // ���� �Ͻ������� �� ������� ���� ���̱�
            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.volume = backgroundMusicVolume * masterVolume * 0.3f;
            }
        }
        else
        {
            // ���� �ٽ� Ȱ��ȭ�� �� ���� ����
            UpdateAllVolumes();
        }
    }
}
