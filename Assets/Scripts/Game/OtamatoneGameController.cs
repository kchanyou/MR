using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 오타마톤 게임 모드 컨트롤러 - 다른 악기 찾기 & 악기 맞추기
/// </summary>
public class OtamatoneGameController : MonoBehaviour, IGameMode
{
    [Header("UI 참조")]
    [SerializeField] private Button[] instrumentButtons = new Button[10]; // 최대 10개 악기 버튼
    [SerializeField] private Image[] instrumentIcons = new Image[10];     // 악기 아이콘들
    [SerializeField] private Text[] instrumentLabels = new Text[10];      // 악기 이름 텍스트
    [SerializeField] private Text instructionText;                        // 지시사항
    [SerializeField] private Text questionCountText;                      // 문제 번호
    [SerializeField] private Button playAgainButton;                      // 다시 듣기 버튼

    [Header("시각적 효과")]
    [SerializeField] private Animator otamatoneAnimator;                  // 오타마톤 애니메이터
    [SerializeField] private ParticleSystem correctEffect;               // 정답 효과
    [SerializeField] private ParticleSystem wrongEffect;                 // 오답 효과
    [SerializeField] private Image soundWaveVisualizer;                  // 음파 시각화

    [Header("색상 설정")]
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color highlightButtonColor = Color.yellow;
    [SerializeField] private Color correctButtonColor = Color.green;
    [SerializeField] private Color wrongButtonColor = Color.red;
    [SerializeField] private Color playingButtonColor = Color.cyan;

    [Header("악기 아이콘 스프라이트")]
    [SerializeField] private Sprite[] instrumentSprites; // 악기별 스프라이트들

    private GameModeController gameController;
    private LevelData currentLevelData;
    private QuestionData currentQuestionData;
    private bool isWaitingForAnswer = false;
    private bool isPlayingSequence = false;
    private int currentQuestionIndex = 0;
    private int activeButtonCount = 3; // 기본 3개 버튼

    // 악기 데이터
    private Dictionary<string, string> instrumentNames;
    private Dictionary<string, Sprite> instrumentSpriteDict;

    private void Awake()
    {
        gameController = GetComponent<GameModeController>();
        InitializeInstrumentData();
        SetupButtons();
    }

    private void InitializeInstrumentData()
    {
        instrumentNames = new Dictionary<string, string>
        {
            {"piano", "피아노"}, {"guitar", "기타"}, {"violin", "바이올린"}, {"flute", "플루트"},
            {"trumpet", "트럼펫"}, {"drums", "드럼"}, {"cello", "첼로"}, {"saxophone", "색소폰"},
            {"harp", "하프"}, {"clarinet", "클라리넷"}, {"trombone", "트롬본"}, {"oboe", "오보에"},
            {"bassoon", "바순"}, {"horn", "호른"}, {"tuba", "튜바"}, {"timpani", "팀파니"},
            {"gayageum", "가야금"}, {"daegeum", "대금"}, {"haegeum", "해금"}, {"janggu", "장구"},
            {"ajaeng", "아쟁"}, {"geomungo", "거문고"}, {"sogeum", "소금"}, {"danso", "단소"},
            {"piccolo", "피콜로"}, {"bass_clarinet", "베이스클라리넷"}, {"english_horn", "잉글리시호른"}
        };

        instrumentSpriteDict = new Dictionary<string, Sprite>();
        InitializeSpriteDictionary();
    }

    private void InitializeSpriteDictionary()
    {
        // Inspector에서 설정된 스프라이트 배열을 딕셔너리로 변환
        if (instrumentSprites != null && instrumentSprites.Length > 0)
        {
            string[] keys = {
                "piano", "guitar", "violin", "flute", "trumpet", "drums", "cello", "saxophone",
                "harp", "clarinet", "trombone", "oboe", "bassoon", "horn", "tuba", "timpani",
                "gayageum", "daegeum", "haegeum", "janggu", "ajaeng", "geomungo", "sogeum", "danso"
            };

            for (int i = 0; i < keys.Length && i < instrumentSprites.Length; i++)
            {
                if (instrumentSprites[i] != null)
                {
                    instrumentSpriteDict[keys[i]] = instrumentSprites[i];
                }
            }
        }
    }

