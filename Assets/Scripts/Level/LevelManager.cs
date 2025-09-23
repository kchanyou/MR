using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레벨 데이터를 로드하고 관리하는 매니저 클래스
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("현재 게임 상태")]
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
    /// 모든 게임 데이터를 로드합니다.
    /// </summary>
    private void LoadAllGameData()
    {
        try
        {
            TextAsset gameDataFile = Resources.Load<TextAsset>("LevelData/GameDataContainer");

            if (gameDataFile != null)
            {
                gameDataContainer = JsonUtility.FromJson<GameDataContainer>(gameDataFile.text);
                Debug.Log("게임 데이터 로드 완료");
            }
            else
            {
                Debug.LogError("게임 데이터 파일을 찾을 수 없습니다!");
                CreateDefaultGameData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"게임 데이터 로드 실패: {e.Message}");
            CreateDefaultGameData();
        }
    }

    private void CreateDefaultGameData()
    {
        gameDataContainer = new GameDataContainer();
        // 기본 데이터 생성은 추후 구현
    }

    /// <summary>
    /// 게임 모드를 설정합니다.
    /// </summary>
    public bool SetGameMode(GameModeType modeType, int stageIndex = 0)
    {
        currentGameMode = modeType;
        currentStageIndex = stageIndex;
        currentQuestionIndex = 0;

        // 캐릭터 타입 결정
        string characterType = GetCharacterTypeFromMode(modeType);

        // 해당 캐릭터의 게임 모드 컬렉션 가져오기
        currentGameModeCollection = gameDataContainer?.GetCharacterModes(characterType);

        if (currentGameModeCollection != null)
        {
            // 실제 스테이지 인덱스는 0-7 범위로 조정
            int adjustedStageIndex = stageIndex % 8;
            currentLevelData = currentGameModeCollection.GetStage(modeType, adjustedStageIndex);

            if (currentLevelData != null)
            {
                Debug.Log($"게임 모드 설정: {modeType}, 스테이지: {adjustedStageIndex}");
                return true;
            }
        }

        Debug.LogError($"스테이지 데이터를 찾을 수 없습니다: {modeType}, {stageIndex}");
        return false;
    }

    /// <summary>
    /// 현재 스테이지 데이터를 반환합니다.
    /// </summary>
    public LevelData GetCurrentStageData()
    {
        return currentLevelData;
    }

    /// <summary>
    /// 현재 문제 데이터를 반환합니다.
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
    /// 다음 문제로 진행합니다.
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
    /// 스테이지 완료 처리
    /// </summary>
    public void CompleteStage(int totalScore)
    {
        if (currentLevelData != null)
        {
            string characterType = GetCharacterTypeFromMode(currentGameMode);
            string modeTypeStr = currentGameMode.ToString();

            // DataManager를 통해 진행도 저장
            DataManager.Instance?.UpdateStageProgress(characterType, modeTypeStr, currentStageIndex % 8, totalScore);

            // 업적 달성 확인
            if (totalScore >= currentLevelData.targetScore)
            {
                DataManager.Instance?.UnlockAchievement(characterType, modeTypeStr, currentStageIndex % 8);
            }
        }
    }

    /// <summary>
    /// 게임 모드에서 캐릭터 타입 추출
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
    /// 캐릭터의 모든 모드 타입 반환
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
    /// 주파수 조절이 적용된 오디오 클립명 반환
    /// </summary>
    public string GetAdjustedAudioClipName(string originalName)
    {
        // DataManager에서 사용자의 가청 주파수 설정을 가져와서
        // 적절한 주파수 대역의 오디오 클립명을 반환
        float maxFreq = DataManager.Instance?.gameData.audibleFrequencyMax ?? 20000f;

        // 주파수에 따른 오디오 클립 변형 로직
        if (maxFreq < 8000f)
            return originalName + "_low";
        else if (maxFreq < 16000f)
            return originalName + "_mid";
        else
            return originalName; // 원본 사용
    }
}
