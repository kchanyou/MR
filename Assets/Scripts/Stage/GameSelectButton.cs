using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GameSelectButton : MonoBehaviour
{
    [Header("���� Ÿ�� ����")]
    [Tooltip("�� ��ư�� ������ ������ �̸� (DataManager�� Key�� ��ġ�ؾ� ��)")]
    public string gameType;

    [Header("������ �� �̸�")]
    public string stageMapSceneName = "StageMapScene";

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(SelectGame);

        if (string.IsNullOrEmpty(gameType))
        {
            Debug.LogError("GameSelectButton�� gameType�� �������� �ʾҽ��ϴ�!", gameObject);
        }
    }

    void SelectGame()
    {
        // GameManager�� ������ ���� Ÿ���� ����
        GameManager.Instance.SetCurrentGameType(gameType);

        // �������� �� ������ ��ȯ
        GameManager.Instance.LoadScene(stageMapSceneName);
    }
}
