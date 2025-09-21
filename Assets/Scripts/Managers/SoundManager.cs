using UnityEngine;
using UnityEngine.Audio; // AudioMixer�� ����ϱ� ���� �ʿ�

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer mainMixer; // Unity �����Ϳ��� ������ ����� �ͼ�
    public AudioMixerGroup bgmGroup; // BGM�� ����� �ͼ� �׷�
    public AudioMixerGroup sfxGroup; // SFX�� ����� �ͼ� �׷�

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

            // AudioSource ������Ʈ �ʱ�ȭ �� �ͼ� �׷� �Ҵ�
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

    // ������� ���
    public void PlayBGM(AudioClip clip)
    {
        if (bgmPlayer.clip == clip && bgmPlayer.isPlaying) return;
        bgmPlayer.clip = clip;
        bgmPlayer.Play();
    }

    // ȿ���� ��� (����� Ķ���극�̼� ��� EQ ���� ����)
    public void PlaySFX(AudioClip clip)
    {
        AdjustSfxEqualizer();
        sfxPlayer.PlayOneShot(clip);
    }

    // ����� �����Ϳ� ���� SFX ������������ ����
    private void AdjustSfxEqualizer()
    {
        if (DataManager.Instance == null || !DataManager.Instance.gameData.isCalibrated)
        {
            // Ķ���극�̼� �����Ͱ� ������ EQ�� �⺻������ �Ӵϴ�.
            mainMixer.SetFloat("SFX_LowGain", 0f);
            mainMixer.SetFloat("SFX_MidGain", 0f);
            mainMixer.SetFloat("SFX_HighGain", 0f);
            return;
        }

        float userMaxFreq = DataManager.Instance.gameData.audibleFrequencyMax;

        // ����ڰ� �� ��� ���ļ� �뿪�� ������ŵ�ϴ�.
        // �� ��(��: 4000, 12000)�� �׽�Ʈ�� ���� �����ؾ� �մϴ�.
        if (userMaxFreq < 4000f) // ������ �뿪�� �� ��� ���
        {
            mainMixer.SetFloat("SFX_LowGain", 5f); // �������� 5dB ����
            mainMixer.SetFloat("SFX_MidGain", 0f);
            mainMixer.SetFloat("SFX_HighGain", -5f); // �������� ����
        }
        else if (userMaxFreq < 12000f) // ������ �뿪�� �� ��� ���
        {
            mainMixer.SetFloat("SFX_LowGain", -5f);
            mainMixer.SetFloat("SFX_MidGain", 5f);
            mainMixer.SetFloat("SFX_HighGain", -5f);
        }
        else // ������ �뿪�� �� ��� ���
        {
            mainMixer.SetFloat("SFX_LowGain", -5f);
            mainMixer.SetFloat("SFX_MidGain", 0f);
            mainMixer.SetFloat("SFX_HighGain", 5f);
        }
    }

    // ���� ���� (���ú� ������ ��ȯ�Ͽ� �ͼ��� ����)
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

