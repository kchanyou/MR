using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 스테이지 노드의 3가지 상태를 정의합니다.
public enum NodeState { Locked, Unlocked, Cleared }

public class StageNode : MonoBehaviour
{
    [Header("UI References")]
    public Button stageButton;          // 클릭 가능한 버튼 컴포넌트
    public TextMeshProUGUI stageNumberText; // 스테이지 번호를 표시할 텍스트
    public GameObject lockedIcon;       // 잠금 상태일 때 표시될 아이콘 (자물쇠 이미지 등)
    public GameObject clearedIcon;      // 클리어 상태일 때 표시될 아이콘 (체크마크, 별 이미지 등)

    private int stageIndex;
    private StageMapManager manager;

    /// <summary>
    /// StageMapManager가 노드를 생성할 때 호출하여 초기화하는 함수
    /// </summary>
    public void Setup(StageMapManager mapManager, int index, NodeState state)
    {
        manager = mapManager;
        stageIndex = index;
        stageNumberText.text = (stageIndex + 1).ToString();

        // 노드의 상태에 따라 UI를 설정합니다.
        switch (state)
        {
            case NodeState.Locked:
                stageButton.interactable = false; // 버튼 비활성화
                lockedIcon.SetActive(true);
                clearedIcon.SetActive(false);
                break;
            case NodeState.Unlocked:
                stageButton.interactable = true;  // 버튼 활성화
                lockedIcon.SetActive(false);
                clearedIcon.SetActive(false);
                break;
            case NodeState.Cleared:
                stageButton.interactable = true;  // 버튼 활성화 (다시 플레이 가능)
                lockedIcon.SetActive(false);
                clearedIcon.SetActive(true);
                break;
        }
    }

    void Start()
    {
        // 버튼이 클릭되었을 때 StageMapManager에게 알리도록 리스너를 추가합니다.
        stageButton.onClick.AddListener(OnNodeClicked);
    }

    private void OnNodeClicked()
    {
        // Haptic 피드백을 먼저 주고
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.VibrateLight();
        }
        // 매니저에게 몇 번째 스테이지가 선택되었는지 알립니다.
        manager.OnStageSelected(stageIndex);
    }
}
