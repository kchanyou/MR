using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임의 전반적인 상태와 씬 전환을 관리하는 싱글톤 클래스입니다.
/// 현재 선택된 게임 타입을 저장하는 역할도 합니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    /// <summary>
    /// 현재 플레이어가 선택한 게임의 종류를 저장하는 정적(static) 변수입니다.
    /// static으로 선언되어 씬이 바뀌어도 값이 유지되며, 어디서든 GameManager.CurrentGameType으로 접근할 수 있습니다.
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
    /// 현재 게임 타입을 설정합니다. '게임 선택 씬'의 버튼들이 이 함수를 호출하게 됩니다.
    /// </summary>
    /// <param name="gameType">"Dolphin", "Penguin", "Automaton" 등 DataManager에 정의된 Key값</param>
    public void SetCurrentGameType(string gameType)
    {
        CurrentGameType = gameType;
        Debug.Log($"현재 게임 타입 설정: {CurrentGameType}");
    }

    /// <summary>
    /// 지정된 이름의 씬을 로드합니다.
    /// </summary>
    /// <param name="sceneName">로드할 씬의 이름</param>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 게임을 종료합니다.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

