using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임 모드 타입 열거형
/// </summary>
public enum GameModeType
{
    // 돌고래 모드
    Dolphin_DifferentSound,     // 방울 속 다른 소리 찾기
    Dolphin_MelodyShape,        // 멜로디 모양 맞추기

    // 펭귄 모드  
    Penguin_RhythmJump,         // 박자에 맞춰 점프
    Penguin_RhythmFollow,       // 리듬 따라치기

    // 오타마톤 모드
    Otamatone_DifferentInstrument, // 다른 악기 찾기
    Otamatone_InstrumentMatch      // 악기 맞추기
}

/// <summary>
/// 각 문제의 데이터를 담는 클래스
/// </summary>
[System.Serializable]
public class QuestionData
{
    [Header("문제 기본 정보")]
    public int questionIndex;
    public string questionDescription;

    [Header("오디오 설정")]
    public string[] audioClipNames;        // 재생할 오디오 파일들
    public float[] frequencies;            // 각 오디오의 주파수
    public float[] volumes;                // 각 오디오의 볼륨
    public int correctAnswerIndex;         // 정답 인덱스

    [Header("리듬 게임용 데이터")]
    public float[] rhythmTiming;           // 리듬 타이밍 배열
    public int bpm;                        // BPM (펭귄 모드용)

    [Header("시각적 설정")]
    public int buttonCount = 3;            // 선택지 버튼 개수
    public Color highlightColor = Color.yellow;

    public bool IsValid()
    {
        return audioClipNames != null && audioClipNames.Length > 0;
    }
}

/// <summary>
/// 각 스테이지의 설정 데이터를 담는 클래스
/// </summary>
[System.Serializable]
public class LevelData
{
    [Header("스테이지 기본 정보")]
    public int stageIndex;
    public string stageName;
    public bool isBossStage = false;       // 8번째 스테이지 여부

    [Header("게임 모드")]
    public GameModeType primaryGameMode;   // 주 게임 모드
    public GameModeType[] bossModeMix;     // 보스 스테이지용 혼합 모드

    [Header("문제 구성")]
    public QuestionData[] questions = new QuestionData[10]; // 10문제 고정

    [Header("캐릭터 설정")]
    public string characterName;           // "Dolphin", "Penguin", "Otamatone"
    public string characterMentAudio;      // 캐릭터 멘트 오디오 파일명

    [Header("성과 및 보상")]
    public int targetScore = 80;           // 업적 달성 목표 점수
    public string achievementItemName;     // 업적 아이템 이름
    public string achievementItemIcon;     // 업적 아이템 아이콘

    [Header("오디오 주파수 설정")]
    public float baseFrequency = 440f;     // 기준 주파수
    public float frequencyRange = 1000f;   // 주파수 범위

    [Header("시각적 설정")]
    public Color stageThemeColor = Color.blue;
    public string backgroundImageName;

    public bool IsValid()
    {
        return questions != null && questions.Length == 10 &&
               !string.IsNullOrEmpty(stageName);
    }

    /// <summary>
    /// 보스 스테이지인지 확인
    /// </summary>
    public bool IsBossStage()
    {
        return isBossStage && bossModeMix != null && bossModeMix.Length > 0;
    }

    /// <summary>
    /// 유효한 문제 개수 반환
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
/// 게임 모드별 데이터를 관리하는 클래스
/// </summary>
[System.Serializable]
public class GameModeCollection
{
    [Header("캐릭터 정보")]
    public string characterType;          // "Dolphin", "Penguin", "Otamatone"
    public string displayName;
    public string description;

    [Header("게임 모드별 스테이지")]
    public LevelData[] mode1Stages = new LevelData[8];  // 첫 번째 모드 8스테이지
    public LevelData[] mode2Stages = new LevelData[8];  // 두 번째 모드 8스테이지

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
        return 16; // 각 캐릭터당 2개 모드 × 8스테이지 = 16스테이지
    }
}

/// <summary>
/// 전체 게임 데이터 컨테이너
/// </summary>
[System.Serializable]
public class GameDataContainer
{
    [Header("캐릭터별 게임 모드")]
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
