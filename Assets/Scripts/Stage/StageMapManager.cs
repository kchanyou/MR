using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ���õ� ���ӿ� �´� �������� ���� �����ϰ� �����մϴ�.
/// </summary>
public class StageMapManager : MonoBehaviour
{
    public static StageMapManager Instance;

    [Header("Map Settings")]
    [SerializeField] private GameObject stageNodePrefab;
    [SerializeField] private Transform nodeContainer;

    [Header("Layout Settings")]
    [SerializeField] private float ySpacing = 180f;
    [SerializeField] private float xOffset = 220f;

    [Header("Layout Randomness")]
    [SerializeField] private float xRandomMin = 30f;
    [SerializeField] private float xRandomMax = 50f;
    [SerializeField] private float yRandomRange = 10f;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";

    private string currentGameType;
    private GameModeCollection currentGameModeCollection;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeStageMap();
    }

    /// <summary>
    /// �������� ���� �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeStageMap()
    {
        // GameManager�κ��� ���� ���õ� ���� Ÿ���� �����ɴϴ�.
        currentGameType = GameManager.CurrentGameType;

        // ���� Ÿ���� �������� �ʾҴٸ� �⺻�� ����
        if (string.IsNullOrEmpty(currentGameType))
        {
            Debug.LogError("���õ� ���� Ÿ���� �����ϴ�! GameSelectScene���� �����ؾ� �մϴ�.");
            currentGameType = "Penguin"; // �׽�Ʈ�� �⺻��
        }

        // LevelManager�� ���ٸ� ����
        if (LevelManager.Instance == null)
        {
            GameObject levelManagerObj = new GameObject("LevelManager");
            levelManagerObj.AddComponent<LevelManager>();
        }

        // ���� ������ �����̳ʿ��� ���� ĳ������ ��� �÷��� ��������
        LoadGameModeCollection();

        if (currentGameModeCollection != null)
        {
            GenerateStageNodes();
        }
        else
        {
            Debug.LogError($"���� ��� �÷����� ã�� �� �����ϴ�: {currentGameType}");
            GenerateDefaultStageNodes();
        }
    }

    /// <summary>
    /// ���� ĳ������ ���� ��� �÷����� �ε��մϴ�.
    /// </summary>
    private void LoadGameModeCollection()
    {
        try
        {
            TextAsset gameDataFile = Resources.Load<TextAsset>("LevelData/GameDataContainer");

            if (gameDataFile != null)
            {
                GameDataContainer container = JsonUtility.FromJson<GameDataContainer>(gameDataFile.text);
                currentGameModeCollection = container?.GetCharacterModes(currentGameType);

                if (currentGameModeCollection != null)
                {
                    Debug.Log($"���� ��� �÷��� �ε� �Ϸ�: {currentGameType}");
                }
            }
            else
            {
                Debug.LogError("GameDataContainer.json ������ ã�� �� �����ϴ�!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"���� ������ �ε� ����: {e.Message}");
        }
    }

    /// <summary>
    /// ���� ���� Ÿ�Կ� �´� �������� ������ �����ϰ� ��ġ�մϴ�.
    /// </summary>
    void GenerateStageNodes()
    {
        if (currentGameModeCollection == null)
        {
            GenerateDefaultStageNodes();
            return;
        }

        // DataManager�κ��� ���� ������ Ŭ���� ������ �����ɴϴ�.
        int totalStages = currentGameModeCollection.GetTotalStages(); // 16�� (2��� �� 8��������)

        for (int i = 0; i < totalStages; i++)
        {
            // �������� ��� ��ġ ���
            float xPos = (i % 2 == 0) ? -xOffset : xOffset;
            float yPos = -i * ySpacing;
            Vector2 nodePosition = new Vector2(xPos, yPos);

            // ���� ������ �߰�
            float randomX = (Random.value < 0.5f) ? Random.Range(-xRandomMax, -xRandomMin) : Random.Range(xRandomMin, xRandomMax);
            float randomY = Random.Range(-yRandomRange, yRandomRange);
            nodePosition += new Vector2(randomX, randomY);

            // ����������带 �����ϰ� ��ġ ����
            GameObject nodeObject = Instantiate(stageNodePrefab, nodeContainer);
            nodeObject.GetComponent<RectTransform>().anchoredPosition = nodePosition;

            StageNode node = nodeObject.GetComponent<StageNode>();

            // �������� ������ ��������
            LevelData stageData = GetStageData(i);
            string stageName = stageData?.stageName ?? $"Stage {i + 1}";

            // Ŭ���� ���� Ȯ�� (DataManager�� ����)
            NodeState state = GetStageNodeState(i);

            // ��� �ʱ�ȭ
            node.Setup(this, i, state);

            // �������� �̸� ���� (StageNode�� SetStageName �޼��尡 �ִٸ�)
            SetStageNodeInfo(node, stageName, stageData);
        }

        // ��ũ�� ���� ��ü ���̸� �������� ������ �°� ����
        UpdateScrollViewSize(totalStages);
    }

    /// <summary>
    /// Ư�� �ε����� �������� �����͸� �����ɴϴ�.
    /// </summary>
    private LevelData GetStageData(int stageIndex)
    {
        if (currentGameModeCollection == null) return null;

        // 0-7: ù ��° ���, 8-15: �� ��° ���
        bool isFirstMode = stageIndex < 8;
        int adjustedIndex = stageIndex % 8;

        GameModeType modeType = GetGameModeType(isFirstMode);
        return currentGameModeCollection.GetStage(modeType, adjustedIndex);
    }

    /// <summary>
    /// ���� ĳ���Ϳ� ��忡 �´� GameModeType�� ��ȯ�մϴ�.
    /// </summary>
    private GameModeType GetGameModeType(bool isFirstMode)
    {
        switch (currentGameType)
        {
            case "Dolphin":
                return isFirstMode ? GameModeType.Dolphin_DifferentSound : GameModeType.Dolphin_MelodyShape;
            case "Penguin":
                return isFirstMode ? GameModeType.Penguin_RhythmJump : GameModeType.Penguin_RhythmFollow;
            case "Otamatone":
                return isFirstMode ? GameModeType.Otamatone_DifferentInstrument : GameModeType.Otamatone_InstrumentMatch;
            default:
                return GameModeType.Dolphin_DifferentSound;
        }
    }

    /// <summary>
    /// �������� ����� ���¸� Ȯ���մϴ�.
    /// </summary>
    private NodeState GetStageNodeState(int stageIndex)
    {
        if (DataManager.Instance == null) return NodeState.Locked;

        // �� ��庰�� ���൵ Ȯ��
        bool isFirstMode = stageIndex < 8;
        int adjustedIndex = stageIndex % 8;
        string modeTypeStr = isFirstMode ? "Mode1" : "Mode2";

        StageProgress progress = DataManager.Instance.GetStageProgress(currentGameType, modeTypeStr, adjustedIndex);

        if (progress.isUnlocked)
        {
            return progress.bestScore > 0 ? NodeState.Cleared : NodeState.Unlocked;
        }
        else if (stageIndex == 0) // ù ��° ���������� �׻� �ر�
        {
            return NodeState.Unlocked;
        }
        else
        {
            return NodeState.Locked;
        }
    }

    /// <summary>
    /// �������� ��忡 ������ �����մϴ�.
    /// </summary>
    private void SetStageNodeInfo(StageNode node, string stageName, LevelData stageData)
    {
        // StageNode�� �ؽ�Ʈ ������Ʈ�� �ִٸ� ����
        Text stageText = node.GetComponentInChildren<Text>();
        if (stageText != null)
        {
            stageText.text = stageName;
        }

        // �߰����� �������� ���� ����
        if (stageData != null)
        {
            // �������� �׸� ���� ���� ��
            UnityEngine.UI.Image nodeImage = node.GetComponent<UnityEngine.UI.Image>();
            if (nodeImage != null)
            {
                nodeImage.color = Color.Lerp(Color.white, stageData.stageThemeColor, 0.3f);
            }
        }
    }

    /// <summary>
    /// �⺻ �������� ��带 �����մϴ�. (���� �����Ͱ� ���� ���)
    /// </summary>
    private void GenerateDefaultStageNodes()
    {
        int defaultStageCount = 16; // 2��� �� 8��������

        for (int i = 0; i < defaultStageCount; i++)
        {
            // ���� ������ ����
            float xPos = (i % 2 == 0) ? -xOffset : xOffset;
            float yPos = -i * ySpacing;
            Vector2 nodePosition = new Vector2(xPos, yPos);

            float randomX = (Random.value < 0.5f) ? Random.Range(-xRandomMax, -xRandomMin) : Random.Range(xRandomMin, xRandomMax);
            float randomY = Random.Range(-yRandomRange, yRandomRange);
            nodePosition += new Vector2(randomX, randomY);

            GameObject nodeObject = Instantiate(stageNodePrefab, nodeContainer);
            nodeObject.GetComponent<RectTransform>().anchoredPosition = nodePosition;

            StageNode node = nodeObject.GetComponent<StageNode>();

            NodeState state = i == 0 ? NodeState.Unlocked : NodeState.Locked;
            node.Setup(this, i, state);
        }

        UpdateScrollViewSize(defaultStageCount);
    }

    /// <summary>
    /// ��ũ�� ���� ũ�⸦ �����մϴ�.
    /// </summary>
    private void UpdateScrollViewSize(int stageCount)
    {
        RectTransform containerRect = nodeContainer.GetComponent<RectTransform>();
        float totalHeight = (stageCount - 1) * ySpacing + 200; // ������ ���� �߰�
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalHeight);
    }

    /// <summary>
    /// �������� ��尡 Ŭ���Ǿ��� �� StageNode ��ũ��Ʈ���� ȣ���մϴ�.
    /// </summary>
    public void OnStageSelected(int stageIndex)
    {
        // ���� ��(���� ��)���� ��� ���������� �ε����� �� �� �ֵ��� ���õ� ���������� ��ȣ�� �����մϴ�.
        PlayerPrefs.SetInt("SelectedStage", stageIndex);
        PlayerPrefs.Save();

        // ���� ������ �̵�
        GameManager.Instance.LoadScene(gameSceneName);
    }

    /// <summary>
    /// ���� �ر� �˸��� �޾��� �� ȣ��˴ϴ�.
    /// </summary>
    public void OnAchievementUnlocked(int stageIndex)
    {
        StageNode[] stageNodes = FindObjectsOfType<StageNode>();

        foreach (StageNode node in stageNodes)
        {
            if (node.transform.GetSiblingIndex() == stageIndex)
            {
                node.OnAchievementUnlocked();
                break;
            }
        }
    }

    /// <summary>
    /// ��� �������� ��带 ���ΰ�ħ�մϴ�.
    /// </summary>
    public void RefreshAllNodes()
    {
        StageNode[] stageNodes = FindObjectsOfType<StageNode>();

        foreach (StageNode node in stageNodes)
        {
            node.RefreshNode();
        }
    }
}
