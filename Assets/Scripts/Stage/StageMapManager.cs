using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ���õ� ���ӿ� �´� �������� ���� �������� �����ϰ� �����մϴ�.
/// </summary>
public class StageMapManager : MonoBehaviour
{
    public static StageMapManager Instance;

    [Header("Map Settings")]
    [Tooltip("�������� ��� UI�� ������")]
    public GameObject stageNodePrefab;
    [Tooltip("������ ������ ��ġ�� �θ� Transform (Scroll View�� Content)")]
    public Transform nodeContainer;
    [Tooltip("���Ӵ� �� �������� ����")]
    public int totalStages = 8;

    [Header("Layout Settings")]
    [Tooltip("�������� ��� ���� ���� ����")]
    public float ySpacing = 180f;
    [Tooltip("������� ��ġ�� ���� �¿� ��")]
    public float xOffset = 220f;

    [Header("Layout Randomness")]
    [Tooltip("X�� ���� ��ġ�� �ּ� ����")]
    public float xRandomMin = 30f;
    [Tooltip("X�� ���� ��ġ�� �ִ� ����")]
    public float xRandomMax = 50f;
    [Tooltip("Y�� ���� ��ġ�� ���� (+/-)")]
    public float yRandomRange = 10f;

    [Header("Scene Settings")]
    [Tooltip("���������� �������� �� �Ѿ ���� ���� �̸�")]
    public string gameSceneName = "GameScene";

    private string currentGameType;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // GameManager�κ��� ���� ���õ� ���� Ÿ���� �����ɴϴ�.
        currentGameType = GameManager.CurrentGameType;

        // ���� ���� ���� ���� ��ġ�� �ʰ� �ٷ� �� ���� �����ߴٸ�, currentGameType�� ������� �� �ֽ��ϴ�.
        // �� ��� ������ �����ϰ� �׽�Ʈ�� ���� �⺻���� �����մϴ�.
        if (string.IsNullOrEmpty(currentGameType))
        {
            Debug.LogError("���õ� ���� Ÿ���� �����ϴ�! GameSelectScene���� �����ؾ� �մϴ�.");
            currentGameType = "Dolphin"; // �׽�Ʈ�� �⺻��
        }

        GenerateStageNodes();
    }

    /// <summary>
    /// ���� ���� Ÿ�Կ� ���� �������� ������ �����ϰ� ��ġ�մϴ�.
    /// </summary>
    void GenerateStageNodes()
    {
        // DataManager�κ��� ���� ������ Ŭ���� ������ �����ɴϴ�.
        int highestClearedStage = DataManager.Instance.GetHighestClearedStage(currentGameType);

        for (int i = 0; i < totalStages; i++)
        {
            // ������� ��ġ ���
            float xPos = (i % 2 == 0) ? -xOffset : xOffset;
            float yPos = -i * ySpacing;
            Vector2 nodePosition = new Vector2(xPos, yPos);

            // ���� ������ �߰�
            float randomX = (Random.value < 0.5f) ? Random.Range(-xRandomMax, -xRandomMin) : Random.Range(xRandomMin, xRandomMax);
            float randomY = Random.Range(-yRandomRange, yRandomRange);
            nodePosition += new Vector2(randomX, randomY);

            // ���������κ��� ��� ���� �� ��ġ ����
            GameObject nodeObject = Instantiate(stageNodePrefab, nodeContainer);
            nodeObject.GetComponent<RectTransform>().anchoredPosition = nodePosition;

            StageNode node = nodeObject.GetComponent<StageNode>();

            // Ŭ���� ������ ���� ����� ���� (���, ����, Ŭ����) ����
            NodeState state;
            if (i <= highestClearedStage)
            {
                state = NodeState.Cleared; // �̹� Ŭ������ ��������
            }
            else if (i == highestClearedStage + 1)
            {
                state = NodeState.Unlocked; // �ٷ� ������ �÷����� �� �ִ� ��������
            }
            else
            {
                state = NodeState.Locked; // ���� �÷����� �� ���� ��������
            }

            // ��� �ʱ�ȭ
            node.Setup(this, i, state);
        }

        // ��ũ�� ������ ��ü ���̸� �������� ������ �°� ����
        RectTransform containerRect = nodeContainer.GetComponent<RectTransform>();
        float totalHeight = (totalStages - 1) * ySpacing + 200; // �ణ�� ���� �߰�
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalHeight);
    }

    /// <summary>
    /// �������� ��尡 Ŭ���Ǿ��� �� StageNode ��ũ��Ʈ�� ���� ȣ��˴ϴ�.
    /// </summary>
    public void OnStageSelected(int stageIndex)
    {
        // ���� ��(���� ��)���� � ���������� �ε����� �� �� �ֵ��� ���õ� �������� ��ȣ�� �����մϴ�.
        PlayerPrefs.SetInt("SelectedStage", stageIndex);
        PlayerPrefs.Save();

        GameManager.Instance.LoadScene(gameSceneName);
    }
}

