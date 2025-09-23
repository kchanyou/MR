using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    [Header("���൵ ����")]
    // ĳ���ͺ�, ��庰, ���������� ���൵
    public Dictionary<string, Dictionary<string, StageProgress[]>> progressData;

    [Header("���� �ý���")]
    // ĳ���ͺ�, ��庰, ���������� ���� �޼� ����
    public Dictionary<string, Dictionary<string, bool[]>> achievements;

    [Header("Ķ���극�̼� ������")]
    public bool isCalibrated;
    public float audibleFrequencyMax;

    [Header("��ü ���")]
    public PlayerStatistics playerStats;

    [Header("��ü ���� ����")]
    public GlobalGameSettings globalSettings;

    public GameData()
    {
        // 3�� ĳ���� �� 2�� ��� �� 8�� �������� = 48�� ���൵
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

                // �ʱ�ȭ
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
    public int bestScore = 0;              // �ְ� ����
    public int completionCount = 0;        // ���� Ƚ��
    public float bestTime = float.MaxValue; // �ְ� �ð�
    public float totalPlayTime = 0f;       // �� �÷��� �ð�
    public bool isUnlocked = false;        // �ر� ����
    public System.DateTime lastPlayed;     // ������ �÷��� �ð�
}

[System.Serializable]
public class PlayerStatistics
{
    public float totalPlayTime = 0f;       // ��ü �÷��� �ð�
    public int totalGamesPlayed = 0;       // �� ���� �÷��� Ƚ��
    public int totalAchievements = 0;      // �� ���� ����
    public Dictionary<string, int> characterPlayCount; // ĳ���ͺ� �÷��� Ƚ��

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
    [Tooltip("������ ����")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Tooltip("����� ����")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.8f;

    [Tooltip("ȿ���� ����")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Tooltip("���� Ȱ��ȭ")]
    public bool hapticEnabled = true;

    [Tooltip("��� ����")]
    public string language = "KR";
}

/// <summary>
/// Ȯ��� ������ �Ŵ��� - �ΰ��Ϳ� û����Ȱ ���ӿ�
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
                Debug.Log("���� ������ �ε� �Ϸ�");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"������ �ε� ����: {e.Message}");
                gameData = new GameData();
            }
        }
        else
        {
            gameData = new GameData();
            Debug.Log("���ο� ���� ������ ����");
        }
    }

    private void ValidateAndFixData()
    {
        // ������ ���Ἲ �˻� �� ����
        if (gameData.progressData == null || gameData.achievements == null)
        {
            gameData = new GameData();
        }

        // globalSettings ����
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
            Debug.Log($"������ ���� �Ϸ�: {dataFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"������ ���� ����: {e.Message}");
        }
    }

    /// <summary>
    /// �������� ���൵ ������Ʈ
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

            // ���� ������Ʈ
            if (score > progress.bestScore)
            {
                progress.bestScore = score;
            }

            progress.completionCount++;
            progress.lastPlayed = System.DateTime.Now;
            progress.isUnlocked = true;

            // ���� �������� �ر�
            if (stageIndex < 7)
            {
                gameData.progressData[character][mode][stageIndex + 1].isUnlocked = true;
            }

            // ��� ������Ʈ
            gameData.playerStats.totalGamesPlayed++;
            gameData.playerStats.characterPlayCount[character]++;

            SaveData();
            Debug.Log($"���൵ ������Ʈ: {character} {mode} Stage{stageIndex} Score:{score}");
        }
    }

    /// <summary>
    /// ���� �ر�
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
                Debug.Log($"���� �ر�: {character} {mode} Stage{stageIndex}");

                // ���� �ر� �̺�Ʈ (UI �˸� ��)
                OnAchievementUnlocked(character, mode, stageIndex);
            }
        }
    }

    /// <summary>
    /// ���� �ر� �̺�Ʈ
    /// </summary>
    private void OnAchievementUnlocked(string character, string mode, int stageIndex)
    {
        // UI �˸��̳� ȿ�� ó��
        // ���� UI �Ŵ����� ����
    }

    /// <summary>
    /// �������� ���൵ ��ȸ
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
    /// ���� �޼� ���� Ȯ��
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
    /// ĳ���ͺ� �� ���� ���� ��ȯ
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
    /// �÷��� �ð� ����
    /// </summary>
    public void AddPlayTime(string character, float playTime)
    {
        gameData.playerStats.totalPlayTime += playTime;
        SaveData();
    }

    /// <summary>
    /// Ķ���극�̼� ������ ������Ʈ
    /// </summary>
    public void UpdateCalibrationData(float maxFrequency)
    {
        gameData.isCalibrated = true;
        gameData.audibleFrequencyMax = maxFrequency;
        SaveData();
        Debug.Log($"Ķ���극�̼� �Ϸ�: �ִ� ���ļ� {maxFrequency}Hz");
    }

    /// <summary>
    /// ��ü ���� ������ ������Ʈ�մϴ�.
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
