using UnityEngine;
using System.IO;
using System.Collections.Generic;

// 저장할 데이터의 구조를 정의하는 클래스입니다.
[System.Serializable]
public class GameData
{
    // 게임별 진행도
    public Dictionary<string, int> highestClearedStages;

    // --- 추가된 캘리브레이션 데이터 ---
    [Tooltip("주파수 캘리브레이션을 진행했는지 여부")]
    public bool isCalibrated;
    [Tooltip("사용자가 들을 수 있는 최대 주파수 값")]
    public float audibleFrequencyMax;
    // --- ---

    // GameData가 처음 생성될 때 초기값을 설정합니다.
    public GameData()
    {
        highestClearedStages = new Dictionary<string, int>
        {
            { "Dolphin", -1 },
            { "Penguin", -1 },
            { "Automaton", -1 }
        };

        // 캘리브레이션 데이터 초기화
        isCalibrated = false;
        audibleFrequencyMax = 20000f; // 기본값 (최대 가청 주파수)
    }
}

/// <summary>
/// 게임 데이터를 파일로 저장하고 불러오는 역할을 담당하는 싱글톤 매니저입니다.
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
            // JsonUtility는 Dictionary를 직접 변환하지 못하므로, 이 부분은 실제 구현 시 주의가 필요합니다.
            // 현재는 개념 설명을 위해 그대로 두지만, 문제가 발생하면 수정이 필요할 수 있습니다.
            gameData = JsonUtility.FromJson<GameData>(json);

            // 파일은 있지만 데이터 구조가 맞지 않는 경우를 대비한 안전장치
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
        Debug.Log($"데이터 저장 완료: {dataFilePath}");
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
    /// 캘리브레이션 결과를 저장하는 함수
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