    private void SetupButtons()
    {
        for (int i = 0; i < instrumentButtons.Length; i++)
        {
            int buttonIndex = i; // 클로저 문제 해결
            if (instrumentButtons[i] != null)
            {
                instrumentButtons[i].onClick.AddListener(() => OnInstrumentButtonClick(buttonIndex));
            }
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClick);
        }
    }

    #region IGameMode 구현

    public void InitializeGameMode(LevelData levelData)
    {
        currentLevelData = levelData;
        currentQuestionIndex = 0;

        // 게임 모드에 따른 버튼 개수 설정
        activeButtonCount = currentLevelData.primaryGameMode == GameModeType.Otamatone_DifferentInstrument ? 3 :
                          (levelData.stageIndex < 2 ? 4 : (levelData.stageIndex < 4 ? 6 : (levelData.stageIndex < 6 ? 8 : 10)));

        SetupUIForCurrentMode();
        InitializeGameUI();
        UpdateUI();

        Debug.Log($"오타마톤 게임 초기화: {levelData.primaryGameMode}");
    }

    public void StartQuestion(QuestionData questionData)
    {
        currentQuestionData = questionData;
        isWaitingForAnswer = false;
        isPlayingSequence = true;

        UpdateQuestionUI();
        PrepareInstrumentButtons();
        UpdateGameplayUI();

        StartCoroutine(PlayCharacterIntroAndStart());
    }

    public void ProcessUserInput(int inputIndex)
    {
        if (!isWaitingForAnswer || isPlayingSequence || inputIndex >= activeButtonCount)
            return;

        isWaitingForAnswer = false;
        bool isCorrect = (inputIndex == currentQuestionData.correctAnswerIndex);

        // 즉시 피드백 제공
        ShowAnswerFeedback(inputIndex, isCorrect);

        // 점수 업데이트
        if (isCorrect)
        {
            gameController.AddScore(10);
            PlayCorrectEffect();
        }
        else
        {
            PlayWrongEffect();
        }

        StartCoroutine(WaitAndProceedToNext(isCorrect));
    }

    public void EndQuestion(bool isCorrect)
    {
        ResetButtonColors();
        currentQuestionIndex++;
    }

    public void UpdateUI()
    {
        UpdateProgressUI();
        UpdateInstructionText();
    }

    #endregion

    #region 버튼 이벤트 처리

    private void OnInstrumentButtonClick(int buttonIndex)
    {
        ProcessUserInput(buttonIndex);
    }

    private void OnPlayAgainClick()
    {
        if (!isPlayingSequence && currentQuestionData != null)
        {
            StartCoroutine(ReplayCurrentQuestion());
        }
    }

    #endregion

    #region UI 업데이트 및 관리

    /// <summary>
    /// 문제 시작 시 UI를 업데이트합니다.
    /// </summary>
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

            if (questionCountText != null)
            {
                questionCountText.text = $"문제 {currentQuestionIndex + 1} / 10";
            }
        }
    }

    /// <summary>
    /// 게임 시작 시 전체 UI를 초기화합니다.
    /// </summary>
    private void InitializeGameUI()
    {
        ResetButtonColors();

        if (instructionText != null)
        {
            instructionText.text = "오타마톤과 함께 악기 소리를 탐험해보세요!";
        }

        if (questionCountText != null)
        {
            questionCountText.text = "준비 중...";
        }

        if (playAgainButton != null)
        {
            playAgainButton.interactable = false;
        }
    }

    /// <summary>
    /// 문제 진행 중 UI 상태를 업데이트합니다.
    /// </summary>
    private void UpdateGameplayUI()
    {
        if (playAgainButton != null && currentLevelData.primaryGameMode == GameModeType.Otamatone_InstrumentMatch)
        {
            playAgainButton.interactable = !isPlayingSequence;
        }

        SetButtonsInteractable(isWaitingForAnswer && !isPlayingSequence);
    }

    /// <summary>
    /// 모든 악기 버튼의 상호작용 가능 여부를 설정합니다.
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < activeButtonCount && i < instrumentButtons.Length; i++)
        {
            if (instrumentButtons[i] != null)
            {
                instrumentButtons[i].interactable = interactable;
            }
        }
    }

    /// <summary>
    /// 현재 게임 모드에 맞게 지시 텍스트를 업데이트합니다.
    /// </summary>
    private void UpdateInstructionText()
    {
        if (instructionText != null && currentQuestionData != null)
        {
            if (currentLevelData.primaryGameMode == GameModeType.Otamatone_DifferentInstrument)
            {
                instructionText.text = "다른 악기는 어느 것인가요?";
            }
            else
            {
                instructionText.text = "어떤 악기의 소리인가요?";
            }
        }
    }

    /// <summary>
    /// 진행률 표시 UI를 업데이트합니다.
    /// </summary>
    private void UpdateProgressUI()
    {
        if (questionCountText != null)
        {
            float progress = (float)(currentQuestionIndex + 1) / 10.0f;
            questionCountText.text = $"문제 {currentQuestionIndex + 1} / 10 ({progress:P0})";
        }
    }

    #endregion

    #region UI 설정 및 관리

    private void SetupUIForCurrentMode()
    {
        // 필요한 버튼만 활성화
        for (int i = 0; i < instrumentButtons.Length; i++)
        {
            if (instrumentButtons[i] != null)
            {
                instrumentButtons[i].gameObject.SetActive(i < activeButtonCount);
            }
        }

        // 다시 듣기 버튼은 악기 맞추기 모드에서만 활성화
        if (playAgainButton != null)
        {
            playAgainButton.gameObject.SetActive(currentLevelData.primaryGameMode == GameModeType.Otamatone_InstrumentMatch);
        }
    }

    private void PrepareInstrumentButtons()
    {
        if (currentLevelData.primaryGameMode == GameModeType.Otamatone_DifferentInstrument)
        {
            SetupDifferentInstrumentMode();
        }
        else
        {
            SetupInstrumentMatchMode();
        }
    }

    private void SetupDifferentInstrumentMode()
    {
        for (int i = 0; i < activeButtonCount && i < currentQuestionData.audioClipNames.Length; i++)
        {
            string instrumentKey = ExtractInstrumentFromClipName(currentQuestionData.audioClipNames[i]);
            SetupInstrumentButton(i, instrumentKey);
        }
    }

    private void SetupInstrumentMatchMode()
    {
        string[] stageInstruments = GetInstrumentChoicesForStage();

        for (int i = 0; i < activeButtonCount && i < stageInstruments.Length; i++)
        {
            SetupInstrumentButton(i, stageInstruments[i]);
        }
    }

    private void SetupInstrumentButton(int buttonIndex, string instrumentKey)
    {
        if (buttonIndex >= 0 && buttonIndex < instrumentButtons.Length && instrumentButtons[buttonIndex] != null)
        {
            // 악기 이름 설정
            if (buttonIndex < instrumentLabels.Length && instrumentLabels[buttonIndex] != null)
            {
                string displayName = instrumentNames.ContainsKey(instrumentKey) ? instrumentNames[instrumentKey] : instrumentKey;
                instrumentLabels[buttonIndex].text = displayName;
            }

            // 악기 아이콘 설정
            if (buttonIndex < instrumentIcons.Length && instrumentIcons[buttonIndex] != null && instrumentSpriteDict.ContainsKey(instrumentKey))
            {
                instrumentIcons[buttonIndex].sprite = instrumentSpriteDict[instrumentKey];
            }

            SetButtonColor(buttonIndex, normalButtonColor);
        }
    }

    private string[] GetInstrumentChoicesForStage()
    {
        switch (currentLevelData.stageIndex % 4)
        {
            case 0: return new string[] { "piano", "guitar", "violin", "flute" };
            case 1: return new string[] { "saxophone", "cello", "horn", "harp", "timpani", "drums" };
            case 2: return new string[] { "gayageum", "daegeum", "haegeum", "janggu", "ajaeng", "piano", "violin", "flute" };
            case 3: return new string[] { "piccolo", "bass_clarinet", "english_horn", "geomungo", "violin", "cello", "piano", "flute", "oboe", "harp" };
            default: return new string[] { "piano", "guitar", "violin", "flute" };
        }
    }

    private string ExtractInstrumentFromClipName(string clipName)
    {
        int underscoreIndex = clipName.IndexOf('_');
        return underscoreIndex > 0 ? clipName.Substring(0, underscoreIndex) : clipName;
    }

    #endregion

    #region 오디오 및 시퀀스 처리

    private IEnumerator PlayCharacterIntroAndStart()
    {
        // 오타마톤 멘트 재생
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCharacterVoice(currentLevelData.characterMentAudio);
        }

        if (otamatoneAnimator != null)
        {
            otamatoneAnimator.SetTrigger("Speak");
        }

        yield return new WaitForSeconds(2.0f);

        if (currentLevelData.primaryGameMode == GameModeType.Otamatone_DifferentInstrument)
        {
            yield return StartCoroutine(PlayDifferentInstrumentSequence());
        }
        else
        {
            yield return StartCoroutine(PlayInstrumentMatchSequence());
        }

        isPlayingSequence = false;
        isWaitingForAnswer = true;

        UpdateGameplayUI();
    }

    private IEnumerator PlayDifferentInstrumentSequence()
    {
        instructionText.text = "세 악기의 소리를 들어보세요";

        // 3개 버튼을 순차적으로 하이라이트하며 소리 재생
        for (int i = 0; i < activeButtonCount && i < currentQuestionData.audioClipNames.Length; i++)
        {
            SetButtonColor(i, playingButtonColor);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameAudio(
                    currentQuestionData.audioClipNames[i],
                    currentQuestionData.frequencies[i]
                );
            }

            StartCoroutine(SoundWaveEffect());

            yield return new WaitForSeconds(1.8f);

            SetButtonColor(i, normalButtonColor);
            yield return new WaitForSeconds(0.5f);
        }

        instructionText.text = "다른 악기는 어느 것인가요?";
    }

    private IEnumerator PlayInstrumentMatchSequence()
    {
        instructionText.text = "악기 소리를 들어보세요";

        if (AudioManager.Instance != null && currentQuestionData.audioClipNames.Length > 0)
        {
            AudioManager.Instance.PlayGameAudio(
                currentQuestionData.audioClipNames[0],
                currentQuestionData.frequencies[0]
            );

            StartCoroutine(SoundWaveEffect());
        }

        yield return new WaitForSeconds(2.5f);

        instructionText.text = "어떤 악기의 소리인가요?";
    }

    private IEnumerator ReplayCurrentQuestion()
    {
        instructionText.text = "다시 들어보세요";

        if (currentLevelData.primaryGameMode == GameModeType.Otamatone_InstrumentMatch)
        {
            if (AudioManager.Instance != null && currentQuestionData.audioClipNames.Length > 0)
            {
                AudioManager.Instance.PlayGameAudio(
                    currentQuestionData.audioClipNames[0],
                    currentQuestionData.frequencies[0]
                );

                StartCoroutine(SoundWaveEffect());
            }
        }

        yield return new WaitForSeconds(2.0f);

        UpdateInstructionText();
    }

    private IEnumerator SoundWaveEffect()
    {
        if (soundWaveVisualizer != null)
        {
            Color originalColor = soundWaveVisualizer.color;

            for (int i = 0; i < 10; i++)
            {
                float alpha = Mathf.Sin(i * 0.5f) * 0.5f + 0.5f;
                soundWaveVisualizer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return new WaitForSeconds(0.1f);
            }

            soundWaveVisualizer.color = originalColor;
        }
    }

    #endregion

    #region 시각적 효과 및 피드백

    private void ShowAnswerFeedback(int selectedIndex, bool isCorrect)
    {
        Color feedbackColor = isCorrect ? correctButtonColor : wrongButtonColor;
        SetButtonColor(selectedIndex, feedbackColor);

        if (!isCorrect && currentQuestionData.correctAnswerIndex < activeButtonCount)
        {
            SetButtonColor(currentQuestionData.correctAnswerIndex, correctButtonColor);
        }

        instructionText.text = isCorrect ? "정답입니다!" : "아쉬워요! 다시 도전해보세요";

        if (otamatoneAnimator != null)
        {
            otamatoneAnimator.SetTrigger(isCorrect ? "Happy" : "Sad");
        }
    }

    private void SetButtonColor(int buttonIndex, Color color)
    {
        if (buttonIndex >= 0 && buttonIndex < instrumentButtons.Length && instrumentButtons[buttonIndex] != null)
        {
            ColorBlock colors = instrumentButtons[buttonIndex].colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.selectedColor = color;
            instrumentButtons[buttonIndex].colors = colors;
        }
    }

    private void ResetButtonColors()
    {
        for (int i = 0; i < activeButtonCount; i++)
        {
            SetButtonColor(i, normalButtonColor);
        }
    }

    private void PlayCorrectEffect()
    {
        if (correctEffect != null)
        {
            correctEffect.Play();
        }
    }

    private void PlayWrongEffect()
    {
        if (wrongEffect != null)
        {
            wrongEffect.Play();
        }
    }

    private IEnumerator WaitAndProceedToNext(bool wasCorrect)
    {
        yield return new WaitForSeconds(2.5f);

        EndQuestion(wasCorrect);

        if (currentQuestionIndex < 9)
        {
            yield return new WaitForSeconds(1.0f);
            gameController.LoadNextQuestion();
        }
        else
        {
            gameController.CompleteStage();
        }
    }

    #endregion
}
