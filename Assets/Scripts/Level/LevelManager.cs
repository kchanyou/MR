using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ���� �����͸� �ε��ϰ� �����ϴ� �Ŵ��� Ŭ����
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("���� ���� ����")]
    public GameModeType currentGameMode;
    public int currentStageIndex = 0;
    public int currentQuestionIndex = 0;

    public GameModeCollection currentGameModeCollection;

    private GameDataContainer gameDataContainer;
    private LevelData currentLevelData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllGameData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ��� ���� �����͸� �ε��մϴ�.
    /// </summary>
    private void LoadAllGameData()
    {
        try
        {
            TextAsset gameDataFile = Resources.Load<TextAsset>("LevelData/GameDataContainer");

            if (gameDataFile != null)
            {
                gameDataContainer = JsonUtility.FromJson<GameDataContainer>(gameDataFile.text);
                Debug.Log("���� ������ �ε� �Ϸ�");
            }
            else
            {
                Debug.LogError("���� ������ ������ ã�� �� �����ϴ�!");
                CreateDefaultGameData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"���� ������ �ε� ����: {e.Message}");
            CreateDefaultGameData();
        }
    }

    private void CreateDefaultGameData()
    {
        gameDataContainer = new GameDataContainer();
        // �⺻ ������ ������ ���� ����
    }

    /// <summary>
    /// ���� ��带 �����մϴ�.
    /// </summary>
    public bool SetGameMode(GameModeType modeType, int stageIndex = 0)
    {
        currentGameMode = modeType;
        currentStageIndex = stageIndex;
        currentQuestionIndex = 0;

        // ĳ���� Ÿ�� ����
        string characterType = GetCharacterTypeFromMode(modeType);

        // �ش� ĳ������ ���� ��� �÷��� ��������
        currentGameModeCollection = gameDataContainer?.GetCharacterModes(characterType);

        if (currentGameModeCollection != null)
        {
            // ���� �������� �ε����� 0-7 ������ ����
            int adjustedStageIndex = stageIndex % 8;
            currentLevelData = currentGameModeCollection.GetStage(modeType, adjustedStageIndex);

            if (currentLevelData != null)
            {
                Debug.Log($"���� ��� ����: {modeType}, ��������: {adjustedStageIndex}");
                return true;
            }
        }

        Debug.LogError($"�������� �����͸� ã�� �� �����ϴ�: {modeType}, {stageIndex}");
        return false;
    }

    /// <summary>
    /// ���� �������� �����͸� ��ȯ�մϴ�.
    /// </summary>
    public LevelData GetCurrentStageData()
    {
        return currentLevelData;
    }

    /// <summary>
    /// ���� ���� �����͸� ��ȯ�մϴ�.
    /// </summary>
    public QuestionData GetCurrentQuestion()
    {
        if (currentLevelData?.questions != null &&
            currentQuestionIndex >= 0 &&
            currentQuestionIndex < currentLevelData.questions.Length)
        {
            return currentLevelData.questions[currentQuestionIndex];
        }
        return null;
    }

    /// <summary>
    /// ���� ������ �����մϴ�.
    /// </summary>
    public bool NextQuestion()
    {
        if (currentLevelData?.questions != null &&
            currentQuestionIndex < currentLevelData.questions.Length - 1)
        {
            currentQuestionIndex++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// �������� �Ϸ� ó��
    /// </summary>
    public void CompleteStage(int totalScore)
    {
        if (currentLevelData != null)
        {
            string characterType = GetCharacterTypeFromMode(currentGameMode);
            string modeTypeStr = currentGameMode.ToString();

            // DataManager�� ���� ���൵ ����
            DataManager.Instance?.UpdateStageProgress(characterType, modeTypeStr, currentStageIndex % 8, totalScore);

            // ���� �޼� Ȯ��
            if (totalScore >= currentLevelData.targetScore)
            {
                DataManager.Instance?.UnlockAchievement(characterType, modeTypeStr, currentStageIndex % 8);
            }
        }
    }

    /// <summary>
    /// ���� ��忡�� ĳ���� Ÿ�� ����
    /// </summary>
    private string GetCharacterTypeFromMode(GameModeType modeType)
    {
        string modeStr = modeType.ToString();
        if (modeStr.StartsWith("Dolphin")) return "Dolphin";
        if (modeStr.StartsWith("Penguin")) return "Penguin";
        if (modeStr.StartsWith("Otamatone")) return "Otamatone";
        return "";
    }

    /// <summary>
    /// ĳ������ ��� ��� Ÿ�� ��ȯ
    /// </summary>
    public GameModeType[] GetCharacterModes(string characterType)
    {
        switch (characterType)
        {
            case "Dolphin":
                return new GameModeType[] { GameModeType.Dolphin_DifferentSound, GameModeType.Dolphin_MelodyShape };
            case "Penguin":
                return new GameModeType[] { GameModeType.Penguin_RhythmJump, GameModeType.Penguin_RhythmFollow };
            case "Otamatone":
                return new GameModeType[] { GameModeType.Otamatone_DifferentInstrument, GameModeType.Otamatone_InstrumentMatch };
            default:
                return new GameModeType[0];
        }
    }

    /// <summary>
    /// ���ļ� ������ ����� ����� Ŭ���� ��ȯ
    /// </summary>
    public string GetAdjustedAudioClipName(string originalName)
    {
        // DataManager���� ������� ��û ���ļ� ������ �����ͼ�
        // ������ ���ļ� �뿪�� ����� Ŭ������ ��ȯ
        float maxFreq = DataManager.Instance?.gameData.audibleFrequencyMax ?? 20000f;

        // ���ļ��� ���� ����� Ŭ�� ���� ����
        if (maxFreq < 8000f)
            return originalName + "_low";
        else if (maxFreq < 16000f)
            return originalName + "_mid";
        else
            return originalName; // ���� ���
    }
}
