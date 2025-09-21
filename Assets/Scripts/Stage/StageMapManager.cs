using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 선택된 게임에 맞는 스테이지 맵을 동적으로 생성하고 관리합니다.
/// </summary>
public class StageMapManager : MonoBehaviour
{
    public static StageMapManager Instance;

    [Header("Map Settings")]
    [Tooltip("스테이지 노드 UI의 프리팹")]
    public GameObject stageNodePrefab;
    [Tooltip("생성된 노드들이 위치할 부모 Transform (Scroll View의 Content)")]
    public Transform nodeContainer;
    [Tooltip("게임당 총 스테이지 개수")]
    public int totalStages = 8;

    [Header("Layout Settings")]
    [Tooltip("스테이지 노드 간의 세로 간격")]
    public float ySpacing = 180f;
    [Tooltip("지그재그 배치를 위한 좌우 폭")]
    public float xOffset = 220f;

    [Header("Layout Randomness")]
    [Tooltip("X축 랜덤 위치의 최소 범위")]
    public float xRandomMin = 30f;
    [Tooltip("X축 랜덤 위치의 최대 범위")]
    public float xRandomMax = 50f;
    [Tooltip("Y축 랜덤 위치의 범위 (+/-)")]
    public float yRandomRange = 10f;

    [Header("Scene Settings")]
    [Tooltip("스테이지를 선택했을 때 넘어갈 게임 씬의 이름")]
    public string gameSceneName = "GameScene";

    private string currentGameType;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // GameManager로부터 현재 선택된 게임 타입을 가져옵니다.
        currentGameType = GameManager.CurrentGameType;

        // 만약 게임 선택 씬을 거치지 않고 바로 이 씬을 실행했다면, currentGameType이 비어있을 수 있습니다.
        // 이 경우 오류를 방지하고 테스트를 위해 기본값을 설정합니다.
        if (string.IsNullOrEmpty(currentGameType))
        {
            Debug.LogError("선택된 게임 타입이 없습니다! GameSelectScene에서 시작해야 합니다.");
            currentGameType = "Dolphin"; // 테스트용 기본값
        }

        GenerateStageNodes();
    }

    /// <summary>
    /// 현재 게임 타입에 맞춰 스테이지 노드들을 생성하고 배치합니다.
    /// </summary>
    void GenerateStageNodes()
    {
        // DataManager로부터 현재 게임의 클리어 정보를 가져옵니다.
        int highestClearedStage = DataManager.Instance.GetHighestClearedStage(currentGameType);

        for (int i = 0; i < totalStages; i++)
        {
            // 지그재그 위치 계산
            float xPos = (i % 2 == 0) ? -xOffset : xOffset;
            float yPos = -i * ySpacing;
            Vector2 nodePosition = new Vector2(xPos, yPos);

            // 랜덤 오프셋 추가
            float randomX = (Random.value < 0.5f) ? Random.Range(-xRandomMax, -xRandomMin) : Random.Range(xRandomMin, xRandomMax);
            float randomY = Random.Range(-yRandomRange, yRandomRange);
            nodePosition += new Vector2(randomX, randomY);

            // 프리팹으로부터 노드 생성 및 위치 설정
            GameObject nodeObject = Instantiate(stageNodePrefab, nodeContainer);
            nodeObject.GetComponent<RectTransform>().anchoredPosition = nodePosition;

            StageNode node = nodeObject.GetComponent<StageNode>();

            // 클리어 정보에 따라 노드의 상태 (잠김, 해제, 클리어) 결정
            NodeState state;
            if (i <= highestClearedStage)
            {
                state = NodeState.Cleared; // 이미 클리어한 스테이지
            }
            else if (i == highestClearedStage + 1)
            {
                state = NodeState.Unlocked; // 바로 다음에 플레이할 수 있는 스테이지
            }
            else
            {
                state = NodeState.Locked; // 아직 플레이할 수 없는 스테이지
            }

            // 노드 초기화
            node.Setup(this, i, state);
        }

        // 스크롤 영역의 전체 높이를 스테이지 개수에 맞게 조절
        RectTransform containerRect = nodeContainer.GetComponent<RectTransform>();
        float totalHeight = (totalStages - 1) * ySpacing + 200; // 약간의 여백 추가
        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, totalHeight);
    }

    /// <summary>
    /// 스테이지 노드가 클릭되었을 때 StageNode 스크립트에 의해 호출됩니다.
    /// </summary>
    public void OnStageSelected(int stageIndex)
    {
        // 다음 씬(게임 씬)에서 어떤 스테이지를 로드할지 알 수 있도록 선택된 스테이지 번호를 저장합니다.
        PlayerPrefs.SetInt("SelectedStage", stageIndex);
        PlayerPrefs.Save();

        GameManager.Instance.LoadScene(gameSceneName);
    }
}

