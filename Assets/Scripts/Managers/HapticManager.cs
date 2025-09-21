using UnityEngine;

// Lofelt.NiceVibrations ������ ����� ���, �� ���ӽ����̽��� Ȱ��ȭ�մϴ�.
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
    /// ������ �⺻ ������ ����մϴ�. (UI ��ġ ��)
    /// </summary>
    public void VibrateLight()
    {
#if UNITY_IOS && USE_NICE_VIBRATIONS
        // iOS�̰� NiceVibrations ������ Ȱ��ȭ�� ���, ������ ��ƽ �ǵ���� ����մϴ�.
        HapticPatterns.Play(HapticPatterns.PresetType.LightImpact);

#elif UNITY_ANDROID
        // �ȵ���̵��� ���, �⺻ ������ ����ϵ� ������ ���ϰ� ������ �� �ֽ��ϴ�.
        // (�� ����� Android API ������ ���� ���� ���ΰ� �ٸ��ϴ�.)
        // ���⼭�� �⺻ ������ ����մϴ�.
        Handheld.Vibrate();

#elif UNITY_EDITOR
        // �����Ϳ����� ������ ������ �� �����Ƿ�, � ������ ȣ��Ǿ����� �α׸� ����ϴ�.
        Debug.Log("Haptic Feedback: Vibrate Light");

#else
        // �� �� �÷���(��: iOS���� NiceVibrations�� ������� ���� ��)������ �⺻ ������ ����մϴ�.
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// '����'�� �ǹ��ϴ� ��ƽ �ǵ���� ����մϴ�.
    /// </summary>
    public void VibrateSuccess()
    {
#if UNITY_IOS && USE_NICE_VIBRATIONS
        HapticPatterns.Play(HapticPatterns.PresetType.Success);

#elif UNITY_ANDROID
        // �ȵ���̵忡���� '����'�� ǥ���ϱ� ���� ª�� ������ �� �� �ִ� ������ ����� �� �ֽ��ϴ�.
        // (�̸� ���ؼ��� ������ ���� ��� ��� ������ �ʿ��ϳ�, ���⼭�� �⺻ �������� ��ü�մϴ�.)
        Handheld.Vibrate();

#elif UNITY_EDITOR
        Debug.Log("Haptic Feedback: Vibrate Success");

#else
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// '����' �Ǵ� '���'�� �ǹ��ϴ� ��ƽ �ǵ���� ����մϴ�.
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

