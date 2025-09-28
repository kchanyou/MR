using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// 2지선다 스텁(일시정지 대응)
    /// - 라운드 수, 라벨만 설정하여 테스트용으로 사용
    /// </summary>
    public class TwoChoiceMinigame : MonoBehaviour, IPausableGame {
        [Header("UI Refs (Assign in Inspector)")]
        [SerializeField] TMP_Text promptText;
        [SerializeField] Button   leftButton;
        [SerializeField] TMP_Text leftLabel;
        [SerializeField] Button   rightButton;
        [SerializeField] TMP_Text rightLabel;
        [SerializeField] TMP_Text progressText;

        [Header("Config")]
        [SerializeField] int totalTrials = 5;
        [SerializeField] float interTrialDelay = 0.6f;
        [SerializeField] bool useUnscaledTime = true;

        int _trialIndex;
        int _correct;
        float _sumReaction;
        bool _waiting;
        bool _paused;
        int _answerSide;
        float _reactionTimer;

        public System.Action<int, bool, float> OnTrialEnd;
        public System.Action<int, int, float> OnGameFinished;

        void Awake() {
            if (leftButton)  leftButton.onClick.AddListener(() => Submit(0));
            if (rightButton) rightButton.onClick.AddListener(() => Submit(1));
            SetInteractable(false);
        }

        public void ConfigureLabels(string left, string right, string promptHint = null) {
            if (leftLabel)  leftLabel.text  = left;
            if (rightLabel) rightLabel.text = right;
            if (promptText && !string.IsNullOrEmpty(promptHint)) promptText.text = promptHint;
        }

        public void StartGame() {
            StopAllCoroutines();
            _trialIndex = 0; _correct = 0; _sumReaction = 0f;
            StartCoroutine(RunLoop());
        }

        IEnumerator RunLoop() {
            yield return WaitSmart(0.2f);

            while (_trialIndex < totalTrials) {
                _answerSide = Random.value < 0.5f ? 0 : 1;

                // 준비
                _waiting = true;
                _reactionTimer = 0f;
                SetInteractable(true);
                UpdateProgressUI();

                // 응답 대기(일시정지 시 타이머 정지)
                while (_waiting) {
                    if (!_paused) _reactionTimer += Dt();
                    yield return null;
                }

                // 라운드 간 대기
                yield return WaitSmart(interTrialDelay);
            }

            float avg = (_trialIndex > 0) ? _sumReaction / _trialIndex : 0f;
            OnGameFinished?.Invoke(_trialIndex, _correct, avg);
        }

        void Submit(int side) {
            if (!_waiting || _paused) return;
            bool ok = (side == _answerSide);
            if (ok) _correct++;
            _sumReaction += Mathf.Max(0f, _reactionTimer);
            OnTrialEnd?.Invoke(_trialIndex + 1, ok, _reactionTimer);
            _trialIndex++;
            SetInteractable(false);
            _waiting = false;
            UpdateProgressUI();
        }

        void SetInteractable(bool on) {
            if (leftButton)  leftButton.interactable  = on;
            if (rightButton) rightButton.interactable = on;
        }

        void UpdateProgressUI() {
            if (progressText) progressText.text = $"{Mathf.Min(_trialIndex, totalTrials)}/{totalTrials}";
        }

        // Pause API
        public void Pause() { _paused = true; SetInteractable(false); }
        public void Resume() { _paused = false; if (_waiting) SetInteractable(true); }
        public bool IsPaused => _paused;

        // Timing
        float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        IEnumerator WaitSmart(float sec) {
            float t = 0f; sec = Mathf.Max(0f, sec);
            while (t < sec) { if (!_paused) t += Dt(); yield return null; }
        }
    }
}