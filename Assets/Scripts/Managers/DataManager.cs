using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    [Header("진행도 관리")]
    // 캐릭터별, 모드별, 스테이지별 진행도
    public Dictionary<string, Dictionary<string, StageProgress[]>> progressData;

    [Header("업적 시스템")]
    // 캐릭터별, 모드별, 스테이지별 업적 달성 여부
    public Dictionary<string, Dictionary<string, bool[]>> achievements;

    [Header("캘리브레이션 데이터")]
    public bool isCalibrated;
    public float audibleFrequencyMax;

    [Header("전체 통계")]
    public PlayerStatistics playerStats;

    [Header("전체 게임 설정")]
    public GlobalGameSettings globalSettings;

    public GameData()
    {
        // 3개 캐릭터 × 2개 모드 × 8개 스테이지 = 48개 진행도
        progressData = new Dictionary<string, Dictionary<string, StageProgress[]>>();
        achievements = new Dictionary<string, Dictionary<string, bool[]>>();

        string[] characters = { "Dolphin", "Penguin", "Otamatone" };
        string[] modes = { "Mode1", "Mode2" };

        foreach (string character in characters)
        {
            progressData[character] = new Dictionary<string, StageProgress[]>();
            achievements[character] = new Dictionary<string, bool[]>();

            foreach (string mode in modes)
            {
                progressData[character][mode] = new StageProgress[8];
                achievements[character][mode] = new bool[8];

                // 초기화
                for (int i = 0; i < 8; i++)
                {
                    progressData[character][mode][i] = new StageProgress();
                    achievements[character][mode][i] = false;
                }
            }
        }

        isCalibrated = false;
        audibleFrequencyMax = 20000f;
        playerStats = new PlayerStatistics();
        globalSettings = new GlobalGameSettings();
    }
}

[System.Serializable]
public class StageProgress
{
    public int bestScore = 0;              // 최고 점수
    public int completionCount = 0;        // 완주 횟수
    public float bestTime = float.MaxValue; // 최고 시간
    public float totalPlayTime = 0f;       // 총 플레이 시간
    public bool isUnlocked = false;        // 해금 여부
    public System.DateTime lastPlayed;     // 마지막 플레이 시간
}

[System.Serializable]
public class PlayerStatistics
{
    public float totalPlayTime = 0f;       // 전체 플레이 시간
    public int totalGamesPlayed = 0;       // 총 게임 플레이 횟수
    public int totalAchievements = 0;      // 총 업적 개수
    public Dictionary<string, int> characterPlayCount; // 캐릭터별 플레이 횟수

    public PlayerStatistics()
    {
        characterPlayCount = new Dictionary<string, int>
        {
            { "Dolphin", 0 },
            { "Penguin", 0 },
            { "Otamatone", 0 }
        };
    }
}

