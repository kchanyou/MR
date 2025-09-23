using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 선택된 게임에 맞는 스테이지 맵을 생성하고 관리합니다.
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
    /// 스테이지 맵을 초기화합니다.
    /// </summary>
    private void InitializeStageMap()
    {
        // GameManager로부터 현재 선택된 게임 타입을 가져옵니다.
        currentGameType = GameManager.CurrentGameType;

        // 게임 타입이 설정되지 않았다면 기본값 설정
        if (string.IsNullOrEmpty(currentGameType))
        {
            Debug.LogError("선택된 게임 타입이 없습니다! GameSelectScene에서 선택해야 합니다.");
            currentGameType = "Penguin"; // 테스트용 기본값
        }

        // LevelManager가 없다면 생성
        if (LevelManager.Instance == null)
        {
            GameObject levelManagerObj = new GameObject("LevelManager");
            levelManagerObj.AddComponent<LevelManager>();
        }

        // 게임 데이터 컨테이너에서 현재 캐릭터의 모드 컬렉션 가져오기
        LoadGameModeCollection();

        if (currentGameModeCollection != null)
        {
            GenerateStageNodes();
        }
        else
        {
            Debug.LogError($"게임 모드 컬렉션을 찾을 수 없습니다: {currentGameType}");
            GenerateDefaultStageNodes();
        }
    }

    /// <summary>
    /// 현재 캐릭터의 게임 모드 컬렉션을 로드합니다.
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
                    Debug.Log($"게임 모드 컬렉션 로드 완료: {currentGameType}");
                }
            }
            else
            {
                Debug.LogError("GameDataContainer.json 파일을 찾을 수 없습니다!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"게임 데이터 로드 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 현재 게임 타입에 맞는 스테이지 노드들을 생성하고 배치합니다.
    /// </summary>
    void GenerateStageNodes()
    {
        if (currentGameModeCollection == null)
        {
            GenerateDefaultStageNodes();
            return;
        }

        // DataManager로부터 현재 게임의 클리어 정보를 가져옵니다.
        int totalStages = currentGameModeCollection.GetTotalStages(); // 16개 (2모드 × 8스테이지)

        for (int i = 0; i < totalStages; i++)
        {
            // 스테이지 노드 위치 계산
            float xPos = (i % 2 == 0) ? -xOffset : xOffset;
            float yPos = -i * ySpacing;
            Vector2 nodePosition = new Vector2(xPos, yPos);

            // 랜덤 오프셋 추가
            float randomX = (Random.value < 0.5f) ? Random.Range(-xRandomMax, -xRandomMin) : Random.Range(xRandomMin, xRandomMax);
            float randomY = Random.Range(-yRandomRange, yRandomRange);
            nodePosition += new Vector2(randomX, randomY);

            // 스테이지노드를 생성하고 위치 설정
            GameObject nodeObject = Instantiate(stageNodePrefab, nodeContainer);
            nodeObject.GetComponent<RectTransform>().anchoredPosition = nodePosition;

            StageNode node = nodeObject.GetComponent<StageNode>();

            // 스테이지 데이터 가져오기
            LevelData stageData = GetStageData(i);
            string stageName = stageData?.stageName ?? $"Stage {i + 1}";

            // 클리어 상태 확인 (DataManager를 통해)
            NodeState state = GetStageNodeState(i);

            // 노드 초기화
            node.Setup(this, i, state);

            // 스테이지 이름 설정 (StageNode에 SetStageName 메서드가 있다면)
            SetStageNodeInfo(node, stageName, stageData);
        }

        // 스크롤 뷰의 전체 높이를 스테이지 개수에 맞게 조정
        UpdateScrollViewSize(totalStages);
    }

    /// <summary>
    /// 특정 인덱스의 스테이지 데이터를 가져옵니다.
    /// </summary>
    private LevelData GetStageData(int stageIndex)
    {
        if (currentGameModeCollection == null) return null;

        // 0-7: 첫 번째 모드, 8-15: 두 번째 모드
        bool isFirstMode = stageIndex < 8;
        int adjustedIndex = stageIndex % 8;

        GameModeType modeType = GetGameModeType(isFirstMode);
        return currentGameModeCollection.GetStage(modeType, adjustedIndex);
    }

    /// <summary>
    /// 현재 캐릭터와 모드에 맞는 GameModeType을 반환합니다.
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
    /// 스테이지 노드의 상태를 확인합니다.
    /// </summary>
    private NodeState GetStageNodeState(int stageIndex)
    {
        if (DataManager.Instance == null) return NodeState.Locked;

        // 각 모드별로 진행도 확인
        bool isFirstMode = stageIndex < 8;
        int adjustedIndex = stageIndex % 8;
        string modeTypeStr = isFirstMode ? "Mode1" : "Mode2";

        StageProgress progress = DataManager.Instance.GetStageProgress(currentGameType, modeTypeStr, adjustedIndex);

        if (progress.isUnlocked)
        {
            return progress.bestScore > 0 ? NodeState.Cleared : NodeState.Unlocked;
        }
        else if (stageIndex == 0) // 첫 번째 스테이지는 항상 해금
        {
            return NodeState.Unlocked;
        }
        else
        {
            return NodeState.Locked;
        }
    }

    /// <summary>
    /// 스테이지 노드에 정보를 설정합니다.
    /// </summary>
    private void SetStageNodeInfo(StageNode node, string stageName, LevelData stageData)
    {
        // StageNode에 텍스트 컴포넌트가 있다면 설정
        Text stageText = node.GetComponentInChildren<Text>();
        if (stageText != null)
        {
            stageText.text = stageName;
        }

        // 추가적인 스테이지 정보 설정
        if (stageData != null)
        {
            // 스테이지 테마 색상 적용 등
            UnityEngine.UI.Image nodeImage = node.GetComponent<UnityEngine.UI.Image>();
            if (nodeImage != null)
            {
                nodeImage.color = Color.Lerp(Color.white, stageData.stageThemeColor, 0.3f);
            }
        }
    }

    /// <summary>
    /// 기본 스테이지 노드를 생성합니다. (레벨 데이터가 없는 경우)
    /// </summary>
    private void GenerateDefaultStageNodes()
    {
        int defaultStageCount = 16; // 2모드 × 8스테이지

        for (int i = 0; i < defaultStageCount; i++)
        {
            // 기존 로직과 동일
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
    /// 스크롤 뷰의 크기를 조정합니다.
    /// </summary>
    private void UpdateScrollViewSize(int stageCount)
    {
        RectTransform containerRect = nodeContainer.GetComponent<RectTransform>();
        float totalHeight = (stageCount - 1) * ySpacing + 200; // 여백을 위한 추가
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalHeight);
    }

    /// <summary>
    /// 스테이지 노드가 클릭되었을 때 StageNode 스크립트에서 호출합니다.
    /// </summary>
    public void OnStageSelected(int stageIndex)
    {
        // 다음 씬(게임 씬)에서 어느 스테이지의 인덱스를 알 수 있도록 선택된 스테이지의 번호를 저장합니다.
        PlayerPrefs.SetInt("SelectedStage", stageIndex);
        PlayerPrefs.Save();

        // 게임 씬으로 이동
        GameManager.Instance.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 업적 해금 알림을 받았을 때 호출됩니다.
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
    /// 모든 스테이지 노드를 새로고침합니다.
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
