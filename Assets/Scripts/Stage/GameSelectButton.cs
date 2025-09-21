using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GameSelectButton : MonoBehaviour
{
    [Header("게임 타입 설정")]
    [Tooltip("이 버튼이 선택할 게임의 이름 (DataManager의 Key와 일치해야 함)")]
    public string gameType;

    [Header("연결할 씬 이름")]
    public string stageMapSceneName = "StageMapScene";

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(SelectGame);

        if (string.IsNullOrEmpty(gameType))
        {
            Debug.LogError("GameSelectButton에 gameType이 설정되지 않았습니다!", gameObject);
        }
    }

    void SelectGame()
    {
        // GameManager에 선택한 게임 타입을 저장
        GameManager.Instance.SetCurrentGameType(gameType);

        // 스테이지 맵 씬으로 전환
        GameManager.Instance.LoadScene(stageMapSceneName);
    }
}
