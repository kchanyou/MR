using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ������ �������� ���¿� �� ��ȯ�� �����ϴ� �̱��� Ŭ�����Դϴ�.
/// ���� ���õ� ���� Ÿ���� �����ϴ� ���ҵ� �մϴ�.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    /// <summary>
    /// ���� �÷��̾ ������ ������ ������ �����ϴ� ����(static) �����Դϴ�.
    /// static���� ����Ǿ� ���� �ٲ� ���� �����Ǹ�, ��𼭵� GameManager.CurrentGameType���� ������ �� �ֽ��ϴ�.
    /// </summary>
    public static string CurrentGameType { get; private set; }

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
    /// ���� ���� Ÿ���� �����մϴ�. '���� ���� ��'�� ��ư���� �� �Լ��� ȣ���ϰ� �˴ϴ�.
    /// </summary>
    /// <param name="gameType">"Dolphin", "Penguin", "Automaton" �� DataManager�� ���ǵ� Key��</param>
    public void SetCurrentGameType(string gameType)
    {
        CurrentGameType = gameType;
        Debug.Log($"���� ���� Ÿ�� ����: {CurrentGameType}");
    }

    /// <summary>
    /// ������ �̸��� ���� �ε��մϴ�.
    /// </summary>
    /// <param name="sceneName">�ε��� ���� �̸�</param>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// ������ �����մϴ�.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("������ �����մϴ�.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

