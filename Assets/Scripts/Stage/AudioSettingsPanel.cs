using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ����� ���� UI �г�
/// </summary>
public class AudioSettingsPanel : MonoBehaviour
{
    [Header("UI ������Ʈ")]
    [SerializeField] private GameObject settingsPanelRoot;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button saveSettingsButton;

    [Header("���� �����̴�")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider gameAudioVolumeSlider;
    [SerializeField] private Slider backgroundMusicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("���� �ؽ�Ʈ")]
    [SerializeField] private Text masterVolumeText;
    [SerializeField] private Text gameAudioVolumeText;
    [SerializeField] private Text backgroundMusicVolumeText;
    [SerializeField] private Text sfxVolumeText;

    [Header("���ļ� ����")]
    [SerializeField] private Slider frequencySlider;
    [SerializeField] private Text frequencyText;
    [SerializeField] private Button testFrequencyButton;

    [Header("��Ÿ ����")]
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

        // ���� ���� �ε�
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

        // ���ļ� ���� �ε�
        if (frequencySlider != null && DataManager.Instance.gameData.isCalibrated)
        {
            frequencySlider.value = DataManager.Instance.gameData.audibleFrequencyMax;
        }

        // ��Ÿ ���� �ε�
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

        // ���� ���� ����
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

        // ��Ÿ ���� ����
        if (hapticToggle != null)
        {
            settings.hapticEnabled = hapticToggle.isOn;
        }

        // ���ļ� ���� ����
        if (frequencySlider != null)
        {
            DataManager.Instance.UpdateCalibrationData(frequencySlider.value);
        }

        // ���� ����
        DataManager.Instance.UpdateGlobalSettings(settings);
        AudioManager.Instance?.SaveAudioSettings();

        Debug.Log("����� ������ ����Ǿ����ϴ�.");

        // ���� �Ϸ� �˸� (���û���)
        ShowSaveConfirmation();
    }

    private void ShowSaveConfirmation()
    {
        // TODO: ���� �Ϸ� �˸� UI ǥ��
        Debug.Log("������ ����Ǿ����ϴ�!");
    }
}
