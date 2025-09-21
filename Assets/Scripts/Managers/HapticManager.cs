using UnityEngine;

// Lofelt.NiceVibrations 에셋을 사용할 경우, 이 네임스페이스를 활성화합니다.
#if USE_NICE_VIBRATIONS
using Lofelt.NiceVibrations;
#endif

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 가벼운 기본 진동을 재생합니다. (UI 터치 등)
    /// </summary>
    public void VibrateLight()
    {
#if UNITY_IOS && USE_NICE_VIBRATIONS
        // iOS이고 NiceVibrations 에셋이 활성화된 경우, 가벼운 탭틱 피드백을 재생합니다.
        HapticPatterns.Play(HapticPatterns.PresetType.LightImpact);

#elif UNITY_ANDROID
        // 안드로이드의 경우, 기본 진동을 사용하되 강도를 약하게 조절할 수 있습니다.
        // (이 기능은 Android API 레벨에 따라 지원 여부가 다릅니다.)
        // 여기서는 기본 진동을 사용합니다.
        Handheld.Vibrate();

#elif UNITY_EDITOR
        // 에디터에서는 진동을 실행할 수 없으므로, 어떤 진동이 호출되었는지 로그를 남깁니다.
        Debug.Log("Haptic Feedback: Vibrate Light");

#else
        // 그 외 플랫폼(예: iOS에서 NiceVibrations를 사용하지 않을 때)에서는 기본 진동을 사용합니다.
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// '성공'을 의미하는 햅틱 피드백을 재생합니다.
    /// </summary>
    public void VibrateSuccess()
    {
#if UNITY_IOS && USE_NICE_VIBRATIONS
        HapticPatterns.Play(HapticPatterns.PresetType.Success);

#elif UNITY_ANDROID
        // 안드로이드에서는 '성공'을 표현하기 위해 짧은 진동을 두 번 주는 패턴을 사용할 수 있습니다.
        // (이를 위해서는 별도의 패턴 재생 기능 구현이 필요하나, 여기서는 기본 진동으로 대체합니다.)
        Handheld.Vibrate();

#elif UNITY_EDITOR
        Debug.Log("Haptic Feedback: Vibrate Success");

#else
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// '실패' 또는 '경고'를 의미하는 햅틱 피드백을 재생합니다.
    /// </summary>
    public void VibrateFailure()
    {
#if UNITY_IOS && USE_NICE_VIBRATIONS
        HapticPatterns.Play(HapticPatterns.PresetType.Failure);

#elif UNITY_ANDROID
        Handheld.Vibrate();

#elif UNITY_EDITOR
        Debug.Log("Haptic Feedback: Vibrate Failure");

#else
        Handheld.Vibrate();
#endif
    }
}

