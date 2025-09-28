using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G2: 멜로디 방향 맞히기 (2지선다)
    /// - 라운드 시작 시 멜로디를 자동 재생(프리뷰)
    /// - "다시 듣기"로 프리뷰 전체 반복 가능(반응속도 타이머 정지)
    /// - 일시정지/재개(IPausableGame) 지원
    /// 
    /// 스테이지별 과제 유형(TaskType):
    /// - UpDownSimple      : 상승 vs 하강 (3음, 예: 0, +s, +2s / 0, -s, -2s)
    /// - HillValley        : 상승→하강(산) vs 하강→상승(골짜기) (3음, 예: 0, +s, 0 / 0, -s, 0)
    /// - TripleChange      : 상승→하강→상승 vs 하강→상승→하강 (4음, 예: 0, +s, 0, +s / 0, -s, 0, -s)
    /// 
    /// 오디오:
    /// - chromaticClips(12개 C~B) 또는 baseClipForPitchShift(단일 톤) 사용
    /// </summary>
    public class G2MelodyDirection : MonoBehaviour, IPausableGame {
        public enum TaskType { UpDownSimple = 0, HillValley = 1, TripleChange = 2 }

        [Header("UI (Assign in Inspector)")]
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button   leftButton;
        [SerializeField] TMP_Text leftLabel;
        [SerializeField] Button   rightButton;
        [SerializeField] TMP_Text rightLabel;
        [SerializeField] Button   replayButton;

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [Tooltip("C, C#, D, D#, E, F, F#, G, G#, A, A#, B 순서")]
        [SerializeField] AudioClip[] chromaticClips;
        [SerializeField] bool usePitchShiftIfClipsMissing = true;
        [SerializeField] AudioClip baseClipForPitchShift;

        [Header("Timing")]
        [SerializeField, Range(0.1f, 1.5f)] float noteDuration = 0.5f;
        [SerializeField, Range(0.0f, 0.8f)] float gapBetweenNotes = 0.12f;
        [SerializeField, Range(0.0f, 2.0f)] float preTrialDelay = 0.25f;
        [SerializeField, Range(0.0f, 0.5f)] float postPreviewDelay = 0.05f;
        [SerializeField] bool useUnscaledTime = true;

        [Header("Rules")]
        [SerializeField, Min(1)] int  totalTrials = 8;
        [SerializeField] int stepSemitones = 2;              // 기본 멜로디 이동폭(반음 수)
        [SerializeField] TaskType task = TaskType.UpDownSimple;

        // 상태
        int   _trialIndex;
        int   _correct;
        float _sumReaction;

        bool  _paused;
        bool  _previewPlaying;
        bool  _replaying;
        bool  _waitingForAnswer;
        float _reactionTimer;

        // 정답 클래스: 0 = Left, 1 = Right
        int   _answerClass;
        int[] _lastSeq; // 마지막 프리뷰 멜로디의 (0..11) 인덱스 배열

        public System.Action<int, bool, float> OnTrialEnd;
        public System.Action<int, int, float> OnGameFinished;

        void Awake() {
            if (leftButton)  leftButton.onClick.AddListener(() => Submit(0));
            if (rightButton) rightButton.onClick.AddListener(() => Submit(1));
            if (replayButton) replayButton.onClick.AddListener(OnReplay);

            SetButtons(false);
            SetReplay(false);
            if (promptText && string.IsNullOrEmpty(promptText.text)) promptText.text = "멜로디를 듣고 방향을 고르세요";
        }

        // ---- 외부 주입 API ----
        public void SetTask(TaskType t) { task = t; }
        public void SetStep(int semitones) { stepSemitones = Mathf.Clamp(semitones, 1, 6); }
        public void SetTotalTrials(int n) { totalTrials = Mathf.Max(1, n); }
        public void ConfigureLabels(string left, string right, string prompt = null) {
            if (leftLabel)  leftLabel.text  = left;
            if (rightLabel) rightLabel.text = right;
            if (promptText && !string.IsNullOrEmpty(prompt)) promptText.text = prompt;
        }
        public void SetUseUnscaledTime(bool on) { useUnscaledTime = on; }

        public void StartGame() {
            StopAllCoroutines();
            _trialIndex = 0; _correct = 0; _sumReaction = 0f;
            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop() {
            yield return WaitSmart(preTrialDelay);

            while (_trialIndex < totalTrials) {
                UpdateProgressUI();

                // 이번 라운드 목표 클래스(왼쪽/오른쪽)를 랜덤 결정
                _answerClass = Random.value < 0.5f ? 0 : 1;

                // 멜로디 시퀀스 생성 및 프리뷰 재생
                _lastSeq = BuildMelodySequence(task, _answerClass, stepSemitones);
                yield return PlayPreview(_lastSeq);

                // 선택 대기 진입
                _waitingForAnswer = true;
                _reactionTimer = 0f;
                SetButtons(true);
                SetReplay(true);

                // 응답 대기(재생/일시정지 중에는 반응시간 정지)
                while (_waitingForAnswer) {
                    if (!_paused && !_previewPlaying && !_replaying) _reactionTimer += Dt();
                    yield return null;
                }

                SetReplay(false);
                yield return WaitSmart(preTrialDelay);
            }

            float avg = (_trialIndex > 0) ? _sumReaction / _trialIndex : 0f;
            OnGameFinished?.Invoke(_trialIndex, _correct, avg);
        }

        // ----- Melody generation -----
        // 반환: 멜로디 음의 0..11 인덱스 배열, 정답 클래스는 _answerClass에 저장됨
        int[] BuildMelodySequence(TaskType type, int answerClass, int step) {
            // 방향 패턴 정의에 맞춰 offsets 생성
            int[] offsets;
            switch (type) {
                case TaskType.UpDownSimple:
                    // 3음: 상승(0, +s, +2s) / 하강(0, -s, -2s)
                    offsets = (answerClass == 0) ? new[] { 0, +step, +2*step }  // Left=상승
                                                : new[] { 0, -step, -2*step }; // Right=하강
                    break;

                case TaskType.HillValley:
                    // 3음: 산(0, +s, 0) / 골짜기(0, -s, 0)
                    offsets = (answerClass == 0) ? new[] { 0, +step, 0 }   // Left=산
                                                : new[] { 0, -step, 0 }; // Right=골짜기
                    break;

                case TaskType.TripleChange:
                default:
                    // 4음: 상승→하강→상승(0, +s, 0, +s) / 하강→상승→하강(0, -s, 0, -s)
                    offsets = (answerClass == 0) ? new[] { 0, +step, 0, +step }   // Left
                                                : new[] { 0, -step, 0, -step }; // Right
                    break;
            }

            // 루트 선택: 전체 오프셋이 0..11을 벗어나지 않도록 여유 범위 안에서 선택
            int minOff = 0, maxOff = 0;
            for (int i = 0; i < offsets.Length; i++) { minOff = Mathf.Min(minOff, offsets[i]); maxOff = Mathf.Max(maxOff, offsets[i]); }
            int minRoot = Mathf.Max(0, -minOff);
            int maxRoot = Mathf.Min(11, 11 - maxOff);
            int root = Random.Range(minRoot, maxRoot + 1);

            // 실제 음 인덱스로 변환
            int[] seq = new int[offsets.Length];
            for (int i = 0; i < offsets.Length; i++) seq[i] = root + offsets[i];
            return seq;
        }

        // ----- Playback -----
        IEnumerator PlayPreview(int[] seq) {
            _previewPlaying = true;
            SetButtons(false);
            SetReplay(false);

            for (int i = 0; i < seq.Length; i++) {
                PlayNote(seq[i]);
                yield return WaitSmart(noteDuration);
                if (i < seq.Length - 1) yield return WaitSmart(gapBetweenNotes);
            }

            yield return WaitSmart(postPreviewDelay);
            _previewPlaying = false;
        }

        void OnReplay() {
            if (_paused || _previewPlaying || _replaying || !_waitingForAnswer || _lastSeq == null) return;
            StartCoroutine(CoReplay());
        }

        IEnumerator CoReplay() {
            _replaying = true;
            SetButtons(false);
            SetReplay(false);

            for (int i = 0; i < _lastSeq.Length; i++) {
                PlayNote(_lastSeq[i]);
                yield return WaitSmart(noteDuration);
                if (i < _lastSeq.Length - 1) yield return WaitSmart(gapBetweenNotes);
            }

            SetButtons(true);
            SetReplay(true);
            _replaying = false;
        }

        void PlayNote(int index) {
            if (!audioSource) return;
            audioSource.Stop();

            if (chromaticClips != null && chromaticClips.Length > 0) {
                int i = Mathf.Clamp(index, 0, chromaticClips.Length - 1);
                var clip = chromaticClips[i];
                if (clip) {
                    audioSource.pitch = 1f;
                    audioSource.clip = clip;
                    audioSource.Play();
                    return;
                }
            }

            if (usePitchShiftIfClipsMissing && baseClipForPitchShift) {
                // baseClip를 12-TET 기준으로 시프트
                audioSource.clip = baseClipForPitchShift;
                float semis = index % 12;
                audioSource.pitch = Mathf.Pow(2f, semis / 12f);
                audioSource.Play();
            }
        }

        // ----- Answer -----
        void Submit(int pickedClass) {
            if (!_waitingForAnswer || _paused || _previewPlaying || _replaying) return;

            bool ok = (pickedClass == _answerClass);
            if (ok) _correct++;
            _sumReaction += Mathf.Max(0f, _reactionTimer);

            OnTrialEnd?.Invoke(_trialIndex + 1, ok, _reactionTimer);
            _trialIndex++;

            SetButtons(false);
            SetReplay(false);
            _waitingForAnswer = false;

            if (promptText) promptText.text = ok ? "정답" : "오답";
        }

        void SetButtons(bool on) {
            if (leftButton)  leftButton.interactable  = on;
            if (rightButton) rightButton.interactable = on;
        }
        void SetReplay(bool on) { if (replayButton) replayButton.interactable = on; }
        void UpdateProgressUI() {
            if (progressText) progressText.text = $"{Mathf.Min(_trialIndex, totalTrials)}/{totalTrials}";
        }

        // ----- Pause API -----
        public void Pause() {
            _paused = true;
            SetButtons(false);
            SetReplay(false);
            if (audioSource && audioSource.isPlaying) audioSource.Pause();
        }
        public void Resume() {
            _paused = false;
            if (_waitingForAnswer && !_previewPlaying && !_replaying) {
                SetButtons(true);
                SetReplay(true);
            }
            if (audioSource) audioSource.UnPause();
        }
        public bool IsPaused => _paused;

        // ----- Timing helpers -----
        float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        IEnumerator WaitSmart(float sec) {
            float t = 0f; sec = Mathf.Max(0f, sec);
            while (t < sec) { if (!_paused) t += Dt(); yield return null; }
        }
    }
}