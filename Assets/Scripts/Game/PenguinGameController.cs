using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 펭귄 게임 모드 컨트롤러 - 박자에 맞춰 점프 & 리듬 따라치기
/// </summary>
public class PenguinGameController : MonoBehaviour, IGameMode
{
    [Header("UI 참조")]
    [SerializeField] private Button jumpButton;                    // 점프 버튼
    [SerializeField] private Button rhythmButton;                  // 리듬 버튼
    [SerializeField] private Text instructionText;                 // 지시사항
    [SerializeField] private Text bpmText;                         // BPM 표시
    [SerializeField] private Text scoreText;                       // 점수 표시
    [SerializeField] private Slider timingAccuracySlider;          // 타이밍 정확도 바
    [SerializeField] private GameObject jumpModeContainer;          // 점프 모드 UI
    [SerializeField] private GameObject rhythmModeContainer;        // 리듬 모드 UI

    [Header("시각적 효과")]
    [SerializeField] private Animator penguinAnimator;             // 펭귄 애니메이터
    [SerializeField] private ParticleSystem jumpEffect;           // 점프 효과
    [SerializeField] private Image beatIndicator;                  // 박자 표시기
    [SerializeField] private RectTransform rhythmVisualizer;       // 리듬 시각화

    [Header("타이밍 설정")]
    [SerializeField] private float perfectTiming = 0.1f;           // 완벽한 타이밍 범위
    [SerializeField] private float goodTiming = 0.2f;              // 좋은 타이밍 범위
    [SerializeField] private float acceptableTiming = 0.3f;        // 허용 가능한 타이밍 범위

    private GameModeController gameController;
    private LevelData currentLevelData;
    private QuestionData currentQuestionData;
    private bool isGameActive = false;
    private bool isWaitingForInput = false;

    // 박자/리듬 관련 변수
    private float currentBPM = 60f;
    private float beatInterval;
    private float nextBeatTime;
    private int currentBeatIndex = 0;
    private int totalBeats = 0;
    private List<float> expectedBeatTimes;
    private List<bool> userInputs;
    private int currentScore = 0;

    private void Awake()
    {
        gameController = GetComponent<GameModeController>();
        SetupButtons();
        expectedBeatTimes = new List<float>();
        userInputs = new List<bool>();
    }

    private void SetupButtons()
    {
        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(OnJumpButtonClick);
        }

