using UnityEngine;

/// <summary>
/// ���� �ǵ�� �Ŵ��� - �ΰ��Ϳ� ����ڸ� ���� �˰� �ǵ��
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance;

    [Header("���� ����")]
    [SerializeField] private bool hapticEnabled = true;

    [Header("���� ���� ����")]
    [Range(0f, 1f)]
    [SerializeField] private float correctAnswerIntensity = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float wrongAnswerIntensity = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float buttonClickIntensity = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float achievementIntensity = 0.8f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHapticSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadHapticSettings()
    {
        if (DataManager.Instance?.gameData?.globalSettings != null)
        {
            hapticEnabled = DataManager.Instance.gameData.globalSettings.hapticEnabled;
        }
    }

    /// <summary>
    /// ���� �ǵ�� ����
    /// </summary>
    public void PlayCorrectAnswerHaptic()
    {
        if (!hapticEnabled) return;

        PlayHapticPattern(new float[] { correctAnswerIntensity }, new float[] { 0.1f });
    }

    /// <summary>
    /// ���� �ǵ�� ����
    /// </summary>
    public void PlayWrongAnswerHaptic()
    {
        if (!hapticEnabled) return;

        PlayHapticPattern(
            new float[] { wrongAnswerIntensity, 0f, wrongAnswerIntensity },
            new float[] { 0.1f, 0.05f, 0.1f }
        );
    }

    /// <summary>
    /// ��ư Ŭ�� ����
    /// </summary>
    public void PlayButtonClickHaptic()
    {
        if (!hapticEnabled) return;

        PlayHapticPattern(new float[] { buttonClickIntensity }, new float[] { 0.05f });
    }

    /// <summary>
    /// ���� �޼� ����
    /// </summary>
    public void PlayAchievementHaptic()
    {
        if (!hapticEnabled) return;

        PlayHapticPattern(
            new float[] { achievementIntensity, 0f, achievementIntensity, 0f, achievementIntensity },
            new float[] { 0.2f, 0.1f, 0.2f, 0.1f, 0.3f }
        );
    }

    /// <summary>
    /// ���� ���߱�� ���� (��� ����)
    /// </summary>
    public void PlayBeatHaptic(float intensity = 0.2f)
    {
        if (!hapticEnabled) return;

        PlayHapticPattern(new float[] { intensity }, new float[] { 0.1f });
    }

    private void PlayHapticPattern(float[] intensities, float[] durations)
    {
        StartCoroutine(HapticPatternCoroutine(intensities, durations));
    }

    private System.Collections.IEnumerator HapticPatternCoroutine(float[] intensities, float[] durations)
    {
        for (int i = 0; i < intensities.Length && i < durations.Length; i++)
        {
            if (intensities[i] > 0f)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
                // iOS ���� ����
                Handheld.Vibrate();
#endif
            }

            yield return new WaitForSeconds(durations[i]);
        }
    }

    /// <summary>
    /// ���� Ȱ��ȭ/��Ȱ��ȭ ����
    /// </summary>
    public void SetHapticEnabled(bool enabled)
    {
        hapticEnabled = enabled;

        if (DataManager.Instance?.gameData?.globalSettings != null)
        {
            DataManager.Instance.gameData.globalSettings.hapticEnabled = enabled;
            DataManager.Instance.SaveData();
        }
    }

    /// <summary>
    /// ���� Ȱ��ȭ ���� ��ȯ
    /// </summary>
    public bool IsHapticEnabled()
    {
        return hapticEnabled;
    }
}
