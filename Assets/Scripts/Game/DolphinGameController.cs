using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ���� ���� ��� ��Ʈ�ѷ� - ��� �� �ٸ� �Ҹ� ã�� & ��ε� ��� ���߱�
/// </summary>
public class DolphinGameController : MonoBehaviour, IGameMode
{
    [Header("UI ����")]
    [SerializeField] private Button[] bubbleButtons = new Button[3];        // ����� ��ư��
    [SerializeField] private Button[] directionButtons = new Button[2];     // ���� ���� ��ư�� (���/�ϰ�)
    [SerializeField] private Image[] bubbleHighlights = new Image[3];       // ��ư ���̶���Ʈ
    [SerializeField] private Text instructionText;                          // ���û��� �ؽ�Ʈ
    [SerializeField] private Text questionCountText;                        // ���� ��ȣ ǥ��
    [SerializeField] private GameObject bubbleContainer;                    // ����� �����̳�
    [SerializeField] private GameObject directionContainer;                 // ���� ��ư �����̳�

    [Header("�ð��� ȿ��")]
    [SerializeField] private ParticleSystem correctEffect;                  // ���� ȿ��
    [SerializeField] private ParticleSystem wrongEffect;                    // ���� ȿ��
    [SerializeField] private Animator dolphinAnimator;                      // ���� �ִϸ�����

    [Header("���� ����")]
    [SerializeField] private Color normalBubbleColor = Color.white;
    [SerializeField] private Color highlightBubbleColor = Color.cyan;
    [SerializeField] private Color correctBubbleColor = Color.green;
    [SerializeField] private Color wrongBubbleColor = Color.red;

    private GameModeController gameController;
    private LevelData currentLevelData;
    private QuestionData currentQuestionData;
    private bool isWaitingForAnswer = false;
    private bool isPlayingSequence = false;
    private int currentQuestionIndex = 0;

    private void Awake()
    {
        gameController = GetComponent<GameModeController>();
        SetupButtons();
    }

    private void SetupButtons()
    {
        // ����� ��ư ����
        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            int buttonIndex = i; // Ŭ���� ���� �ذ�
            if (bubbleButtons[i] != null)
            {
                bubbleButtons[i].onClick.AddListener(() => OnBubbleButtonClick(buttonIndex));
            }
        }

