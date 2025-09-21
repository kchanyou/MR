using UnityEngine;
using System.IO;
using System.Collections.Generic;

// ������ �������� ������ �����ϴ� Ŭ�����Դϴ�.
[System.Serializable]
public class GameData
{
    // ���Ӻ� ���൵
    public Dictionary<string, int> highestClearedStages;

    // --- �߰��� Ķ���극�̼� ������ ---
    [Tooltip("���ļ� Ķ���극�̼��� �����ߴ��� ����")]
    public bool isCalibrated;
    [Tooltip("����ڰ� ���� �� �ִ� �ִ� ���ļ� ��")]
    public float audibleFrequencyMax;
    // --- ---

    // GameData�� ó�� ������ �� �ʱⰪ�� �����մϴ�.
    public GameData()
    {
        highestClearedStages = new Dictionary<string, int>
        {
            { "Dolphin", -1 },
            { "Penguin", -1 },
            { "Automaton", -1 }
        };

        // Ķ���극�̼� ������ �ʱ�ȭ
        isCalibrated = false;
        audibleFrequencyMax = 20000f; // �⺻�� (�ִ� ��û ���ļ�)
    }
}

/// <summary>
/// ���� �����͸� ���Ϸ� �����ϰ� �ҷ����� ������ ����ϴ� �̱��� �Ŵ����Դϴ�.
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
            dataFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
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
            string json = File.ReadAllText(dataFilePath);
            // JsonUtility�� Dictionary�� ���� ��ȯ���� ���ϹǷ�, �� �κ��� ���� ���� �� ���ǰ� �ʿ��մϴ�.
            // ����� ���� ������ ���� �״�� ������, ������ �߻��ϸ� ������ �ʿ��� �� �ֽ��ϴ�.
            gameData = JsonUtility.FromJson<GameData>(json);

            // ������ ������ ������ ������ ���� �ʴ� ��츦 ����� ������ġ
            if (gameData == null)
            {
                gameData = new GameData();
            }
            if (gameData.highestClearedStages == null)
            {
                gameData.highestClearedStages = new Dictionary<string, int>
                {
                    { "Dolphin", -1 }, { "Penguin", -1 }, { "Automaton", -1 }
                };
            }
        }
        else
        {
            gameData = new GameData();
        }
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(dataFilePath, json);
        Debug.Log($"������ ���� �Ϸ�: {dataFilePath}");
    }

    public int GetHighestClearedStage(string gameType)
    {
        if (gameData.highestClearedStages.ContainsKey(gameType))
        {
            return gameData.highestClearedStages[gameType];
        }
        return -1;
    }

    public void UpdateHighestClearedStage(string gameType, int stageIndex)
    {
        if (gameData.highestClearedStages.ContainsKey(gameType) && stageIndex > gameData.highestClearedStages[gameType])
        {
            gameData.highestClearedStages[gameType] = stageIndex;
            SaveData();
        }
    }

    /// <summary>
    /// Ķ���극�̼� ����� �����ϴ� �Լ�
    /// </summary>
    public void UpdateCalibrationData(float maxFrequency)
    {
        gameData.isCalibrated = true;
        gameData.audibleFrequencyMax = maxFrequency;
        SaveData();
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }
}

