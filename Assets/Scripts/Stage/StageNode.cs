using UnityEngine;
using UnityEngine.UI;
using TMPro;

// �������� ����� 3���� ���¸� �����մϴ�.
public enum NodeState { Locked, Unlocked, Cleared }

public class StageNode : MonoBehaviour
{
    [Header("UI References")]
    public Button stageButton;          // Ŭ�� ������ ��ư ������Ʈ
    public TextMeshProUGUI stageNumberText; // �������� ��ȣ�� ǥ���� �ؽ�Ʈ
    public GameObject lockedIcon;       // ��� ������ �� ǥ�õ� ������ (�ڹ��� �̹��� ��)
    public GameObject clearedIcon;      // Ŭ���� ������ �� ǥ�õ� ������ (üũ��ũ, �� �̹��� ��)

    private int stageIndex;
    private StageMapManager manager;

    /// <summary>
    /// StageMapManager�� ��带 ������ �� ȣ���Ͽ� �ʱ�ȭ�ϴ� �Լ�
    /// </summary>
    public void Setup(StageMapManager mapManager, int index, NodeState state)
    {
        manager = mapManager;
        stageIndex = index;
        stageNumberText.text = (stageIndex + 1).ToString();

        // ����� ���¿� ���� UI�� �����մϴ�.
        switch (state)
        {
            case NodeState.Locked:
                stageButton.interactable = false; // ��ư ��Ȱ��ȭ
                lockedIcon.SetActive(true);
                clearedIcon.SetActive(false);
                break;
            case NodeState.Unlocked:
                stageButton.interactable = true;  // ��ư Ȱ��ȭ
                lockedIcon.SetActive(false);
                clearedIcon.SetActive(false);
                break;
            case NodeState.Cleared:
                stageButton.interactable = true;  // ��ư Ȱ��ȭ (�ٽ� �÷��� ����)
                lockedIcon.SetActive(false);
                clearedIcon.SetActive(true);
                break;
        }
    }

    void Start()
    {
        // ��ư�� Ŭ���Ǿ��� �� StageMapManager���� �˸����� �����ʸ� �߰��մϴ�.
        stageButton.onClick.AddListener(OnNodeClicked);
    }

    private void OnNodeClicked()
    {
        // Haptic �ǵ���� ���� �ְ�
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.VibrateLight();
        }
        // �Ŵ������� �� ��° ���������� ���õǾ����� �˸��ϴ�.
        manager.OnStageSelected(stageIndex);
    }
}
