using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    [Header("진행도 관리")]
    public Dictionary<string, Dictionary<string, StageProgress[]>> progressData;

    [Header("업적 시스템")]
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
                progressData[character][mode] = new StageProgress[4];
                achievements[character][mode] = new bool[4];

                for (int i = 0; i < 4; i++)
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
    public int bestScore = 0;
    public int completionCount = 0;
    public float bestTime = float.MaxValue;
    public float totalPlayTime = 0f;
    public bool isUnlocked = false;
    public System.DateTime lastPlayed;
}

[System.Serializable]
public class PlayerStatistics
{
    public float totalPlayTime = 0f;
    public int totalGamesPlayed = 0;
    public int totalAchievements = 0;
    public Dictionary<string, int> characterPlayCount;

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
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float bgmVolume = 0.8f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    public bool hapticEnabled = true;
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
        if (gameData.progressData == null || gameData.achievements == null)
        {
            gameData = new GameData();
        }

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

    public void UpdateStageProgress(string character, string modeType, int stageIndex, int score)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.progressData.ContainsKey(character) &&
            gameData.progressData[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 4)
        {
            var progress = gameData.progressData[character][mode][stageIndex];

            if (score > progress.bestScore)
            {
                progress.bestScore = score;
            }

            progress.completionCount++;
            progress.lastPlayed = System.DateTime.Now;
            progress.isUnlocked = true;

            if (stageIndex < 3)
            {
                gameData.progressData[character][mode][stageIndex + 1].isUnlocked = true;
            }

            gameData.playerStats.totalGamesPlayed++;
            gameData.playerStats.characterPlayCount[character]++;

            SaveData();
        }
    }

    public void UnlockAchievement(string character, string modeType, int stageIndex)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.achievements.ContainsKey(character) &&
            gameData.achievements[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 4)
        {
            if (!gameData.achievements[character][mode][stageIndex])
            {
                gameData.achievements[character][mode][stageIndex] = true;
                gameData.playerStats.totalAchievements++;
                SaveData();
            }
        }
    }

    public StageProgress GetStageProgress(string character, string modeType, int stageIndex)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.progressData.ContainsKey(character) &&
            gameData.progressData[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 4)
        {
            return gameData.progressData[character][mode][stageIndex];
        }
        return new StageProgress();
    }

    public bool IsAchievementUnlocked(string character, string modeType, int stageIndex)
    {
        string mode = modeType.Contains("1") || modeType.Contains("DifferentSound") ||
                      modeType.Contains("RhythmJump") || modeType.Contains("DifferentInstrument")
                      ? "Mode1" : "Mode2";

        if (gameData.achievements.ContainsKey(character) &&
            gameData.achievements[character].ContainsKey(mode) &&
            stageIndex >= 0 && stageIndex < 4)
        {
            return gameData.achievements[character][mode][stageIndex];
        }
        return false;
    }

    public void AddPlayTime(string character, float playTime)
    {
        gameData.playerStats.totalPlayTime += playTime;
        SaveData();
    }

    public void UpdateCalibrationData(float maxFrequency)
    {
        gameData.isCalibrated = true;
        gameData.audibleFrequencyMax = maxFrequency;
        SaveData();
    }

    public void UpdateGlobalSettings(GlobalGameSettings settings)
    {
        gameData.globalSettings = settings;
        SaveData();
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }
}