        if (rhythmButton != null)
        {
            rhythmButton.onClick.AddListener(OnRhythmButtonClick);
        }
    }

    #region IGameMode 구현

    public void InitializeGameMode(LevelData levelData)
    {
        currentLevelData = levelData;

        // 게임 모드에 따른 UI 설정
        bool isJumpMode = levelData.primaryGameMode == GameModeType.Penguin_RhythmJump;

        jumpModeContainer.SetActive(isJumpMode);
        rhythmModeContainer.SetActive(!isJumpMode);

        ResetGameState();
        UpdateUI();

        Debug.Log($"펭귄 게임 초기화: {levelData.primaryGameMode}");
    }

    public void StartQuestion(QuestionData questionData)
    {
        currentQuestionData = questionData;
        currentBPM = questionData.bpm > 0 ? questionData.bpm : 60f;
        beatInterval = 60f / currentBPM;

        PrepareQuestion();
        StartCoroutine(PlayCharacterIntroAndStart());
    }

    public void ProcessUserInput(int inputIndex)
    {
        if (!isWaitingForInput)
            return;

        float inputTime = Time.time;
        float accuracy = CalculateTimingAccuracy(inputTime);

        ProcessTimingInput(accuracy);

        // 펭귄 점프 애니메이션
        if (penguinAnimator != null)
        {
            penguinAnimator.SetTrigger("Jump");
        }

        PlayJumpEffect();
    }

    public void EndQuestion(bool isCorrect)
    {
        isGameActive = false;
        isWaitingForInput = false;

        // 최종 점수 계산
        int finalScore = CalculateFinalScore();
        gameController.AddScore(finalScore);
    }

    public void UpdateUI()
    {
        if (bpmText != null)
        {
            bpmText.text = $"BPM: {currentBPM:F0}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"점수: {currentScore}";
        }

        if (instructionText != null && currentQuestionData != null)
        {
            if (currentLevelData.primaryGameMode == GameModeType.Penguin_RhythmJump)
            {
                instructionText.text = "박자에 맞춰 점프 버튼을 누르세요!";
            }
            else
            {
                instructionText.text = "펭귄의 리듬을 듣고 따라해보세요!";
            }
        }
    }

    #endregion

    #region 버튼 이벤트 처리

    private void OnJumpButtonClick()
    {
        ProcessUserInput(0);
    }

    private void OnRhythmButtonClick()
    {
        ProcessUserInput(0);
    }

    #endregion

    #region 게임 로직

    private void PrepareQuestion()
    {
        expectedBeatTimes.Clear();
        userInputs.Clear();
        currentScore = 0;
        currentBeatIndex = 0;

        if (currentQuestionData.rhythmTiming != null && currentQuestionData.rhythmTiming.Length > 0)
        {
            // 리듬 패턴이 정의된 경우
            totalBeats = currentQuestionData.rhythmTiming.Length;

            for (int i = 0; i < totalBeats; i++)
            {
                float beatTime = Time.time + 3.0f + (currentQuestionData.rhythmTiming[i] * beatInterval);
                expectedBeatTimes.Add(beatTime);
                userInputs.Add(false);
            }
        }
        else
        {
            // 기본 정박 패턴
            totalBeats = 4; // 기본 4박

            for (int i = 0; i < totalBeats; i++)
            {
                float beatTime = Time.time + 3.0f + (i * beatInterval);
                expectedBeatTimes.Add(beatTime);
                userInputs.Add(false);
            }
        }
    }

    private IEnumerator PlayCharacterIntroAndStart()
    {
        // 펭귄 인사 멘트
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterVoice(currentLevelData.characterMentAudio);
        }

        if (penguinAnimator != null)
        {
            penguinAnimator.SetTrigger("Speak");
        }

        yield return new WaitForSeconds(2.0f);

        if (currentLevelData.primaryGameMode == GameModeType.Penguin_RhythmJump)
        {
            yield return StartCoroutine(StartJumpMode());
        }
        else
        {
            yield return StartCoroutine(StartRhythmMode());
        }
    }

    private IEnumerator StartJumpMode()
    {
        instructionText.text = "박자가 시작됩니다. 준비하세요!";
        yield return new WaitForSeconds(1.0f);

        isGameActive = true;
        isWaitingForInput = true;

        // 메트로놈 시작
        StartCoroutine(PlayMetronome());

        // 박자 시각화 시작
        StartCoroutine(VisualizeBeat());
    }

    private IEnumerator StartRhythmMode()
    {
        instructionText.text = "펭귄의 리듬을 듣고 기억하세요!";

        // 먼저 펭귄이 리듬을 보여줌
        yield return StartCoroutine(DemonstrateRhythm());

        yield return new WaitForSeconds(1.0f);

        instructionText.text = "이제 따라해보세요!";

        isGameActive = true;
        isWaitingForInput = true;

        // 사용자 입력 대기
        StartCoroutine(WaitForRhythmInput());
    }

    private IEnumerator PlayMetronome()
    {
        while (isGameActive && currentBeatIndex < totalBeats)
        {
            yield return new WaitUntil(() => Time.time >= expectedBeatTimes[currentBeatIndex]);

            // 메트로놈 소리 재생
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameAudio("metronome_tick", 440f);
            }

            currentBeatIndex++;
        }

        // 게임 종료
        yield return new WaitForSeconds(1.0f);
        EndQuestion(true);
    }

    private IEnumerator DemonstrateRhythm()
    {
        for (int i = 0; i < expectedBeatTimes.Count; i++)
        {
            // 펭귄 리듬 액션
            if (penguinAnimator != null)
            {
                penguinAnimator.SetTrigger("Rhythm");
            }

            // 리듬 소리 재생
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameAudio("rhythm_sound", 660f);
            }

            // 시각적 효과
            StartCoroutine(RhythmVisualEffect());

            yield return new WaitForSeconds(beatInterval);
        }
    }

    private IEnumerator WaitForRhythmInput()
    {
        float startTime = Time.time;
        float timeoutDuration = totalBeats * beatInterval + 2.0f;

        while (isGameActive && (Time.time - startTime) < timeoutDuration)
        {
            yield return null;
        }

        EndQuestion(true);
    }

    private IEnumerator VisualizeBeat()
    {
        while (isGameActive)
        {
            // 박자 표시기 애니메이션
            if (beatIndicator != null)
            {
                Color originalColor = beatIndicator.color;
                beatIndicator.color = Color.yellow;

                yield return new WaitForSeconds(0.1f);

                beatIndicator.color = originalColor;
            }

            yield return new WaitForSeconds(beatInterval - 0.1f);
        }
    }

    private IEnumerator RhythmVisualEffect()
    {
        if (rhythmVisualizer != null)
        {
            Vector3 originalScale = rhythmVisualizer.localScale;
            rhythmVisualizer.localScale = originalScale * 1.5f;

            yield return new WaitForSeconds(0.2f);

            rhythmVisualizer.localScale = originalScale;
        }
    }

    #endregion

    #region 타이밍 및 점수 계산

    private float CalculateTimingAccuracy(float inputTime)
    {
        float closestBeatTime = float.MaxValue;
        float minDifference = float.MaxValue;

        foreach (float beatTime in expectedBeatTimes)
        {
            float difference = Mathf.Abs(inputTime - beatTime);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestBeatTime = beatTime;
            }
        }

        return minDifference;
    }

    private void ProcessTimingInput(float accuracy)
    {
        int points = 0;
        string feedback = "";

        if (accuracy <= perfectTiming)
        {
            points = 100;
            feedback = "완벽!";
        }
        else if (accuracy <= goodTiming)
        {
            points = 80;
            feedback = "좋아요!";
        }
        else if (accuracy <= acceptableTiming)
        {
            points = 60;
            feedback = "괜찮아요!";
        }
        else
        {
            points = 20;
            feedback = "조금 더 정확하게!";
        }

        currentScore += points;

        // 타이밍 정확도 슬라이더 업데이트
        if (timingAccuracySlider != null)
        {
            float accuracy01 = Mathf.Clamp01(1.0f - (accuracy / acceptableTiming));
            timingAccuracySlider.value = accuracy01;
        }

        // 피드백 표시
        StartCoroutine(ShowTimingFeedback(feedback));

        UpdateUI();
    }

    private IEnumerator ShowTimingFeedback(string feedback)
    {
        string originalText = instructionText.text;
        instructionText.text = feedback;

        yield return new WaitForSeconds(0.5f);

        instructionText.text = originalText;
    }

    private int CalculateFinalScore()
    {
        // 전체 정확도에 따른 최종 점수 계산
        return Mathf.RoundToInt(currentScore / (float)totalBeats);
    }

    private void PlayJumpEffect()
    {
        if (jumpEffect != null)
        {
            jumpEffect.Play();
        }
    }

    private void ResetGameState()
    {
        isGameActive = false;
        isWaitingForInput = false;
        currentScore = 0;
        currentBeatIndex = 0;
        expectedBeatTimes.Clear();
        userInputs.Clear();
    }

    #endregion

    private void Update()
    {
        // 실시간 박자 체크 (디버그용)
        if (isGameActive && Input.GetKeyDown(KeyCode.Space))
        {
            ProcessUserInput(0);
        }
    }
}
