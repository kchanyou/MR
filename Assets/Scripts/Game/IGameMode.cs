using UnityEngine;

/// <summary>
/// ���� ��庰 ���� ������ �����ϴ� �������̽�
/// </summary>
public interface IGameMode
{
    /// <summary>
    /// ���� ��� �ʱ�ȭ
    /// </summary>
    void InitializeGameMode(LevelData levelData);

    /// <summary>
    /// ���� ����
    /// </summary>
    void StartQuestion(QuestionData questionData);

    /// <summary>
    /// ����� �Է� ó��
    /// </summary>
    void ProcessUserInput(int inputIndex);

    /// <summary>
    /// ���� ���� ó��
    /// </summary>
    void EndQuestion(bool isCorrect);

    /// <summary>
    /// UI ������Ʈ
    /// </summary>
    void UpdateUI();
}
