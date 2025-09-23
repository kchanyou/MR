using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ������ �������� ��� - ���� ǥ�� �� �� ���� ����
/// </summary>
public class StageNode : MonoBehaviour
{
    [Header("UI ������Ʈ")]
    [SerializeField] private Button nodeButton;
    [SerializeField] private Image nodeBackground;
    [SerializeField] private Image stageIcon;
    [SerializeField] private Text stageNumberText;
    [SerializeField] private Text stageNameText;
    [SerializeField] private Image difficultyStars;
    [SerializeField] private Slider progressSlider;

    [Header("���� ǥ��")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject clearStamp;
    [SerializeField] private Image achievementBadge;
    [SerializeField] private Text bestScoreText;

    [Header("���� ����")]
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color clearedColor = Color.green;
    [SerializeField] private Color currentColor = Color.yellow;

    // �������� ����
    private StageMapManager stageMapManager;
    private int stageIndex;
    private NodeState nodeState;
    private LevelData stageData;
    private bool hasAchievement = false;

    /// <summary>
    /// �������� ��带 �ʱ�ȭ�մϴ�.
    /// </summary>
    public void Setup(StageMapManager manager, int index, NodeState state)
    {
        stageMapManager = manager;
        stageIndex = index;
        nodeState = state;

        LoadStageData();
        UpdateNodeVisuals();
        SetupButton();

        Debug.Log($"StageNode {index} ���� �Ϸ�: {state}");
    }

    /// <summary>
    /// �������� �����͸� �ε��մϴ�.
    /// </summary>
    private void LoadStageData()
    {
        string currentGameType = GameManager.CurrentGameType;

        if (!string.IsNullOrEmpty(currentGameType))
        {
            stageData = GetStageDataFromContainer(currentGameType, stageIndex);

            if (stageData != null)
            {
                CheckAchievementStatus(currentGameType, stageIndex);
            }
        }
    }

    private LevelData GetStageDataFromContainer(string gameType, int index)
    {
        try
        {
            TextAsset gameDataFile = Resources.Load<TextAsset>("LevelData/GameDataContainer");
            if (gameDataFile != null)
            {
                GameDataContainer container = JsonUtility.FromJson<GameDataContainer>(gameDataFile.text);
                GameModeCollection collection = container?.GetCharacterModes(gameType);

                if (collection != null)
                {
                    bool isFirstMode = index < 4;  // 0-3: Mode1, 4-7: Mode2
                    int adjustedIndex = index % 4;
                    GameModeType modeType = GetGameModeType(gameType, isFirstMode);

                    return collection.GetStage(modeType, adjustedIndex);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�������� ������ �ε� ����: {e.Message}");
        }

        return null;
    }

    private GameModeType GetGameModeType(string gameType, bool isFirstMode)
    {
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

    private void CheckAchievementStatus(string gameType, int index)
    {
        if (DataManager.Instance != null)
        {
            bool isFirstMode = index < 4;
            int adjustedIndex = index % 4;
            string modeTypeStr = isFirstMode ? "Mode1" : "Mode2";

            hasAchievement = DataManager.Instance.IsAchievementUnlocked(gameType, modeTypeStr, adjustedIndex);
        }
    }

    /// <summary>
    /// ����� �ð��� ��Ҹ� ������Ʈ�մϴ�.
    /// </summary>
    private void UpdateNodeVisuals()
    {
        UpdateBasicInfo();
        UpdateStateVisuals();
        UpdateProgressInfo();
        UpdateAchievementBadge();
    }

    private void UpdateBasicInfo()
    {
        // �������� ��ȣ
        if (stageNumberText != null)
        {
            stageNumberText.text = (stageIndex + 1).ToString();
        }

        // �������� �̸�
        if (stageNameText != null && stageData != null)
        {
            stageNameText.text = stageData.stageName;
        }
        else if (stageNameText != null)
        {
            stageNameText.text = $"Stage {stageIndex + 1}";
        }

        // �������� ������
        if (stageIcon != null && stageData != null)
        {
            UpdateStageIcon();
        }

        // ���̵� ǥ��
        if (difficultyStars != null && stageData != null)
        {
            UpdateDifficultyDisplay();
        }
    }

    private void UpdateStageIcon()
    {
        if (stageIcon == null || stageData == null) return;

        Color iconColor = Color.white;

        switch (stageData.primaryGameMode)
        {
            case GameModeType.Dolphin_DifferentSound:
            case GameModeType.Dolphin_MelodyShape:
                iconColor = new Color(0.2f, 0.7f, 1.0f);
                break;
            case GameModeType.Penguin_RhythmJump:
            case GameModeType.Penguin_RhythmFollow:
                iconColor = new Color(0.9f, 0.9f, 1.0f);
                break;
            case GameModeType.Otamatone_DifferentInstrument:
            case GameModeType.Otamatone_InstrumentMatch:
                iconColor = new Color(1.0f, 0.6f, 0.2f);
                break;
        }

        stageIcon.color = iconColor;
    }

    private void UpdateDifficultyDisplay()
    {
        if (difficultyStars == null || stageData == null) return;

        int adjustedIndex = stageIndex % 4;
        float difficulty = 1.0f + (adjustedIndex * 0.75f); // 1.0 ~ 3.25

        float fillAmount = difficulty / 4.0f;
        difficultyStars.fillAmount = fillAmount;
    }

    private void UpdateStateVisuals()
    {
        Color backgroundColor = unlockedColor;
        bool isInteractable = true;

        switch (nodeState)
        {
            case NodeState.Locked:
                backgroundColor = lockedColor;
                isInteractable = false;
                if (lockedOverlay != null) lockedOverlay.SetActive(true);
                if (clearStamp != null) clearStamp.SetActive(false);
                break;

            case NodeState.Unlocked:
                backgroundColor = unlockedColor;
                isInteractable = true;
                if (lockedOverlay != null) lockedOverlay.SetActive(false);
                if (clearStamp != null) clearStamp.SetActive(false);
                break;

            case NodeState.Cleared:
                backgroundColor = clearedColor;
                isInteractable = true;
                if (lockedOverlay != null) lockedOverlay.SetActive(false);
                if (clearStamp != null) clearStamp.SetActive(true);
                break;
        }

        if (nodeBackground != null)
        {
            nodeBackground.color = backgroundColor;
        }

        if (nodeButton != null)
        {
            nodeButton.interactable = isInteractable;
        }
    }

    private void UpdateProgressInfo()
    {
        if (DataManager.Instance == null) return;

        string gameType = GameManager.CurrentGameType;
        bool isFirstMode = stageIndex < 4;
        int adjustedIndex = stageIndex % 4;
        string modeTypeStr = isFirstMode ? "Mode1" : "Mode2";

        StageProgress progress = DataManager.Instance.GetStageProgress(gameType, modeTypeStr, adjustedIndex);

        // �ְ� ���� ǥ��
        if (bestScoreText != null)
        {
            if (progress.bestScore > 0)
            {
                bestScoreText.text = $"�ְ�: {progress.bestScore}��";
                bestScoreText.gameObject.SetActive(true);
            }
            else
            {
                bestScoreText.gameObject.SetActive(false);
            }
        }

        // ���൵ �����̴�
        if (progressSlider != null && stageData != null)
        {
            if (progress.bestScore > 0)
            {
                float progressValue = (float)progress.bestScore / (float)stageData.targetScore;
                progressSlider.value = Mathf.Clamp01(progressValue);
                progressSlider.gameObject.SetActive(true);
            }
            else
            {
                progressSlider.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateAchievementBadge()
    {
        if (achievementBadge != null)
        {
            achievementBadge.gameObject.SetActive(hasAchievement);

            if (hasAchievement)
            {
                StartCoroutine(AchievementBadgeGlow());
            }
        }
    }

    private IEnumerator AchievementBadgeGlow()
    {
        if (achievementBadge == null) yield break;

        Color originalColor = achievementBadge.color;
        float glowDuration = 2.0f;

        while (hasAchievement && achievementBadge.gameObject.activeInHierarchy)
        {
            // ���̵� ��
            for (float t = 0; t < glowDuration / 2; t += Time.deltaTime)
            {
                float alpha = Mathf.Lerp(0.6f, 1.0f, t / (glowDuration / 2));
                achievementBadge.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            // ���̵� �ƿ�
            for (float t = 0; t < glowDuration / 2; t += Time.deltaTime)
            {
                float alpha = Mathf.Lerp(1.0f, 0.6f, t / (glowDuration / 2));
                achievementBadge.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
        }
    }

    private void SetupButton()
    {
        if (nodeButton != null)
        {
            nodeButton.onClick.RemoveAllListeners();
            nodeButton.onClick.AddListener(OnNodeClicked);
        }
    }

    /// <summary>
    /// �������� ��� Ŭ�� �� ȣ��Ǵ� �Լ�
    /// </summary>
    public void OnNodeClicked()
    {
        if (nodeState == NodeState.Locked)
        {
            ShowLockedStageMessage();
            return;
        }

        StartCoroutine(ClickEffect());
        stageMapManager?.OnStageSelected(stageIndex);
    }

    private void ShowLockedStageMessage()
    {
        Debug.Log("�� ���������� ���� ����ֽ��ϴ�!");
        // TODO: UI �˸� ǥ��
    }

    private IEnumerator ClickEffect()
    {
        if (nodeBackground == null) yield break;

        Vector3 originalScale = transform.localScale;

        // ������ �ִϸ��̼�
        for (float t = 0; t < 0.1f; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(1.0f, 1.1f, t / 0.1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        for (float t = 0; t < 0.1f; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(1.1f, 1.0f, t / 0.1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// ��� ������ ������Ʈ�մϴ� (�ܺο��� ȣ��)
    /// </summary>
    public void RefreshNode()
    {
        LoadStageData();
        UpdateNodeVisuals();
    }

    /// <summary>
    /// ���� �޼� �� ȣ��Ǵ� �Լ�
    /// </summary>
    public void OnAchievementUnlocked()
    {
        hasAchievement = true;
        UpdateAchievementBadge();
        StartCoroutine(AchievementUnlockEffect());
    }

    private IEnumerator AchievementUnlockEffect()
    {
        Debug.Log($"�������� {stageIndex + 1} ���� �޼�!");
        // TODO: ��ƼŬ �ý����̳� ��Ÿ �ð��� ȿ�� ����
        yield return null;
    }
}

/// <summary>
/// ��� ���� ������
/// </summary>
public enum NodeState
{
    Locked,
    Unlocked,
    Cleared
}
