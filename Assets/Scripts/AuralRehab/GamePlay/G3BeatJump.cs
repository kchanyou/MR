using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G3: 박자에 맞춰 점프(탭)하기
    /// - 카운트인(프리카운트) 후 패턴 비트를 재생
    /// - 플레이어는 비트 타이밍에 맞춰 큰 버튼을 탭
    /// - 허용 오차(히트 윈도우) 안의 탭을 정답으로 인정
    /// - 한 트라이얼 = 하나의 패턴(비트 N개, BPM 고정)
    /// - 트라이얼 성공 판정: 적중률(hitRatio) >= requiredHitRatio
    /// - 반응지표: 적중한 비트들의 평균 절대 타이밍 오차(초)
    /// </summary>
    public class G3BeatJump : MonoBehaviour, IPausableGame {
        [System.Serializable]
        public struct Pattern {
            public int beats;   // 이 패턴의 비트 수(정수)
            public int bpm;     // 이 패턴의 BPM
            public bool accentFirst; // 첫 박 강세 클릭 사용
            public Pattern(int beats, int bpm, bool accentFirst = true) {
                this.beats = Mathf.Max(1, beats);
                this.bpm = Mathf.Max(20, bpm);
                this.accentFirst = accentFirst;
            }
        }

        [Header("UI (Assign in Inspector)")]
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] TMP_Text bpmText;
        [SerializeField] Button   tapButton;      // 큰 탭 버튼
        [SerializeField] Image    pulseImage;     // 비주얼 펄스(선택)

        [Header("Audio (Metronome)")]
        [SerializeField] AudioSource audioSource; // OneShot 재생 권장
        [SerializeField] AudioClip   clickNormal;
        [SerializeField] AudioClip   clickAccent;

        [Header("Timing")]
        [SerializeField, Range(0.05f, 0.30f)] float hitWindow = 0.15f;     // 허용오차(초)
        [SerializeField, Range(0.00f, 2.00f)] float preTrialDelay = 0.25f; // 트라이얼 시작 전 대기
        [SerializeField] int countInBeats = 4;                              // 프리카운트 박 수
        [SerializeField] bool useUnscaledTime = true;

        [Header("Rules")]
        [SerializeField, Min(1)] int totalTrials = 8;
        [SerializeField, Range(0.1f, 1f)] float requiredHitRatio = 0.6f; // 트라이얼 성공 기준

        // 외부에서 스테이지별 패턴을 세팅
        List<Pattern> _patterns = new List<Pattern>();

        // 상태
        int   _trialIndex;
        int   _trialBeats;
        float _sumAvgAbsErr; // 트라이얼 평균 오차 누적(게임 전체 평균용)
        int   _trialCorrectCount; // 트라이얼 단위 성공 개수 누적(게임 전체 요약용)
        bool  _paused;
        bool  _acceptInput;

        // 타임라인
        float _elapsed;             // 이 트라이얼 경과 시간(일시정지 시 정지)
        List<float> _beatTimes = new List<float>(); // 패턴 비트의 발생 시각(초)들
        bool[] _hit;                // 각 비트의 적중 여부
        float[] _err;               // 각 비트의 절대오차

        // UI 효과 원본
        Color _pulseOrigColor = Color.white;
        Vector3 _pulseOrigScale = Vector3.one;

        public System.Action<int, bool, float> OnTrialEnd;     // (trial#, success, trialAvgAbsErr)
        public System.Action<int, int, float> OnGameFinished;  // (totalTrials, correctTrials, avgAbsErrAcrossTrials)

        void Awake() {
            if (tapButton) tapButton.onClick.AddListener(OnTap);
            if (pulseImage) {
                _pulseOrigColor = pulseImage.color;
                _pulseOrigScale = pulseImage.transform.localScale;
            }
            _acceptInput = false;
            if (promptText && string.IsNullOrEmpty(promptText.text))
                promptText.text = "비트에 맞춰 탭하세요";
        }

        // ---------- 외부 주입 API ----------
        public void SetPatterns(IEnumerable<Pattern> patterns) {
            _patterns.Clear();
            if (patterns != null) _patterns.AddRange(patterns);
        }
        public void SetHitWindowSeconds(float seconds) { hitWindow = Mathf.Clamp(seconds, 0.05f, 0.30f); }
        public void SetRequiredHitRatio(float ratio) { requiredHitRatio = Mathf.Clamp01(ratio); }
        public void SetTotalTrials(int n) { totalTrials = Mathf.Max(1, n); }
        public void SetUseUnscaledTime(bool on) { useUnscaledTime = on; }

        public void StartGame() {
            StopAllCoroutines();
            _trialIndex = 0;
            _trialCorrectCount = 0;
            _sumAvgAbsErr = 0f;
            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop() {
            yield return WaitSmart(preTrialDelay);

            while (_trialIndex < totalTrials) {
                var pattern = GetPatternForTrial(_trialIndex);
                _trialBeats = pattern.beats;

                // UI
                if (bpmText) bpmText.text = $"{pattern.bpm} BPM";
                UpdateProgressText();

                // 캡션
                AuralRehab.Application.ServiceHub.I.Caption.ShowTop($"{pattern.bpm} BPM에 맞춰 탭하세요");

                // 타임라인 세팅
                BuildTimeline(pattern);

                // 카운트인 + 패턴 재생 루프
                yield return RunTrial(pattern);

                // 성과 집계
                int hits = 0; float sumErr = 0f;
                for (int i = 0; i < _hit.Length; i++) if (_hit[i]) { hits++; sumErr += _err[i]; }
                float hitRatio = (_hit.Length > 0) ? (hits / (float)_hit.Length) : 0f;
                bool success = hitRatio >= requiredHitRatio;
                float avgErr = (hits > 0) ? (sumErr / hits) : hitWindow; // 맞춘 게 없으면 hitWindow로 대체

                if (success) _trialCorrectCount++;
                _sumAvgAbsErr += avgErr;

                OnTrialEnd?.Invoke(_trialIndex + 1, success, avgErr);
                _trialIndex++;

                UpdateProgressText();
                yield return WaitSmart(preTrialDelay);
            }

            float gameAvgErr = (_trialIndex > 0) ? (_sumAvgAbsErr / _trialIndex) : 0f;
            OnGameFinished?.Invoke(_trialIndex, _trialCorrectCount, gameAvgErr);
        }

        // 현재 트라이얼의 타임라인 구축
        void BuildTimeline(Pattern p) {
            _elapsed = 0f;
            _acceptInput = false;
            _beatTimes.Clear();

            float interval = 60f / Mathf.Max(20, p.bpm);

            // 카운트인
            for (int i = 0; i < Mathf.Max(0, countInBeats); i++) {
                _beatTimes.Add(i * interval);
            }
            float startPatternT = _beatTimes.Count > 0 ? _beatTimes[_beatTimes.Count - 1] + interval : 0f;

            // 패턴 본 비트
            for (int i = 0; i < p.beats; i++) {
                _beatTimes.Add(startPatternT + i * interval);
            }

            // 스코어링 대상 비트는 "카운트인 이후" 구간만
            int scoringCount = p.beats;
            _hit = new bool[scoringCount];
            _err = new float[scoringCount];
            for (int i = 0; i < scoringCount; i++) { _hit[i] = false; _err[i] = 0f; }
        }

        IEnumerator RunTrial(Pattern p) {
            int nextBeatIdx = 0;
            float interval = 60f / Mathf.Max(20, p.bpm);
            int scoringStartIndex = Mathf.Max(0, countInBeats);
            int scoringEndIndexExclusive = _beatTimes.Count; // 전체 길이(카운트인 + 패턴)

            // 프리카운트 끝나면 입력 허용
            bool inputJustEnabled = false;

            while (nextBeatIdx < _beatTimes.Count) {
                // 시간 진행
                if (!_paused) _elapsed += Dt();

                // 다음 비트 도달 체크
                while (nextBeatIdx < _beatTimes.Count && _elapsed + 1e-5f >= _beatTimes[nextBeatIdx]) {
                    bool accent = false;
                    if (nextBeatIdx < countInBeats) {
                        // 카운트인: 1박째 강세
                        accent = (nextBeatIdx % countInBeats == 0);
                    } else {
                        // 패턴: 첫 박 강세 옵션
                        accent = p.accentFirst && ((nextBeatIdx - countInBeats) % p.beats == 0);
                    }

                    Pulse(accent, 0.08f, 1.08f);
                    PlayClick(accent);

                    nextBeatIdx++;
                }

                // 카운트인 종료 시 입력 허용
                if (!inputJustEnabled && _elapsed >= (countInBeats * interval) - 1e-5f) {
                    _acceptInput = true;
                    inputJustEnabled = true;
                }

                yield return null;
            }

            // 패턴 끝난 뒤 약간의 여유
            yield return WaitSmart(0.15f);
            _acceptInput = false;
        }

        void OnTap() {
            if (!_acceptInput || _paused) return;
            // 현재 탭 시각(트라이얼 기준)
            float t = _elapsed;

            // 스코어링 대상 비트 범위
            int scoringStart = Mathf.Max(0, countInBeats);
            int scoringBeats = _hit.Length;

            // 가장 가까운 "아직 미스코어" 비트를 찾되, 허용오차 안만 인정
            int bestIdx = -1;
            float bestAbs = float.MaxValue;

            for (int i = 0; i < scoringBeats; i++) {
                if (_hit[i]) continue;
                int absoluteIdx = scoringStart + i;
                float bt = _beatTimes[absoluteIdx];
                float d = Mathf.Abs(t - bt);
                if (d < bestAbs) {
                    bestAbs = d;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0 && bestAbs <= hitWindow) {
                _hit[bestIdx] = true;
                _err[bestIdx] = bestAbs;
                // 간단 피드백(선택): 버튼 색상 살짝 점멸
                FlashTapButton(0.08f);
            } else {
                // 오차가 크면 무시(원하면 '미스' 효과를 줄 수 있음)
                FlashTapButton(0.08f);
            }
        }

        void PlayClick(bool accent) {
            if (!audioSource) return;
            var clip = accent && clickAccent ? clickAccent : clickNormal;
            if (!clip) return;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(clip, 1f);
        }

        void Pulse(bool accent, float dur, float scale) {
            if (!pulseImage) return;
            StopCoroutine(nameof(CoPulse));
            StartCoroutine(CoPulse(accent, dur, scale));
        }

        IEnumerator CoPulse(bool accent, float dur, float scale) {
            var tg = pulseImage;
            if (!tg) yield break;

            Color start = _pulseOrigColor;
            Color end   = start;
            end.a = Mathf.Clamp01(start.a * (accent ? 1.8f : 1.35f));

            Vector3 s0 = _pulseOrigScale;
            Vector3 s1 = s0 * (accent ? scale * 1.05f : scale);

            float t = 0f;
            while (t < dur) {
                if (!_paused) t += Dt();
                float k = Mathf.Clamp01(t / dur);
                k = k * k * (3f - 2f * k);
                tg.color = Color.Lerp(start, end, k);
                tg.transform.localScale = Vector3.Lerp(s0, s1, k);
                yield return null;
            }
            tg.color = _pulseOrigColor;
            tg.transform.localScale = _pulseOrigScale;
        }

        void FlashTapButton(float dur) {
            if (!tapButton) return;
            var g = tapButton.targetGraphic;
            if (!g) return;
            StopCoroutine(nameof(CoFlash));
            StartCoroutine(CoFlash(g, dur));
        }
        IEnumerator CoFlash(Graphic g, float dur) {
            Color start = g.color;
            Color end = start; end.a = Mathf.Clamp01(start.a * 0.6f);
            float t = 0f;
            while (t < dur) {
                if (!_paused) t += Dt();
                float k = Mathf.Clamp01(t / dur);
                k = k * k * (3f - 2f * k);
                g.color = Color.Lerp(start, end, k);
                yield return null;
            }
            g.color = start;
        }

        void UpdateProgressText() {
            if (progressText) progressText.text = $"{Mathf.Min(_trialIndex, totalTrials)}/{totalTrials}";
        }

        // ---------- Pause ----------
        public void Pause() { _paused = true; if (tapButton) tapButton.interactable = false; if (audioSource) audioSource.Pause(); }
        public void Resume() { _paused = false; if (_acceptInput && tapButton) tapButton.interactable = true; if (audioSource) audioSource.UnPause(); }
        public bool IsPaused => _paused;

        // ---------- Helpers ----------
        Pattern GetPatternForTrial(int idx) {
            if (_patterns == null || _patterns.Count == 0) return new Pattern(4, 60, true);
            return _patterns[idx % _patterns.Count];
        }

        float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        IEnumerator WaitSmart(float sec) {
            float t = 0f; sec = Mathf.Max(0f, sec);
            while (t < sec) { if (!_paused) t += Dt(); yield return null; }
        }
    }
}