        // ���� ��ư ����
        for (int i = 0; i < directionButtons.Length; i++)
        {
            int buttonIndex = i;
            if (directionButtons[i] != null)
            {
                directionButtons[i].onClick.AddListener(() => OnDirectionButtonClick(buttonIndex));
            }
        }
    }

    #region IGameMode ����

    public void InitializeGameMode(LevelData levelData)
    {
        currentLevelData = levelData;
        currentQuestionIndex = 0;

        // ���� ��忡 ���� UI ����
        bool isMelodyMode = levelData.primaryGameMode == GameModeType.Dolphin_MelodyShape;

        bubbleContainer.SetActive(!isMelodyMode);      // �ٸ� �Ҹ� ã�� ���
        directionContainer.SetActive(isMelodyMode);     // ��ε� ��� ���߱� ���

        ResetUI();

        Debug.Log($"���� ���� �ʱ�ȭ: {levelData.primaryGameMode}");
    }

    public void StartQuestion(QuestionData questionData)
    {
        currentQuestionData = questionData;
        isWaitingForAnswer = false;
        isPlayingSequence = true;

        UpdateQuestionUI();

        // ĳ���� ��Ʈ ��� �� ���� ����
        StartCoroutine(PlayCharacterIntroAndStart());
    }

    public void ProcessUserInput(int inputIndex)
    {
        if (!isWaitingForAnswer || isPlayingSequence)
            return;

        isWaitingForAnswer = false;
        bool isCorrect = (inputIndex == currentQuestionData.correctAnswerIndex);

        // ��� �ǵ�� ����
        ShowAnswerFeedback(inputIndex, isCorrect);

        // ���� ��� �� ������Ʈ
        if (isCorrect)
        {
            gameController.AddScore(10);
            PlayCorrectEffect();
        }
        else
        {
            PlayWrongEffect();
        }

        // ��� �� ���� ������ ����
        StartCoroutine(WaitAndProceedToNext(isCorrect));
    }

    public void EndQuestion(bool isCorrect)
    {
        // ���� ���� �� ����
        ResetButtonColors();
        currentQuestionIndex++;
    }

    public void UpdateUI()
    {
        if (questionCountText != null)
        {
            questionCountText.text = $"���� {currentQuestionIndex + 1} / 10";
        }

        if (instructionText != null && currentQuestionData != null)
        {
            instructionText.text = currentQuestionData.questionDescription;
        }
    }

    #endregion

    #region ��ư �̺�Ʈ ó��

    private void OnBubbleButtonClick(int buttonIndex)
    {
        ProcessUserInput(buttonIndex);
    }

    private void OnDirectionButtonClick(int buttonIndex)
    {
        ProcessUserInput(buttonIndex);
    }

    #endregion

    #region ����� �� ������ ó��

    private IEnumerator PlayCharacterIntroAndStart()
    {
        // ������� ����
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(currentLevelData.backgroundImageName))
        {
            AudioManager.Instance.PlayBackgroundMusic($"dolphin_stage_{currentLevelData.stageIndex + 1}");
        }

        // ĳ���� ��Ʈ ���
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterVoice(currentLevelData.characterMentAudio);
        }

        // ���� �ִϸ��̼�
        if (dolphinAnimator != null)
        {
            dolphinAnimator.SetTrigger("Speak");
        }

        yield return new WaitForSeconds(2.0f); // ��Ʈ ��� ���

        // ���� ��忡 ���� ������ ����
        if (currentLevelData.primaryGameMode == GameModeType.Dolphin_DifferentSound)
        {
            yield return StartCoroutine(PlayBubbleSoundSequence());
        }
        else if (currentLevelData.primaryGameMode == GameModeType.Dolphin_MelodyShape)
        {
            yield return StartCoroutine(PlayMelodySequence());
        }

        isPlayingSequence = false;
        isWaitingForAnswer = true;
    }

    private IEnumerator PlayBubbleSoundSequence()
    {
        if (AudioManager.Instance != null && currentQuestionData.audioClipNames != null)
        {
            // AudioManager�� ���� ��� ��� ���
            bool sequenceComplete = false;

            AudioManager.Instance.PlaySequentialAudio(
                currentQuestionData.audioClipNames,
                currentQuestionData.frequencies,
                1.5f,
                (index) => {
                    // �� �Ҹ� ��� �� ��ư ���̶���Ʈ
                    HighlightBubble(index, true);
                    StartCoroutine(DelayedHighlightOff(index, 1.0f));
                },
                () => {
                    sequenceComplete = true;
                }
            );

            // ������ �Ϸ� ���
            yield return new WaitUntil(() => sequenceComplete);
        }
    }

    private IEnumerator DelayedHighlightOff(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        HighlightBubble(index, false);
    }

    private void PlayCorrectEffect()
    {
        if (correctEffect != null)
        {
            correctEffect.Play();
        }

        // ���� ���� ���
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("success_sound");
        }
    }

    private void PlayWrongEffect()
    {
        if (wrongEffect != null)
        {
            wrongEffect.Play();
        }

        // ���� ���� ���
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("wrong_sound");
        }
    }

    private IEnumerator PlayMelodySequence()
    {
        // ��ε� �������� ���
        instructionText.text = "��ε� ��� ������ �����ϼ���";

        if (AudioManager.Instance != null && currentQuestionData.audioClipNames.Length > 0)
        {
            // ���� ��ε� ���
            AudioManager.Instance.PlayGameAudio(
                currentQuestionData.audioClipNames[0],
                currentQuestionData.frequencies[0]
            );
        }

        yield return new WaitForSeconds(3.0f); // ��ε� ��� �ð�

        instructionText.text = "?? ����ΰ���? ?? �ϰ��ΰ���?";
    }

    #endregion

    #region UI ȿ�� �� �ǵ��

    private void HighlightBubble(int index, bool highlight)
    {
        if (index >= 0 && index < bubbleHighlights.Length && bubbleHighlights[index] != null)
        {
            bubbleHighlights[index].color = highlight ? highlightBubbleColor : normalBubbleColor;
        }
    }

    private void ShowAnswerFeedback(int selectedIndex, bool isCorrect)
    {
        Color feedbackColor = isCorrect ? correctBubbleColor : wrongBubbleColor;

        if (currentLevelData.primaryGameMode == GameModeType.Dolphin_DifferentSound)
        {
            // ����� ��� �ǵ��
            if (selectedIndex >= 0 && selectedIndex < bubbleHighlights.Length)
            {
                bubbleHighlights[selectedIndex].color = feedbackColor;
            }
        }
        else
        {
            // ��ε� ��� �ǵ��
            if (selectedIndex >= 0 && selectedIndex < directionButtons.Length)
            {
                ColorBlock colors = directionButtons[selectedIndex].colors;
                colors.normalColor = feedbackColor;
                directionButtons[selectedIndex].colors = colors;
            }
        }

        // �ؽ�Ʈ �ǵ��
        instructionText.text = isCorrect ? "�����Դϴ�! ??" : "�ٽ� �����غ����� ??";

        // ���� ���� �ִϸ��̼�
        if (dolphinAnimator != null)
        {
            dolphinAnimator.SetTrigger(isCorrect ? "Happy" : "Sad");
        }
    }

    private void ResetUI()
    {
        ResetButtonColors();

        if (instructionText != null)
        {
            instructionText.text = "���� ģ���� �Բ� �Ҹ��� �����غ�����!";
        }
    }

    private void ResetButtonColors()
    {
        // ����� ��ư ���� ����
        for (int i = 0; i < bubbleHighlights.Length; i++)
        {
            if (bubbleHighlights[i] != null)
            {
                bubbleHighlights[i].color = normalBubbleColor;
            }
        }

        // ���� ��ư ���� ����
        for (int i = 0; i < directionButtons.Length; i++)
        {
            if (directionButtons[i] != null)
            {
                ColorBlock colors = directionButtons[i].colors;
                colors.normalColor = Color.white;
                directionButtons[i].colors = colors;
            }
        }
    }

    private void UpdateQuestionUI()
    {
        UpdateUI();
        ResetButtonColors();

        if (currentQuestionData != null)
        {
            if (instructionText != null)
            {
                instructionText.text = "�Ҹ��� ������...";
            }
        }
    }

    private IEnumerator WaitAndProceedToNext(bool wasCorrect)
    {
        yield return new WaitForSeconds(2.0f); // �ǵ�� ǥ�� �ð�

        EndQuestion(wasCorrect);

        // ���� ������ ����
        if (currentQuestionIndex < 9) // 10���� �� ���� ���� ������ ������
        {
            // GameModeController���� ���� ���� �ε�
            yield return new WaitForSeconds(1.0f);
            gameController.LoadNextQuestion();
        }
        else
        {
            // �������� �Ϸ�
            gameController.CompleteStage();
        }
    }

    #endregion
}
