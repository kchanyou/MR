using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ���� ��� Ÿ�� ������
/// </summary>
public enum GameModeType
{
    // ���� ���
    Dolphin_DifferentSound,     // ��� �� �ٸ� �Ҹ� ã��
    Dolphin_MelodyShape,        // ��ε� ��� ���߱�

    // ��� ���  
    Penguin_RhythmJump,         // ���ڿ� ���� ����
    Penguin_RhythmFollow,       // ���� ����ġ��

    // ��Ÿ���� ���
    Otamatone_DifferentInstrument, // �ٸ� �Ǳ� ã��
    Otamatone_InstrumentMatch      // �Ǳ� ���߱�
}

/// <summary>
/// �� ������ �����͸� ��� Ŭ����
/// </summary>
[System.Serializable]
public class QuestionData
{
    [Header("���� �⺻ ����")]
    public int questionIndex;
    public string questionDescription;

    [Header("����� ����")]
    public string[] audioClipNames;        // ����� ����� ���ϵ�
    public float[] frequencies;            // �� ������� ���ļ�
    public float[] volumes;                // �� ������� ����
    public int correctAnswerIndex;         // ���� �ε���

    [Header("���� ���ӿ� ������")]
    public float[] rhythmTiming;           // ���� Ÿ�̹� �迭
    public int bpm;                        // BPM (��� ����)

    [Header("�ð��� ����")]
    public int buttonCount = 3;            // ������ ��ư ����
    public Color highlightColor = Color.yellow;

    public bool IsValid()
    {
        return audioClipNames != null && audioClipNames.Length > 0;
    }
}

/// <summary>
/// �� ���������� ���� �����͸� ��� Ŭ����
/// </summary>
[System.Serializable]
public class LevelData
{
    [Header("�������� �⺻ ����")]
    public int stageIndex;
    public string stageName;
    public bool isBossStage = false;       // 8��° �������� ����

    [Header("���� ���")]
    public GameModeType primaryGameMode;   // �� ���� ���
    public GameModeType[] bossModeMix;     // ���� ���������� ȥ�� ���

    [Header("���� ����")]
    public QuestionData[] questions = new QuestionData[10]; // 10���� ����

    [Header("ĳ���� ����")]
    public string characterName;           // "Dolphin", "Penguin", "Otamatone"
    public string characterMentAudio;      // ĳ���� ��Ʈ ����� ���ϸ�

    [Header("���� �� ����")]
    public int targetScore = 80;           // ���� �޼� ��ǥ ����
    public string achievementItemName;     // ���� ������ �̸�
    public string achievementItemIcon;     // ���� ������ ������

    [Header("����� ���ļ� ����")]
    public float baseFrequency = 440f;     // ���� ���ļ�
    public float frequencyRange = 1000f;   // ���ļ� ����

    [Header("�ð��� ����")]
    public Color stageThemeColor = Color.blue;
    public string backgroundImageName;

    public bool IsValid()
    {
        return questions != null && questions.Length == 10 &&
               !string.IsNullOrEmpty(stageName);
    }

    /// <summary>
    /// ���� ������������ Ȯ��
    /// </summary>
    public bool IsBossStage()
    {
        return isBossStage && bossModeMix != null && bossModeMix.Length > 0;
    }

    /// <summary>
    /// ��ȿ�� ���� ���� ��ȯ
    /// </summary>
    public int GetValidQuestionCount()
    {
        int count = 0;
        foreach (var question in questions)
        {
            if (question != null && question.IsValid())
                count++;
        }
        return count;
    }
}

/// <summary>
/// ���� ��庰 �����͸� �����ϴ� Ŭ����
/// </summary>
[System.Serializable]
public class GameModeCollection
{
    [Header("ĳ���� ����")]
    public string characterType;          // "Dolphin", "Penguin", "Otamatone"
    public string displayName;
    public string description;

    [Header("���� ��庰 ��������")]
    public LevelData[] mode1Stages = new LevelData[8];  // ù ��° ��� 8��������
    public LevelData[] mode2Stages = new LevelData[8];  // �� ��° ��� 8��������

    public LevelData GetStage(GameModeType modeType, int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= 8) return null;

        switch (modeType)
        {
            case GameModeType.Dolphin_DifferentSound:
            case GameModeType.Penguin_RhythmJump:
            case GameModeType.Otamatone_DifferentInstrument:
                return mode1Stages?[stageIndex];

            case GameModeType.Dolphin_MelodyShape:
            case GameModeType.Penguin_RhythmFollow:
            case GameModeType.Otamatone_InstrumentMatch:
                return mode2Stages?[stageIndex];

            default:
                return null;
        }
    }

    public int GetTotalStages()
    {
        return 16; // �� ĳ���ʹ� 2�� ��� �� 8�������� = 16��������
    }
}

/// <summary>
/// ��ü ���� ������ �����̳�
/// </summary>
[System.Serializable]
public class GameDataContainer
{
    [Header("ĳ���ͺ� ���� ���")]
    public GameModeCollection dolphinModes;
    public GameModeCollection penguinModes;
    public GameModeCollection otamatoneModes;

    public GameModeCollection GetCharacterModes(string characterType)
    {
        switch (characterType)
        {
            case "Dolphin": return dolphinModes;
            case "Penguin": return penguinModes;
            case "Otamatone": return otamatoneModes;
            default: return null;
        }
    }

    public LevelData GetStageData(GameModeType modeType, int stageIndex)
    {
        GameModeCollection collection = null;

        if (modeType.ToString().StartsWith("Dolphin"))
            collection = dolphinModes;
        else if (modeType.ToString().StartsWith("Penguin"))
            collection = penguinModes;
        else if (modeType.ToString().StartsWith("Otamatone"))
            collection = otamatoneModes;

        return collection?.GetStage(modeType, stageIndex);
    }
}
