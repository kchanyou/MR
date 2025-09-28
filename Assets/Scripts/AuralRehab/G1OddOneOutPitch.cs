using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G1: 거품 속 "다른 소리" 찾기 (3지선다)
    /// - 라운드 시작 시 1→2→3번 선택지를 하이라이트하며, 각 선택지에 할당된 소리를 1회씩 자동 재생(프리뷰)
    /// - 다시 듣기 버튼으로 동일한 프리뷰(1→2→3)를 반복 재생 가능
    /// - intervalSemitones: 7(완전5도) / 4(장3도) / 2(장2도) / 1(반음)
    /// - 일시정지/재개(IPausableGame) 지원
    /// </summary>
    public class G1OddOneOutPitch : MonoBehaviour, IPausableGame {
        [Header("UI (Assign in Inspector)")]
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button btn1;
        [SerializeField] Button btn2;
        [SerializeField] Button btn3;
        [SerializeField] Button replayButton;   // "다시 듣기" 버튼

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [Tooltip("C, C#, D, D#, E, F, F#, G, G#, A, A#, B 순서")]
        [SerializeField] AudioClip[] chromaticClips;
        [SerializeField] bool usePitchShiftIfClipsMissing = true;
        [SerializeField] AudioClip baseClipForPitchShift;

        [Header("Timing")]
        [SerializeField, Range(0.1f, 1.5f)] float noteDuration = 0.6f;     // 각 음 길이
        [SerializeField, Range(0.0f, 0.8f)] float gapBetweenNotes = 0.15f; // 음 사이 간격
        [SerializeField, Range(0.0f, 2.0f)] float preTrialDelay = 0.25f;   // 라운드 시작 전 대기
        [SerializeField, Range(0.0f, 0.5f)] float postPlayReadyDelay = 0.05f; // 프리뷰 후 버튼 활성까지 대기
        [SerializeField] bool useUnscaledTime = true;

        [Header("Rules")]
        [SerializeField, Min(1)] int totalTrials = 8;
        [SerializeField] int intervalSemitones = 7;

        [Header("Highlight")]
        [SerializeField] Color highlightColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField, Range(1f, 1.2f)] float highlightScale = 1.06f;
        [SerializeField] bool useScalePulse = true;

        // 내부 상태
        int _trialIndex;
        int _correct;
        float _sumReaction;

        int _answerIndex;                 // 0..2
        int[] _choiceNotes = new int[3];  // 각 버튼(1~3)에 매핑된 반음 인덱스
        bool _waitingForAnswer;
        float _reactionTimer;
        bool _paused;
        bool _isPreviewing;               // 라운드 시작 프리뷰 재생 중
        bool _isReplaying;                // "다시 듣기" 재생 중

        // 하이라이트 복원용
        Graphic[] _btnGraphic = new Graphic[3];
        Color[]   _origColor  = new Color[3];
        Vector3[] _origScale  = new Vector3[3];

        public System.Action<int, bool, float> OnTrialEnd;
        public System.Action<int, int, float> OnGameFinished;

        void Awake() {
            if (btn1) btn1.onClick.AddListener(() => Submit(0));
            if (btn2) btn2.onClick.AddListener(() => Submit(1));
            if (btn3) btn3.onClick.AddListener(() => Submit(2));
            if (replayButton) replayButton.onClick.AddListener(OnReplayRequested);

            CacheButtonVisuals();
            SetAnswerButtons(false);
            SetReplayButton(false);

            if (promptText && string.IsNullOrEmpty(promptText.text))
                promptText.text = "다른 소리를 고르세요";
        }

        void CacheButtonVisuals() {
            Button[] arr = { btn1, btn2, btn3 };
            for (int i = 0; i < 3; i++) {
                _btnGraphic[i] = arr[i] ? arr[i].targetGraphic : null;
                _origColor[i]  = _btnGraphic[i] ? _btnGraphic[i].color : Color.white;
                _origScale[i]  = arr[i] ? arr[i].transform.localScale : Vector3.one;
            }
        }

        public void SetInterval(int semitones) => intervalSemitones = Mathf.Clamp(semitones, 1, 11);
        public void SetUseUnscaledTime(bool on) => useUnscaledTime = on;

        public void StartGame() {
            StopAllCoroutines();
            _trialIndex = 0; _correct = 0; _sumReaction = 0f;
            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop() {
            yield return WaitSmart(preTrialDelay);

            while (_trialIndex < totalTrials) {
                UpdateProgressUI();
                PrepareTrialMapping();

                // 안내
                AuralRehab.Application.ServiceHub.I.Caption.ShowTop("각 선택지의 소리를 듣고, 다른 소리를 고르세요.");

                // 라운드 시작 프리뷰(1→2→3)
                yield return PlayPreviewSequence();

                // 선택 대기 상태 진입
                _waitingForAnswer = true;
                _reactionTimer = 0f;
                SetAnswerButtons(true);
                SetReplayButton(true);

                // 응답 대기(일시정지·재생 중에는 반응 시간 정지)
                while (_waitingForAnswer) {
                    if (!_paused && !_isPreviewing && !_isReplaying) _reactionTimer += Dt();
                    yield return null;
                }

                SetReplayButton(false);
                yield return WaitSmart(preTrialDelay);
            }

            float avg = (_trialIndex > 0) ? _sumReaction / _trialIndex : 0f;
            OnGameFinished?.Invoke(_trialIndex, _correct, avg);
        }

        // 버튼 매핑(3개 버튼에 음을 배정)
        void PrepareTrialMapping() {
            int n = Mathf.Max(12, chromaticClips != null ? chromaticClips.Length : 12);
            int root  = Random.Range(0, n);
            int other = (root + (intervalSemitones % 12)) % n;

            _answerIndex = Random.Range(0, 3); // "다른 소리"가 들어갈 버튼 인덱스
            for (int i = 0; i < 3; i++) _choiceNotes[i] = (i == _answerIndex) ? other : root;
        }

        // 프리뷰: 1→2→3 하이라이트 + 해당 버튼에 배정된 음 재생
        IEnumerator PlayPreviewSequence() {
            _isPreviewing = true;
            SetAnswerButtons(false); // 프리뷰 중 입력 잠금
            SetReplayButton(false);

            for (int i = 0; i < 3; i++) {
                yield return HighlightAndPlay(i);
                if (i < 2) yield return WaitSmart(gapBetweenNotes);
            }

            _isPreviewing = false;
            // 여기서 버튼을 바로 켜지 않음(상위 루프에서 상태 전환)
        }

        // 다시 듣기(프리뷰와 동일 시퀀스, 반응 시간 정지)
        void OnReplayRequested() {
            if (!_waitingForAnswer || _paused || _isPreviewing || _isReplaying) return;
            StartCoroutine(CoReplay());
        }

        IEnumerator CoReplay() {
            _isReplaying = true;
            SetAnswerButtons(false);
            SetReplayButton(false);

            for (int i = 0; i < 3; i++) {
                yield return HighlightAndPlay(i);
                if (i < 2) yield return WaitSmart(gapBetweenNotes);
            }

            SetAnswerButtons(true);
            SetReplayButton(true);
            _isReplaying = false;
        }

        IEnumerator HighlightAndPlay(int idx) {
            // 하이라이트 ON
            SetHighlight(idx, true);

            // 사운드 재생
            PlayNote(_choiceNotes[idx]);
            yield return WaitSmart(noteDuration);

            // 하이라이트 OFF
            SetHighlight(idx, false);
        }

        void SetHighlight(int idx, bool on) {
            // 색상 하이라이트
            if (_btnGraphic[idx]) {
                _btnGraphic[idx].color = on ? highlightColor : _origColor[idx];
            }
            // 스케일 펄스
            Button b = GetButton(idx);
            if (useScalePulse && b) {
                b.transform.localScale = on ? (_origScale[idx] * highlightScale) : _origScale[idx];
            }
        }

        Button GetButton(int idx) => idx switch {
            0 => btn1, 1 => btn2, _ => btn3
        };

        void PlayNote(int index) {
            if (!audioSource) return;
            audioSource.Stop();

            // 클립 직접 매핑 우선
            if (chromaticClips != null && chromaticClips.Length > 0) {
                int i = ((index % chromaticClips.Length) + chromaticClips.Length) % chromaticClips.Length;
                var clip = chromaticClips[i];
                if (clip) {
                    audioSource.pitch = 1f;
                    audioSource.clip = clip;
                    audioSource.Play();
                    return;
                }
            }
            // 폴백: 피치시프트
            if (usePitchShiftIfClipsMissing && baseClipForPitchShift) {
                audioSource.clip = baseClipForPitchShift;
                float semis = index % 12;
                audioSource.pitch = Mathf.Pow(2f, semis / 12f);
                audioSource.Play();
            }
        }

        void Submit(int picked) {
            if (!_waitingForAnswer || _paused || _isPreviewing || _isReplaying) return;

            bool ok = (picked == _answerIndex);
            if (ok) _correct++;
            _sumReaction += Mathf.Max(0f, _reactionTimer);

            OnTrialEnd?.Invoke(_trialIndex + 1, ok, _reactionTimer);
            _trialIndex++;

            SetAnswerButtons(false);
            _waitingForAnswer = false;

            if (promptText) promptText.text = ok ? "정답" : "오답";
        }

        void SetAnswerButtons(bool on) {
            if (btn1) btn1.interactable = on;
            if (btn2) btn2.interactable = on;
            if (btn3) btn3.interactable = on;
        }

        void SetReplayButton(bool on) {
            if (replayButton) replayButton.interactable = on;
        }

        void UpdateProgressUI() {
            if (progressText) progressText.text = $"{Mathf.Min(_trialIndex, totalTrials)}/{totalTrials}";
        }

        // ---- Pause API ----
        public void Pause() {
            _paused = true;
            SetAnswerButtons(false);
            SetReplayButton(false);
            if (audioSource && audioSource.isPlaying) audioSource.Pause();
        }

        public void Resume() {
            _paused = false;
            if (_waitingForAnswer && !_isPreviewing && !_isReplaying) {
                SetAnswerButtons(true);
                SetReplayButton(true);
            }
            if (audioSource) audioSource.UnPause();
        }

        public bool IsPaused => _paused;

        // ---- Timing helpers ----
        float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        IEnumerator WaitSmart(float seconds) {
            float t = 0f;
            seconds = Mathf.Max(0f, seconds);
            while (t < seconds) {
                if (!_paused) t += Dt();
                yield return null;
            }
        }
    }
}