[System.Serializable]
public class GlobalGameSettings
{
    [Tooltip("마스터 볼륨")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Tooltip("배경음 볼륨")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.8f;

    [Tooltip("효과음 볼륨")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Tooltip("진동 활성화")]
    public bool hapticEnabled = true;

    [Tooltip("언어 설정")]
    public string language = "KR";
}

/// <summary>
/// 확장된 데이터 매니저 - 인공와우 청능재활 게임용
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public GameData gameData;
    private string dataFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            dataFilePath = Path.Combine(Application.persistentDataPath, "cochlear_game_data.json");
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadData()
    {
        if (File.Exists(dataFilePath))
        {
            try
            {
                string json = File.ReadAllText(dataFilePath);
                gameData = JsonUtility.FromJson<GameData>(json);

                if (gameData == null)
                {
                    gameData = new GameData();
                }

                ValidateAndFixData();
                Debug.Log("게임 데이터 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"데이터 로드 실패: {e.Message}");
                gameData = new GameData();
            }
        }
        else
        {
            gameData = new GameData();
            Debug.Log("새로운 게임 데이터 생성");
        }
    }

    private void ValidateAndFixData()
    {
        // 데이터 무결성 검사 및 복구
        if (gameData.progressData == null || gameData.achievements == null)
        {
            gameData = new GameData();
        }

        // globalSettings 검증
        if (gameData.globalSettings == null)
        {
            gameData.globalSettings = new GlobalGameSettings();
        }
    }

    public void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(dataFilePath, json);
            Debug.Log($"데이터 저장 완료: {dataFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"데이터 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 스테이지 진행도 업데이트
    /// </summary>
    public void UpdateStageProgress(string character, string modeType, int stageIndex, int score)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.progressData.ContainsKey(character) &&
            gameData.progressData[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 8)
        {
            var progress = gameData.progressData[character][mode][stageIndex];

            // 점수 업데이트
            if (score > progress.bestScore)
            {
                progress.bestScore = score;
            }

            progress.completionCount++;
            progress.lastPlayed = System.DateTime.Now;
            progress.isUnlocked = true;

            // 다음 스테이지 해금
            if (stageIndex < 7)
            {
                gameData.progressData[character][mode][stageIndex + 1].isUnlocked = true;
            }

            // 통계 업데이트
            gameData.playerStats.totalGamesPlayed++;
            gameData.playerStats.characterPlayCount[character]++;

            SaveData();
            Debug.Log($"진행도 업데이트: {character} {mode} Stage{stageIndex} Score:{score}");
        }
    }

    /// <summary>
    /// 업적 해금
    /// </summary>
    public void UnlockAchievement(string character, string modeType, int stageIndex)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.achievements.ContainsKey(character) &&
            gameData.achievements[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 8)
        {
            if (!gameData.achievements[character][mode][stageIndex])
            {
                gameData.achievements[character][mode][stageIndex] = true;
                gameData.playerStats.totalAchievements++;

                SaveData();
                Debug.Log($"업적 해금: {character} {mode} Stage{stageIndex}");

                // 업적 해금 이벤트 (UI 알림 등)
                OnAchievementUnlocked(character, mode, stageIndex);
            }
        }
    }

    /// <summary>
    /// 업적 해금 이벤트
    /// </summary>
    private void OnAchievementUnlocked(string character, string mode, int stageIndex)
    {
        // UI 알림이나 효과 처리
        // 추후 UI 매니저와 연동
    }

    /// <summary>
    /// 스테이지 진행도 조회
    /// </summary>
    public StageProgress GetStageProgress(string character, string modeType, int stageIndex)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.progressData.ContainsKey(character) &&
            gameData.progressData[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 8)
        {
            return gameData.progressData[character][mode][stageIndex];
        }
        return new StageProgress();
    }

    /// <summary>
    /// 업적 달성 여부 확인
    /// </summary>
    public bool IsAchievementUnlocked(string character, string modeType, int stageIndex)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.achievements.ContainsKey(character) &&
            gameData.achievements[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 8)
        {
            return gameData.achievements[character][mode][stageIndex];
        }
        return false;
    }

    /// <summary>
    /// 캐릭터별 총 업적 개수 반환
    /// </summary>
    public int GetCharacterAchievements(string character)
    {
        int count = 0;
        if (gameData.achievements.ContainsKey(character))
        {
            foreach (var mode in gameData.achievements[character])
            {
                foreach (bool achieved in mode.Value)
                {
                    if (achieved) count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 플레이 시간 누적
    /// </summary>
    public void AddPlayTime(string character, float playTime)
    {
        gameData.playerStats.totalPlayTime += playTime;
        SaveData();
    }

    /// <summary>
    /// 캘리브레이션 데이터 업데이트
    /// </summary>
    public void UpdateCalibrationData(float maxFrequency)
    {
        gameData.isCalibrated = true;
        gameData.audibleFrequencyMax = maxFrequency;
        SaveData();
        Debug.Log($"캘리브레이션 완료: 최대 주파수 {maxFrequency}Hz");
    }

    /// <summary>
    /// 전체 게임 설정을 업데이트합니다.
    /// </summary>
    public void UpdateGlobalSettings(GlobalGameSettings settings)
    {
        gameData.globalSettings = settings;
        SaveData();
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveData();
    }
}
