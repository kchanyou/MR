using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// �� ���� ������ ���� ���� ������ �����ϴ� ���� ��Ʈ�ѷ�
/// </summary>
public class GameModeController : MonoBehaviour
{
    [Header("UI ����")]
    [SerializeField] private Text levelInfoText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button homeButton;

    [Header("���� ��� ��Ʈ�ѷ���")]
    [SerializeField] private DolphinGameController dolphinController;
    [SerializeField] private PenguinGameController penguinController;
    [SerializeField] private OtamatoneGameController otamatoneController;

    [Header("���� ����")]
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
    /// ������ �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeGame()
    {
        // 2_Stage ������ ������ ���� Ÿ�԰� �������� �ε��� ��������
        string gameType = GameManager.CurrentGameType;
        int selectedStage = PlayerPrefs.GetInt("SelectedStage", 0);

        if (string.IsNullOrEmpty(gameType))
        {
            Debug.LogError("���� Ÿ���� �������� �ʾҽ��ϴ�!");
            ReturnToStageSelect();
            return;
        }

        // LevelManager �ʱ�ȭ
        if (LevelManager.Instance == null)
        {
            GameObject levelManagerObj = new GameObject("LevelManager");
            levelManagerObj.AddComponent<LevelManager>();
        }

        // ���� ��� Ÿ�� ����
        GameModeType modeType = DetermineGameModeType(gameType, selectedStage);

        // ���� ��� ���� �� �������� �ε�
        if (LevelManager.Instance.SetGameMode(modeType, selectedStage))
        {
            LoadCurrentStage();
        }
        else
        {
            Debug.LogError($"���� ��� ���� ����: {modeType}");
            ReturnToStageSelect();
        }
    }

    private GameModeType DetermineGameModeType(string gameType, int stageIndex)
    {
        // �������� �ε����� ������� ���� ��� ����
        // �� ĳ���ʹ� 8�� ��������: 0-3�� ù ��° ���, 4-7�� �� ��° ���
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
    /// ���� �������� �����͸� �ε��ϰ� ������ �����մϴ�.
    /// </summary>
    private void LoadCurrentStage()
    {
        currentLevelData = LevelManager.Instance.GetCurrentStageData();

        if (currentLevelData == null || !currentLevelData.IsValid())
        {
            Debug.LogError("��ȿ���� ���� �������� �������Դϴ�!");
            ReturnToStageSelect();
            return;
        }

        // ������ ���� ��� ��Ʈ�ѷ� Ȱ��ȭ
        SetupGameModeController();

        // UI �ʱ�ȭ
        InitializeUI();

        // ���� ���� �ʱ�ȭ
        currentScore = 0;
        currentQuestionIndex = 0;
        gameStartTime = Time.time;

        Debug.Log($"�������� �ε� �Ϸ�: {currentLevelData.stageName}");

        // ù ��° ���� ����
        LoadNextQuestion();
    }

    private void SetupGameModeController()
    {
        // ��� ��Ʈ�ѷ� ��Ȱ��ȭ
        if (dolphinController != null) dolphinController.gameObject.SetActive(false);
        if (penguinController != null) penguinController.gameObject.SetActive(false);
        if (otamatoneController != null) otamatoneController.gameObject.SetActive(false);

        // ���� ���� ��忡 �´� ��Ʈ�ѷ� Ȱ��ȭ
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

        // ���� ��� �ʱ�ȭ
        activeGameMode?.InitializeGameMode(currentLevelData);
    }

    private void InitializeUI()
    {
        UpdateLevelInfoUI();
        UpdateScoreUI();
        UpdateTimeUI();

        // ��� ����
        if (backgroundImage != null)
        {
            backgroundImage.color = currentLevelData.stageThemeColor;
        }
    }

    /// <summary>
    /// ���� ������ �ε��մϴ�.
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
                Debug.LogError($"��ȿ���� ���� ���� ������: {currentQuestionIndex}");
                CompleteStage();
            }
        }
        else
        {
            CompleteStage();
        }
    }

    /// <summary>
    /// ������ �߰��մϴ�.
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreUI();
    }

    /// <summary>
    /// ���������� �Ϸ��մϴ�.
    /// </summary>
    public void CompleteStage()
    {
        isGameActive = false;
        float playTime = Time.time - gameStartTime;

        // �÷��� �ð� ���
        string characterType = currentLevelData.characterName;
        DataManager.Instance?.AddPlayTime(characterType, playTime);

        // �������� �Ϸ� ó��
        LevelManager.Instance?.CompleteStage(currentScore);

        Debug.Log($"�������� �Ϸ�! ���� ����: {currentScore}");

        // ��� ȭ�� ǥ�� �Ǵ� ���� ���������� ����
        ShowStageCompleteUI();
    }

    private void ShowStageCompleteUI()
    {
        // �������� �Ϸ� UI ǥ��
        // ���⼭ ��� ȭ���� �����ְų� �ٷ� �������� �������� ���ư� �� ����

        // �ӽ÷� 2�� �� �������� �������� ���ư���
        Invoke(nameof(ReturnToStageSelect), 2.0f);
    }

    private void UpdateGameTimer()
    {
        // ����� �ð� ���� ����, �ʿ�� ����
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
            scoreText.text = $"����: {currentScore}";
        }
    }

    private void UpdateTimeUI()
    {
        if (timeText != null)
        {
            timeText.text = $"����: {currentQuestionIndex + 1}/10";
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

        // �Ͻ����� UI ǥ��
        // TODO: �Ͻ����� �г� ����
    }

    public void ResumeGame()
    {
        isGameActive = true;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// �������� ���� ȭ������ ���ư��ϴ�.
    /// </summary>
    public void ReturnToStageSelect()
    {
        Time.timeScale = 1f; // �ð� ������ ����
        GameManager.Instance?.LoadScene("2_Stage");
    }

    /// <summary>
    /// ���� ���� �����͸� ��ȯ�մϴ�.
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }
}
