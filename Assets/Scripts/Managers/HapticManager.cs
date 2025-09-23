using UnityEngine;

/// <summary>
/// 진동 피드백 매니저 - 인공와우 사용자를 위한 촉각 피드백
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance;

    [Header("진동 설정")]
    [SerializeField] private bool hapticEnabled = true;

    [Header("진동 강도 설정")]
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
    /// 정답 피드백 진동
    /// </summary>
    public void PlayCorrectAnswerHaptic()
    {
        if (!hapticEnabled) return;

        PlayHapticPattern(new float[] { correctAnswerIntensity }, new float[] { 0.1f });
    }

    /// <summary>
    /// 오답 피드백 진동
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
    /// 버튼 클릭 진동
    /// </summary>
    public void PlayButtonClickHaptic()
    {
        if (!hapticEnabled) return;

        PlayHapticPattern(new float[] { buttonClickIntensity }, new float[] { 0.05f });
    }

    /// <summary>
    /// 업적 달성 진동
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
    /// 박자 맞추기용 진동 (펭귄 게임)
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
                // iOS 진동 구현
                Handheld.Vibrate();
#endif
            }

            yield return new WaitForSeconds(durations[i]);
        }
    }

    /// <summary>
    /// 진동 활성화/비활성화 설정
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
    /// 진동 활성화 상태 반환
    /// </summary>
    public bool IsHapticEnabled()
    {
        return hapticEnabled;
    }
}
