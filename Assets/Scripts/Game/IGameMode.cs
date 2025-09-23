using UnityEngine;

/// <summary>
/// 게임 모드별 고유 로직을 정의하는 인터페이스
/// </summary>
public interface IGameMode
{
    /// <summary>
    /// 게임 모드 초기화
    /// </summary>
    void InitializeGameMode(LevelData levelData);

    /// <summary>
    /// 문제 시작
    /// </summary>
    void StartQuestion(QuestionData questionData);

    /// <summary>
    /// 사용자 입력 처리
    /// </summary>
    void ProcessUserInput(int inputIndex);

    /// <summary>
    /// 문제 종료 처리
    /// </summary>
    void EndQuestion(bool isCorrect);

    /// <summary>
    /// UI 업데이트
    /// </summary>
    void UpdateUI();
}
