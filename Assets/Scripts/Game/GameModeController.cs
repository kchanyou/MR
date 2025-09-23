using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 각 게임 씬에서 게임 모드와 레벨을 제어하는 통합 컨트롤러
/// </summary>
public class GameModeController : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Text levelInfoText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button homeButton;

    [Header("게임 모드 컨트롤러들")]
    [SerializeField] private DolphinGameController dolphinController;
    [SerializeField] private PenguinGameController penguinController;
    [SerializeField] private OtamatoneGameController otamatoneController;

    [Header("게임 상태")]
    public bool isGameActive = false;
    public int currentScore = 0;
    public float remainingTime = 0f;

    private LevelData currentLevelData;
    private QuestionData currentQuestionData;
    private IGameMode activeGameMode;
    private float gameStartTime;
    private int currentQuestionIndex = 0;

    void Start()
    {
        InitializeGame();
        SetupButtons();
    }

    void Update()
    {
        if (isGameActive && currentLevelData != null && currentLevelData.GetValidQuestionCount() > 0)
        {
            UpdateGameTimer();
        }
    }

    private void SetupButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(ReturnToStageSelect);
        }
    }

    /// <summary>
    /// 게임을 초기화합니다.
    /// </summary>
    private void InitializeGame()
    {
        // 2_Stage 씬에서 설정된 게임 타입과 스테이지 인덱스 가져오기
        string gameType = GameManager.CurrentGameType;
        int selectedStage = PlayerPrefs.GetInt("SelectedStage", 0);

        if (string.IsNullOrEmpty(gameType))
        {
            Debug.LogError("게임 타입이 설정되지 않았습니다!");
            ReturnToStageSelect();
            return;
        }

        // LevelManager 초기화
        if (LevelManager.Instance == null)
        {
            GameObject levelManagerObj = new GameObject("LevelManager");
            levelManagerObj.AddComponent<LevelManager>();
        }

        // 게임 모드 타입 결정
        GameModeType modeType = DetermineGameModeType(gameType, selectedStage);

        // 게임 모드 설정 및 스테이지 로드
        if (LevelManager.Instance.SetGameMode(modeType, selectedStage))
        {
            LoadCurrentStage();
        }
        else
        {
            Debug.LogError($"게임 모드 설정 실패: {modeType}");
            ReturnToStageSelect();
        }
    }

    private GameModeType DetermineGameModeType(string gameType, int stageIndex)
    {
        // 스테이지 인덱스를 기반으로 게임 모드 결정
        // 각 캐릭터당 8개 스테이지: 0-3은 첫 번째 모드, 4-7은 두 번째 모드
        bool isFirstMode = stageIndex < 4;

        switch (gameType)
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
    /// 현재 스테이지 데이터를 로드하고 게임을 시작합니다.
    /// </summary>
    private void LoadCurrentStage()
    {
        currentLevelData = LevelManager.Instance.GetCurrentStageData();

        if (currentLevelData == null || !currentLevelData.IsValid())
        {
            Debug.LogError("유효하지 않은 스테이지 데이터입니다!");
            ReturnToStageSelect();
            return;
        }

        // 적절한 게임 모드 컨트롤러 활성화
        SetupGameModeController();

        // UI 초기화
        InitializeUI();

        // 게임 상태 초기화
        currentScore = 0;
        currentQuestionIndex = 0;
        gameStartTime = Time.time;

        Debug.Log($"스테이지 로드 완료: {currentLevelData.stageName}");

        // 첫 번째 문제 시작
        LoadNextQuestion();
    }

    private void SetupGameModeController()
    {
        // 모든 컨트롤러 비활성화
        if (dolphinController != null) dolphinController.gameObject.SetActive(false);
        if (penguinController != null) penguinController.gameObject.SetActive(false);
        if (otamatoneController != null) otamatoneController.gameObject.SetActive(false);

        // 현재 게임 모드에 맞는 컨트롤러 활성화
        switch (currentLevelData.primaryGameMode)
        {
            case GameModeType.Dolphin_DifferentSound:
            case GameModeType.Dolphin_MelodyShape:
                if (dolphinController != null)
                {
                    dolphinController.gameObject.SetActive(true);
                    activeGameMode = dolphinController;
                }
                break;

            case GameModeType.Penguin_RhythmJump:
            case GameModeType.Penguin_RhythmFollow:
                if (penguinController != null)
                {
                    penguinController.gameObject.SetActive(true);
                    activeGameMode = penguinController;
                }
                break;

            case GameModeType.Otamatone_DifferentInstrument:
            case GameModeType.Otamatone_InstrumentMatch:
                if (otamatoneController != null)
                {
                    otamatoneController.gameObject.SetActive(true);
                    activeGameMode = otamatoneController;
                }
                break;
        }

        // 게임 모드 초기화
        activeGameMode?.InitializeGameMode(currentLevelData);
    }

    private void InitializeUI()
    {
        UpdateLevelInfoUI();
        UpdateScoreUI();
        UpdateTimeUI();

        // 배경 설정
        if (backgroundImage != null)
        {
            backgroundImage.color = currentLevelData.stageThemeColor;
        }
    }

    /// <summary>
    /// 다음 문제를 로드합니다.
    /// </summary>
    public void LoadNextQuestion()
    {
        if (currentQuestionIndex < currentLevelData.GetValidQuestionCount())
        {
            currentQuestionData = currentLevelData.questions[currentQuestionIndex];

            if (currentQuestionData != null && currentQuestionData.IsValid())
            {
                isGameActive = true;
                activeGameMode?.StartQuestion(currentQuestionData);
                UpdateUI();
            }
            else
            {
                Debug.LogError($"유효하지 않은 문제 데이터: {currentQuestionIndex}");
                CompleteStage();
            }
        }
        else
        {
            CompleteStage();
        }
    }

    /// <summary>
    /// 점수를 추가합니다.
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreUI();
    }

    /// <summary>
    /// 스테이지를 완료합니다.
    /// </summary>
    public void CompleteStage()
    {
        isGameActive = false;
        float playTime = Time.time - gameStartTime;

        // 플레이 시간 기록
        string characterType = currentLevelData.characterName;
        DataManager.Instance?.AddPlayTime(characterType, playTime);

        // 스테이지 완료 처리
        LevelManager.Instance?.CompleteStage(currentScore);

        Debug.Log($"스테이지 완료! 최종 점수: {currentScore}");

        // 결과 화면 표시 또는 다음 스테이지로 진행
        ShowStageCompleteUI();
    }

    private void ShowStageCompleteUI()
    {
        // 스테이지 완료 UI 표시
        // 여기서 결과 화면을 보여주거나 바로 스테이지 선택으로 돌아갈 수 있음

        // 임시로 2초 후 스테이지 선택으로 돌아가기
        Invoke(nameof(ReturnToStageSelect), 2.0f);
    }

    private void UpdateGameTimer()
    {
        // 현재는 시간 제한 없음, 필요시 구현
    }

    private void UpdateLevelInfoUI()
    {
        if (levelInfoText != null && currentLevelData != null)
        {
            levelInfoText.text = currentLevelData.stageName;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"점수: {currentScore}";
        }
    }

    private void UpdateTimeUI()
    {
        if (timeText != null)
        {
            timeText.text = $"문제: {currentQuestionIndex + 1}/10";
        }
    }

    private void UpdateUI()
    {
        UpdateLevelInfoUI();
        UpdateScoreUI();
        UpdateTimeUI();
        activeGameMode?.UpdateUI();
    }

    public void PauseGame()
    {
        isGameActive = false;
        Time.timeScale = 0f;

        // 일시정지 UI 표시
        // TODO: 일시정지 패널 구현
    }

    public void ResumeGame()
    {
        isGameActive = true;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 스테이지 선택 화면으로 돌아갑니다.
    /// </summary>
    public void ReturnToStageSelect()
    {
        Time.timeScale = 1f; // 시간 스케일 복원
        GameManager.Instance?.LoadScene("2_Stage");
    }

    /// <summary>
    /// 현재 레벨 데이터를 반환합니다.
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }
}
