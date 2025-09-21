using UnityEngine;
using UnityEngine.Audio; // AudioMixer를 사용하기 위해 필요

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer mainMixer; // Unity 에디터에서 연결할 오디오 믹서
    public AudioMixerGroup bgmGroup; // BGM을 출력할 믹서 그룹
    public AudioMixerGroup sfxGroup; // SFX를 출력할 믹서 그룹

    private AudioSource bgmPlayer;
    private AudioSource sfxPlayer;

    [Header("Volume Settings")]
    [Range(0.0001f, 1.0f)]
    public float bgmVolume = 1.0f;
    [Range(0.0001f, 1.0f)]
    public float sfxVolume = 1.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource 컴포넌트 초기화 및 믹서 그룹 할당
            bgmPlayer = gameObject.AddComponent<AudioSource>();
            sfxPlayer = gameObject.AddComponent<AudioSource>();

            bgmPlayer.outputAudioMixerGroup = bgmGroup;
            sfxPlayer.outputAudioMixerGroup = sfxGroup;

            bgmPlayer.loop = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 배경음악 재생
    public void PlayBGM(AudioClip clip)
    {
        if (bgmPlayer.clip == clip && bgmPlayer.isPlaying) return;
        bgmPlayer.clip = clip;
        bgmPlayer.Play();
    }

    // 효과음 재생 (사용자 캘리브레이션 기반 EQ 조절 포함)
    public void PlaySFX(AudioClip clip)
    {
        AdjustSfxEqualizer();
        sfxPlayer.PlayOneShot(clip);
    }

    // 사용자 데이터에 따라 SFX 이퀄라이저를 조절
    private void AdjustSfxEqualizer()
    {
        if (DataManager.Instance == null || !DataManager.Instance.gameData.isCalibrated)
        {
            // 캘리브레이션 데이터가 없으면 EQ를 기본값으로 둡니다.
            mainMixer.SetFloat("SFX_LowGain", 0f);
            mainMixer.SetFloat("SFX_MidGain", 0f);
            mainMixer.SetFloat("SFX_HighGain", 0f);
            return;
        }

        float userMaxFreq = DataManager.Instance.gameData.audibleFrequencyMax;

        // 사용자가 잘 듣는 주파수 대역을 증폭시킵니다.
        // 이 값(예: 4000, 12000)은 테스트를 통해 조절해야 합니다.
        if (userMaxFreq < 4000f) // 저주파 대역을 잘 듣는 경우
        {
            mainMixer.SetFloat("SFX_LowGain", 5f); // 저음역대 5dB 증폭
            mainMixer.SetFloat("SFX_MidGain", 0f);
            mainMixer.SetFloat("SFX_HighGain", -5f); // 고음역대 감쇠
        }
        else if (userMaxFreq < 12000f) // 중주파 대역을 잘 듣는 경우
        {
            mainMixer.SetFloat("SFX_LowGain", -5f);
            mainMixer.SetFloat("SFX_MidGain", 5f);
            mainMixer.SetFloat("SFX_HighGain", -5f);
        }
        else // 고주파 대역을 잘 듣는 경우
        {
            mainMixer.SetFloat("SFX_LowGain", -5f);
            mainMixer.SetFloat("SFX_MidGain", 0f);
            mainMixer.SetFloat("SFX_HighGain", 5f);
        }
    }

    // 볼륨 조절 (데시벨 단위로 변환하여 믹서에 전달)
    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp(volume, 0.0001f, 1.0f);
        mainMixer.SetFloat("BGM_Volume", Mathf.Log10(bgmVolume) * 20);
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp(volume, 0.0001f, 1.0f);
        mainMixer.SetFloat("SFX_Volume", Mathf.Log10(sfxVolume) * 20);
    }
}

