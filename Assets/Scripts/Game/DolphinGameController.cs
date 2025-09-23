using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 돌고래 게임 모드 컨트롤러 - 방울 속 다른 소리 찾기 & 멜로디 모양 맞추기
/// </summary>
public class DolphinGameController : MonoBehaviour, IGameMode
{
    [Header("UI 참조")]
    [SerializeField] private Button[] bubbleButtons = new Button[3];        // 물방울 버튼들
    [SerializeField] private Button[] directionButtons = new Button[2];     // 방향 선택 버튼들 (상승/하강)
    [SerializeField] private Image[] bubbleHighlights = new Image[3];       // 버튼 하이라이트
    [SerializeField] private Text instructionText;                          // 지시사항 텍스트
    [SerializeField] private Text questionCountText;                        // 문제 번호 표시
    [SerializeField] private GameObject bubbleContainer;                    // 물방울 컨테이너
    [SerializeField] private GameObject directionContainer;                 // 방향 버튼 컨테이너

    [Header("시각적 효과")]
    [SerializeField] private ParticleSystem correctEffect;                  // 정답 효과
    [SerializeField] private ParticleSystem wrongEffect;                    // 오답 효과
    [SerializeField] private Animator dolphinAnimator;                      // 돌고래 애니메이터

    [Header("색상 설정")]
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
        // 물방울 버튼 설정
        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            int buttonIndex = i; // 클로저 문제 해결
            if (bubbleButtons[i] != null)
            {
                bubbleButtons[i].onClick.AddListener(() => OnBubbleButtonClick(buttonIndex));
            }
        }

        // 방향 버튼 설정
        for (int i = 0; i < directionButtons.Length; i++)
        {
            int buttonIndex = i;
            if (directionButtons[i] != null)
            {
                directionButtons[i].onClick.AddListener(() => OnDirectionButtonClick(buttonIndex));
            }
        }
    }

    #region IGameMode 구현

    public void InitializeGameMode(LevelData levelData)
    {
        currentLevelData = levelData;
        currentQuestionIndex = 0;

        // 게임 모드에 따른 UI 설정
        bool isMelodyMode = levelData.primaryGameMode == GameModeType.Dolphin_MelodyShape;

        bubbleContainer.SetActive(!isMelodyMode);      // 다른 소리 찾기 모드
        directionContainer.SetActive(isMelodyMode);     // 멜로디 모양 맞추기 모드

        ResetUI();

        Debug.Log($"돌고래 게임 초기화: {levelData.primaryGameMode}");
    }

    public void StartQuestion(QuestionData questionData)
    {
        currentQuestionData = questionData;
        isWaitingForAnswer = false;
        isPlayingSequence = true;

        UpdateQuestionUI();

        // 캐릭터 멘트 재생 후 문제 시작
        StartCoroutine(PlayCharacterIntroAndStart());
    }

    public void ProcessUserInput(int inputIndex)
    {
        if (!isWaitingForAnswer || isPlayingSequence)
            return;

        isWaitingForAnswer = false;
        bool isCorrect = (inputIndex == currentQuestionData.correctAnswerIndex);

        // 즉시 피드백 제공
        ShowAnswerFeedback(inputIndex, isCorrect);

        // 점수 계산 및 업데이트
        if (isCorrect)
        {
            gameController.AddScore(10);
            PlayCorrectEffect();
        }
        else
        {
            PlayWrongEffect();
        }

        // 잠시 후 다음 문제로 진행
        StartCoroutine(WaitAndProceedToNext(isCorrect));
    }

    public void EndQuestion(bool isCorrect)
    {
        // 문제 종료 후 정리
        ResetButtonColors();
        currentQuestionIndex++;
    }

    public void UpdateUI()
    {
        if (questionCountText != null)
        {
            questionCountText.text = $"문제 {currentQuestionIndex + 1} / 10";
        }

        if (instructionText != null && currentQuestionData != null)
        {
            instructionText.text = currentQuestionData.questionDescription;
        }
    }

    #endregion

    #region 버튼 이벤트 처리

    private void OnBubbleButtonClick(int buttonIndex)
    {
        ProcessUserInput(buttonIndex);
    }

    private void OnDirectionButtonClick(int buttonIndex)
    {
        ProcessUserInput(buttonIndex);
    }

    #endregion

    #region 오디오 및 시퀀스 처리

    private IEnumerator PlayCharacterIntroAndStart()
    {
        // 배경음악 시작
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(currentLevelData.backgroundImageName))
        {
            AudioManager.Instance.PlayBackgroundMusic($"dolphin_stage_{currentLevelData.stageIndex + 1}");
        }

        // 캐릭터 멘트 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterVoice(currentLevelData.characterMentAudio);
        }

        // 돌고래 애니메이션
        if (dolphinAnimator != null)
        {
            dolphinAnimator.SetTrigger("Speak");
        }

        yield return new WaitForSeconds(2.0f); // 멘트 재생 대기

        // 게임 모드에 따른 시퀀스 시작
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
            // AudioManager의 순차 재생 기능 사용
            bool sequenceComplete = false;

            AudioManager.Instance.PlaySequentialAudio(
                currentQuestionData.audioClipNames,
                currentQuestionData.frequencies,
                1.5f,
                (index) => {
                    // 각 소리 재생 시 버튼 하이라이트
                    HighlightBubble(index, true);
                    StartCoroutine(DelayedHighlightOff(index, 1.0f));
                },
                () => {
                    sequenceComplete = true;
                }
            );

            // 시퀀스 완료 대기
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

        // 성공 사운드 재생
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

        // 실패 사운드 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("wrong_sound");
        }
    }

    private IEnumerator PlayMelodySequence()
    {
        // 멜로디를 연속으로 재생
        instructionText.text = "멜로디를 듣고 방향을 선택하세요";

        if (AudioManager.Instance != null && currentQuestionData.audioClipNames.Length > 0)
        {
            // 연속 멜로디 재생
            AudioManager.Instance.PlayGameAudio(
                currentQuestionData.audioClipNames[0],
                currentQuestionData.frequencies[0]
            );
        }

        yield return new WaitForSeconds(3.0f); // 멜로디 재생 시간

        instructionText.text = "?? 상승인가요? ?? 하강인가요?";
    }

    #endregion

    #region UI 효과 및 피드백

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
            // 물방울 모드 피드백
            if (selectedIndex >= 0 && selectedIndex < bubbleHighlights.Length)
            {
                bubbleHighlights[selectedIndex].color = feedbackColor;
            }
        }
        else
        {
            // 멜로디 모드 피드백
            if (selectedIndex >= 0 && selectedIndex < directionButtons.Length)
            {
                ColorBlock colors = directionButtons[selectedIndex].colors;
                colors.normalColor = feedbackColor;
                directionButtons[selectedIndex].colors = colors;
            }
        }

        // 텍스트 피드백
        instructionText.text = isCorrect ? "정답입니다! ??" : "다시 생각해보세요 ??";

        // 돌고래 반응 애니메이션
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
            instructionText.text = "돌고래 친구와 함께 소리를 구별해보세요!";
        }
    }

    private void ResetButtonColors()
    {
        // 물방울 버튼 색상 리셋
        for (int i = 0; i < bubbleHighlights.Length; i++)
        {
            if (bubbleHighlights[i] != null)
            {
                bubbleHighlights[i].color = normalBubbleColor;
            }
        }

        // 방향 버튼 색상 리셋
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
                instructionText.text = "소리를 들어보세요...";
            }
        }
    }

    private IEnumerator WaitAndProceedToNext(bool wasCorrect)
    {
        yield return new WaitForSeconds(2.0f); // 피드백 표시 시간

        EndQuestion(wasCorrect);

        // 다음 문제로 진행
        if (currentQuestionIndex < 9) // 10문제 중 아직 남은 문제가 있으면
        {
            // GameModeController에서 다음 문제 로드
            yield return new WaitForSeconds(1.0f);
            gameController.LoadNextQuestion();
        }
        else
        {
            // 스테이지 완료
            gameController.CompleteStage();
        }
    }

    #endregion
}
