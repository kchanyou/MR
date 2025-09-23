using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 업적 표시 및 관리 패널
/// </summary>
public class AchievementPanel : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject achievementPanelRoot;
    [SerializeField] private Button achievementButton;
    [SerializeField] private Button closePanelButton;
    [SerializeField] private Text totalAchievementText;
    [SerializeField] private ScrollRect achievementScrollView;
    [SerializeField] private Transform achievementContainer;

    [Header("업적 아이템 프리팹")]
    [SerializeField] private GameObject achievementItemPrefab;

    [Header("캐릭터별 섹션")]
    [SerializeField] private Text dolphinSectionTitle;
    [SerializeField] private Text penguinSectionTitle;
    [SerializeField] private Text otamatoneSectionTitle;

    private List<AchievementItem> achievementItems;
    private bool isPanelOpen = false;

    private void Awake()
    {
        achievementItems = new List<AchievementItem>();
        SetupButtons();

        if (achievementPanelRoot != null)
        {
            achievementPanelRoot.SetActive(false);
        }
    }

    private void Start()
    {
        RefreshAchievementData();
    }

    private void SetupButtons()
    {
        if (achievementButton != null)
        {
            achievementButton.onClick.AddListener(ToggleAchievementPanel);
        }

        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(CloseAchievementPanel);
        }
    }

    /// <summary>
    /// 업적 패널을 토글합니다.
    /// </summary>
    public void ToggleAchievementPanel()
    {
        if (isPanelOpen)
        {
            CloseAchievementPanel();
        }
        else
        {
            OpenAchievementPanel();
        }
    }

    public void OpenAchievementPanel()
    {
        if (achievementPanelRoot != null)
        {
            achievementPanelRoot.SetActive(true);
            isPanelOpen = true;

            RefreshAchievementData();
            StartCoroutine(PanelOpenAnimation());
        }
    }

    public void CloseAchievementPanel()
    {
        if (achievementPanelRoot != null)
        {
            StartCoroutine(PanelCloseAnimation());
        }
    }

    private IEnumerator PanelOpenAnimation()
    {
        if (achievementPanelRoot == null) yield break;

        achievementPanelRoot.transform.localScale = Vector3.zero;

        float duration = 0.3f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(0f, 1f, t / duration);
            achievementPanelRoot.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        achievementPanelRoot.transform.localScale = Vector3.one;
    }

    private IEnumerator PanelCloseAnimation()
    {
        if (achievementPanelRoot == null) yield break;

        float duration = 0.2f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(1f, 0f, t / duration);
            achievementPanelRoot.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        achievementPanelRoot.SetActive(false);
        isPanelOpen = false;
    }

    /// <summary>
    /// 업적 데이터를 새로고침합니다.
    /// </summary>
    public void RefreshAchievementData()
    {
        ClearAchievementItems();
        LoadAllAchievements();
        UpdateTotalAchievementCount();
    }

    private void ClearAchievementItems()
    {
        foreach (AchievementItem item in achievementItems)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        achievementItems.Clear();
    }

    private void LoadAllAchievements()
    {
        string[] characters = { "Dolphin", "Penguin", "Otamatone" };

        foreach (string character in characters)
        {
            CreateCharacterSection(character);
            LoadCharacterAchievements(character);
        }
    }

    private void CreateCharacterSection(string character)
    {
        // 캐릭터 섹션 제목 생성
        GameObject sectionTitle = new GameObject($"{character}Section");
        sectionTitle.transform.SetParent(achievementContainer);
        sectionTitle.transform.localScale = Vector3.one;

        Text titleText = sectionTitle.AddComponent<Text>();
        titleText.text = GetCharacterDisplayName(character);
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = GetCharacterColor(character);

        // 레이아웃 설정
        ContentSizeFitter fitter = sectionTitle.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void LoadCharacterAchievements(string character)
    {
        string[] modes = { "Mode1", "Mode2" };

        foreach (string mode in modes)
        {
            for (int stageIndex = 0; stageIndex < 8; stageIndex++)
            {
                CreateAchievementItem(character, mode, stageIndex);
            }
        }
    }

    private void CreateAchievementItem(string character, string mode, int stageIndex)
    {
        if (achievementItemPrefab == null || achievementContainer == null)
            return;

        GameObject itemObject = Instantiate(achievementItemPrefab, achievementContainer);
        AchievementItem achievementItem = itemObject.GetComponent<AchievementItem>();

        if (achievementItem == null)
        {
            achievementItem = itemObject.AddComponent<AchievementItem>();
        }

        // 업적 데이터 로드
        LevelData stageData = GetStageData(character, mode, stageIndex);
        bool isUnlocked = DataManager.Instance?.IsAchievementUnlocked(character, mode, stageIndex) ?? false;
        StageProgress progress = DataManager.Instance?.GetStageProgress(character, mode, stageIndex) ?? new StageProgress();

        // 업적 아이템 설정
        AchievementData achievementData = new AchievementData
        {
            characterType = character,
            stageName = stageData?.stageName ?? $"Stage {stageIndex + 1}",
            achievementName = stageData?.achievementItemName ?? "미완성 업적",
            achievementIcon = stageData?.achievementItemIcon ?? "default_achievement",
            isUnlocked = isUnlocked,
            currentScore = progress.bestScore,
            targetScore = stageData?.targetScore ?? 80,
            unlockDate = progress.lastPlayed
        };

        achievementItem.Setup(achievementData);
        achievementItems.Add(achievementItem);
    }

    private LevelData GetStageData(string character, string mode, int stageIndex)
    {
        try
        {
            TextAsset gameDataFile = Resources.Load<TextAsset>("LevelData/GameDataContainer");
            if (gameDataFile != null)
            {
                GameDataContainer container = JsonUtility.FromJson<GameDataContainer>(gameDataFile.text);
                GameModeCollection collection = container?.GetCharacterModes(character);

                if (collection != null)
                {
                    bool isFirstMode = mode == "Mode1";
                    GameModeType modeType = GetGameModeType(character, isFirstMode);
                    return collection.GetStage(modeType, stageIndex);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"스테이지 데이터 로드 실패: {e.Message}");
        }

        return null;
    }

    private GameModeType GetGameModeType(string character, bool isFirstMode)
    {
        switch (character)
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

    private void UpdateTotalAchievementCount()
    {
        if (totalAchievementText != null && DataManager.Instance != null)
        {
            int totalAchievements = DataManager.Instance.gameData.playerStats.totalAchievements;
            totalAchievementText.text = $"달성한 업적: {totalAchievements} / 24";
        }
    }

    private string GetCharacterDisplayName(string character)
    {
        switch (character)
        {
            case "Dolphin": return "🐬 돌고래 업적";
            case "Penguin": return "🐧 펭귄 업적";
            case "Otamatone": return "🎵 오타마톤 업적";
            default: return character;
        }
    }

    private Color GetCharacterColor(string character)
    {
        switch (character)
        {
            case "Dolphin": return new Color(0.2f, 0.7f, 1.0f);
            case "Penguin": return new Color(0.9f, 0.9f, 1.0f);
            case "Otamatone": return new Color(1.0f, 0.6f, 0.2f);
            default: return Color.white;
        }
    }
}

/// <summary>
/// 업적 데이터 구조체
/// </summary>
[System.Serializable]
public struct AchievementData
{
    public string characterType;
    public string stageName;
    public string achievementName;
    public string achievementIcon;
    public bool isUnlocked;
    public int currentScore;
    public int targetScore;
    public System.DateTime unlockDate;
}
