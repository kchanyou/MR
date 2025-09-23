using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

/// <summary>
/// 인공와우 사용자를 위한 향상된 주파수 조절 오디오 매니저
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // AudioManager.cs 기존 코드에 다음 부분들을 추가

    [Header("SoundManager 통합 기능")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup voiceMixerGroup;

    [Header("3D 사운드 설정")]
    [SerializeField] private bool enable3DSound = false;
    [SerializeField] private float dopplerLevel = 1.0f;
    [SerializeField] private float soundSpeed = 343.3f;

    // SoundManager에서 이관된 기능들
    private Dictionary<string, AudioClip> preloadedClips;
    private Queue<AudioSource> audioSourcePool;
    private int poolSize = 10;

    [Header("오디오 소스 설정")]
    [SerializeField] private AudioSource characterVoiceSource;
    [SerializeField] private AudioSource gameAudioSource;
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("주파수 조절 설정")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float pitchAdjustmentMultiplier = 1.0f;
    [SerializeField] private float defaultMaxFrequency = 20000f;
    private float userMaxFrequency = 20000f;

    [Header("볼륨 설정")]
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

    [Header("오디오 캐시 설정")]
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
            InitializeSoundManagerFeatures(); // 이 라인 추가
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
        // DataManager에서 사용자의 오디오 설정 로드
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
        // 각 오디오 소스의 기본 설정
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
    /// 캐릭터 멘트를 재생합니다.
    /// </summary>
    public void PlayCharacterVoice(string audioClipName)
    {
        if (string.IsNullOrEmpty(audioClipName) || characterVoiceSource == null)
            return;

        AudioClip clip = LoadAudioClip($"Audio/Character/{audioClipName}");
        if (clip != null)
        {
            characterVoiceSource.clip = clip;
            characterVoiceSource.pitch = CalculatePitchAdjustment(440f); // 기본 음성 주파수
            characterVoiceSource.Play();
        }
    }

    /// <summary>
    /// 게임 오디오를 재생합니다 (주파수 조절 적용).
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
    /// 배경음악을 재생합니다.
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
    /// 효과음을 재생합니다.
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
    /// 순차적으로 오디오를 재생합니다 (게임 로직용).
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
        // 여러 경로에서 오디오 클립 검색
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

        Debug.LogWarning($"오디오 클립을 찾을 수 없습니다: {clipName}");
        return null;
    }

    private AudioClip LoadAudioClip(string path)
    {
        // 캐시에서 먼저 확인
        if (audioClipCache.ContainsKey(path))
        {
            return audioClipCache[path];
        }

        // Resources에서 로드
        AudioClip clip = Resources.Load<AudioClip>(path);

        if (clip != null)
        {
            // 캐시에 추가
            AddToCache(path, clip);
        }

        return clip;
    }

    private void AddToCache(string path, AudioClip clip)
    {
        // 캐시 크기 제한
        if (audioClipCache.Count >= maxCacheSize)
        {
            string oldestKey = cacheKeys.Dequeue();
            audioClipCache.Remove(oldestKey);
        }

        audioClipCache[path] = clip;
        cacheKeys.Enqueue(path);
    }


    // AudioManager Awake()에 추가할 코드
    private void InitializeSoundManagerFeatures()
    {
        preloadedClips = new Dictionary<string, AudioClip>();
        audioSourcePool = new Queue<AudioSource>();
        
        // 오디오 소스 풀 생성
        for (int i = 0; i < poolSize; i++)
        {
            GameObject audioObj = new GameObject($"PooledAudioSource_{i}");
            audioObj.transform.SetParent(transform);
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSourcePool.Enqueue(source);
        }

        // 3D 사운드 설정
        if (enable3DSound)
        {
            AudioSettings.dopplerLevel = dopplerLevel;
            AudioSettings.speedOfSound = soundSpeed;
        }
    }

    /// <summary>
    /// 오디오 클립을 미리 로드합니다 (SoundManager 기능)
    /// </summary>
    public void PreloadAudioClip(string clipName, string path)
    {
        if (!preloadedClips.ContainsKey(clipName))
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip != null)
            {
                preloadedClips[clipName] = clip;
                Debug.Log($"오디오 클립 미리 로드: {clipName}");
            }
        }
    }

    /// <summary>
    /// 여러 오디오 클립을 배치로 미리 로드합니다
    /// </summary>
    public void PreloadAudioClips(string[] clipNames, string[] paths)
    {
        for (int i = 0; i < clipNames.Length && i < paths.Length; i++)
        {
            PreloadAudioClip(clipNames[i], paths[i]);
        }
    }

    /// <summary>
    /// 풀에서 오디오 소스를 가져옵니다
    /// </summary>
    private AudioSource GetPooledAudioSource()
    {
        if (audioSourcePool.Count > 0)
        {
            return audioSourcePool.Dequeue();
        }
        
        // 풀이 비어있으면 새로 생성
        GameObject audioObj = new GameObject($"TempAudioSource_{Time.time}");
        audioObj.transform.SetParent(transform);
        return audioObj.AddComponent<AudioSource>();
    }

    /// <summary>
    /// 오디오 소스를 풀로 반환합니다
    /// </summary>
    private void ReturnToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        audioSourcePool.Enqueue(source);
    }

    /// <summary>
    /// 3D 위치에서 효과음을 재생합니다
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
    /// 오디오 클립 재생 후 소스를 풀로 반환하는 코루틴
    /// </summary>
    private IEnumerator ReturnSourceAfterPlayback(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        ReturnToPool(source);
    }

    /// <summary>
    /// 오디오 클립을 가져옵니다 (캐시 및 미리 로드된 클립 우선)
    /// </summary>
    private AudioClip GetAudioClip(string clipName)
    {
        // 미리 로드된 클립 확인
        if (preloadedClips.ContainsKey(clipName))
        {
            return preloadedClips[clipName];
        }
        
        // 기존 LoadAudioClip 메서드 사용
        return LoadAudioClip($"Audio/SFX/{clipName}");
    }

    /// <summary>
    /// 모든 미리 로드된 클립을 해제합니다
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
        Debug.Log("모든 미리 로드된 오디오 클립 해제 완료");
    }

    /// <summary>
    /// 오디오 믹서 그룹 설정
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
            return 1.0f; // 정상 피치
        }

        // 사용자 가청 범위에 맞게 피치 조절
        float targetFrequency = Mathf.Min(originalFrequency, userMaxFrequency * 0.8f);
        float pitchAdjustment = targetFrequency / originalFrequency;

        return Mathf.Clamp(pitchAdjustment * pitchAdjustmentMultiplier, 0.5f, 2.0f);
    }

    /// <summary>
    /// 모든 오디오를 정지합니다.
    /// </summary>
    public void StopAllAudio()
    {
        characterVoiceSource?.Stop();
        gameAudioSource?.Stop();
        backgroundMusicSource?.Stop();
        sfxSource?.Stop();
    }

    /// <summary>
    /// 배경음악만 정지합니다.
    /// </summary>
    public void StopBackgroundMusic()
    {
        backgroundMusicSource?.Stop();
    }

    /// <summary>
    /// 마스터 볼륨을 설정합니다.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// 게임 오디오 볼륨을 설정합니다.
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
    /// 배경음악 볼륨을 설정합니다.
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
    /// 사용자 가청 주파수를 업데이트합니다.
    /// </summary>
    public void UpdateUserFrequencyRange(float maxFrequency)
    {
        userMaxFrequency = maxFrequency;
        Debug.Log($"사용자 가청 주파수 업데이트: {maxFrequency}Hz");
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
    /// 오디오 설정을 저장합니다.
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
    /// 오디오 캐시를 정리합니다.
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
            // 앱이 일시정지될 때 배경음악 볼륨 줄이기
            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.volume = backgroundMusicVolume * masterVolume * 0.3f;
            }
        }
        else
        {
            // 앱이 다시 활성화될 때 볼륨 복원
            UpdateAllVolumes();
        }
    }
}
