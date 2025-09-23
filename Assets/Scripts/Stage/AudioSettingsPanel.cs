using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오디오 설정 UI 패널
/// </summary>
public class AudioSettingsPanel : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject settingsPanelRoot;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button saveSettingsButton;

    [Header("볼륨 슬라이더")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider gameAudioVolumeSlider;
    [SerializeField] private Slider backgroundMusicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("볼륨 텍스트")]
    [SerializeField] private Text masterVolumeText;
    [SerializeField] private Text gameAudioVolumeText;
    [SerializeField] private Text backgroundMusicVolumeText;
    [SerializeField] private Text sfxVolumeText;

    [Header("주파수 설정")]
    [SerializeField] private Slider frequencySlider;
    [SerializeField] private Text frequencyText;
    [SerializeField] private Button testFrequencyButton;

    [Header("기타 설정")]
    [SerializeField] private Toggle hapticToggle;
    [SerializeField] private Dropdown languageDropdown;

    private bool isPanelOpen = false;

    private void Awake()
    {
        SetupButtons();
        SetupSliders();

        if (settingsPanelRoot != null)
        {
            settingsPanelRoot.SetActive(false);
        }
    }

    private void Start()
    {
        LoadCurrentSettings();
    }

    private void SetupButtons()
    {
        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.AddListener(OpenSettingsPanel);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettingsPanel);
        }

        if (saveSettingsButton != null)
        {
            saveSettingsButton.onClick.AddListener(SaveSettings);
        }

        if (testFrequencyButton != null)
        {
            testFrequencyButton.onClick.AddListener(TestFrequency);
        }
    }

    private void SetupSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (gameAudioVolumeSlider != null)
        {
            gameAudioVolumeSlider.onValueChanged.AddListener(OnGameAudioVolumeChanged);
        }

        if (backgroundMusicVolumeSlider != null)
        {
            backgroundMusicVolumeSlider.onValueChanged.AddListener(OnBackgroundMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (frequencySlider != null)
        {
            frequencySlider.minValue = 8000f;
            frequencySlider.maxValue = 20000f;
            frequencySlider.onValueChanged.AddListener(OnFrequencyChanged);
        }
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanelRoot != null)
        {
            settingsPanelRoot.SetActive(true);
            isPanelOpen = true;
            LoadCurrentSettings();
        }
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanelRoot != null)
        {
            settingsPanelRoot.SetActive(false);
            isPanelOpen = false;
        }
    }

    private void LoadCurrentSettings()
    {
        if (DataManager.Instance?.gameData?.globalSettings == null)
            return;

        var settings = DataManager.Instance.gameData.globalSettings;

        // 볼륨 설정 로드
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = settings.masterVolume;
        }

        if (backgroundMusicVolumeSlider != null)
        {
            backgroundMusicVolumeSlider.value = settings.bgmVolume;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = settings.sfxVolume;
        }

        // 주파수 설정 로드
        if (frequencySlider != null && DataManager.Instance.gameData.isCalibrated)
        {
            frequencySlider.value = DataManager.Instance.gameData.audibleFrequencyMax;
        }

        // 기타 설정 로드
        if (hapticToggle != null)
        {
            hapticToggle.isOn = settings.hapticEnabled;
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }

        if (masterVolumeText != null)
        {
            masterVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnGameAudioVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetGameAudioVolume(value);
        }

        if (gameAudioVolumeText != null)
        {
            gameAudioVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnBackgroundMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBackgroundMusicVolume(value);
        }

        if (backgroundMusicVolumeText != null)
        {
            backgroundMusicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnFrequencyChanged(float value)
    {
        if (frequencyText != null)
        {
            frequencyText.text = $"{Mathf.RoundToInt(value)} Hz";
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateUserFrequencyRange(value);
        }
    }

    private void TestFrequency()
    {
        if (AudioManager.Instance != null)
        {
            float testFreq = frequencySlider != null ? frequencySlider.value : 1000f;
            AudioManager.Instance.PlayGameAudio("test_tone", testFreq);
        }
    }

    private void SaveSettings()
    {
        if (DataManager.Instance?.gameData?.globalSettings == null)
            return;

        var settings = DataManager.Instance.gameData.globalSettings;

        // 볼륨 설정 저장
        if (masterVolumeSlider != null)
        {
            settings.masterVolume = masterVolumeSlider.value;
        }

        if (backgroundMusicVolumeSlider != null)
        {
            settings.bgmVolume = backgroundMusicVolumeSlider.value;
        }

        if (sfxVolumeSlider != null)
        {
            settings.sfxVolume = sfxVolumeSlider.value;
        }

        // 기타 설정 저장
        if (hapticToggle != null)
        {
            settings.hapticEnabled = hapticToggle.isOn;
        }

        // 주파수 설정 저장
        if (frequencySlider != null)
        {
            DataManager.Instance.UpdateCalibrationData(frequencySlider.value);
        }

        // 설정 저장
        DataManager.Instance.UpdateGlobalSettings(settings);
        AudioManager.Instance?.SaveAudioSettings();

        Debug.Log("오디오 설정이 저장되었습니다.");

        // 저장 완료 알림 (선택사항)
        ShowSaveConfirmation();
    }

    private void ShowSaveConfirmation()
    {
        // TODO: 저장 완료 알림 UI 표시
        Debug.Log("설정이 저장되었습니다!");
    }
}
