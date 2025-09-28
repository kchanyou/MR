using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G3: 박자에 맞춰 탭하기 (프리뷰 → 간격 → 스코어링)
    /// - 프리뷰: 카운트인+패턴 재생, 입력/판정 없음
    /// - 간격(gapAfterPreview) 대기
    /// - 스코어링: (옵션 카운트인 후) 동일 패턴 재생, 입력/판정 활성
    /// - 인스펙터에서 오디오/시각/판정 타이밍을 보정 가능
    /// </summary>
    public class G3BeatJump : MonoBehaviour, IPausableGame {
        [System.Serializable]
        public struct Pattern {
            public int beats;   // 패턴 비트 수
            public int bpm;     // BPM
            public bool accentFirst; // 첫 박 강세
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

        [Header("Visual Pulse (Optional)")]
        [SerializeField] Image    pulseImage;     // 비주얼 펄스(선택)

        [Header("Track Visuals")]
        [SerializeField] RectTransform beatTrack; // 가로 트랙
        [SerializeField] Image markerPrefab;      // 비트 마커 프리팹(Image)
        [SerializeField] RectTransform cursor;    // 진행 커서(작은 이미지 Rect)
        [SerializeField] TMP_Text countInText;    // 3,2,1,Go
        [SerializeField] TMP_Text judgeText;      // Perfect/Good/Ok/Miss

        [Header("Track Colors")]
        [SerializeField] Color markerIdle  = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] Color markerBeat  = Color.white;
        [SerializeField] Color markerHit   = new Color(0.2f, 1f, 0.4f);
        [SerializeField] Color markerMiss  = new Color(1f, 0.35f, 0.35f);

        [Header("Judgement Colors")]
        [SerializeField] Color colorPerfect = new Color(0.4f, 1f, 0.8f);
        [SerializeField] Color colorGood    = new Color(0.6f, 0.9f, 1f);
        [SerializeField] Color colorOk      = new Color(1f, 0.9f, 0.5f);
        [SerializeField] Color colorMiss    = new Color(1f, 0.5f, 0.5f);
        [SerializeField, Range(0.05f, 1.0f)] float judgeFadeDuration = 0.25f;

        [Header("Audio (Metronome)")]
        [SerializeField] AudioSource audioSource; // OneShot 재생 권장
        [SerializeField] AudioClip   clickNormal;
        [SerializeField] AudioClip   clickAccent;

        [Header("Timing (Core)")]
        [SerializeField, Range(0.05f, 0.30f)] float hitWindow = 0.15f;     // 허용오차(초)
        [SerializeField, Range(0.00f, 2.00f)] float preTrialDelay = 0.25f; // 트라이얼 시작 전 대기
        [SerializeField] int countInBeats = 4;                              // 프리뷰 카운트인 박 수
        [SerializeField] bool useUnscaledTime = true;

        [Header("Timing (Compensation & Flow)")]
        [Tooltip("오디오 클릭을 미리 내보낼 시간(초). 오디오가 늦게 들리면 값을 늘리세요.")]
        [SerializeField, Range(-0.2f, 0.2f)] float audioAdvance = 0f;
        [Tooltip("마커/펄스를 미리 보여줄 시간(초).")]
        [SerializeField, Range(-0.2f, 0.2f)] float visualAdvance = 0f;
        [Tooltip("판정 타이밍을 이동(초). 오디오가 늦게 들리면 값을 늘리세요(= 목표시각을 뒤로).")]
        [SerializeField, Range(-0.2f, 0.2f)] float judgeTimeOffset = 0f;
        [Tooltip("프리뷰가 끝난 뒤 스코어링 시작까지 간격(초). 구분감 부여용.")]
        [SerializeField, Range(0f, 3f)] float gapAfterPreview = 1.0f;
        [Tooltip("스코어링 패스에 사용할 카운트인 박 수(0=없음).")]
        [SerializeField, Range(0, 8)] int scoringCountInBeats = 0;

        [Header("Rules")]
        [SerializeField, Min(1)] int totalTrials = 8;
        [SerializeField, Range(0.1f, 1f)] float requiredHitRatio = 0.6f; // 트라이얼 성공 기준

        // 외부에서 스테이지별 패턴을 세팅
        List<Pattern> _patterns = new List<Pattern>();

        // 상태(게임 전체)
        int   _trialIndex;
        float _sumAvgAbsErr; // 트라이얼 평균 오차 누적
        int   _trialCorrectCount; // 성공한 트라이얼 수
        bool  _paused;

        // 상태(패스 단위: 프리뷰/스코어링)
        bool  _acceptInput;
        float _elapsed;                   // 이 패스 경과 시간
        int   _countInThisPass;           // 카운트인(이 패스)
        List<float> _beatTimes = new List<float>(); // 이 패스의 모든 목표시각(카운트인+본 비트)

        // 스코어링용 버퍼(스코어링 패스에서만 사이즈 > 0)
        bool[] _hit;    // 각 본 비트의 적중 여부(스코어링)
        bool[] _miss;   // 각 본 비트의 미스 여부(윈도 지남)
        float[] _err;   // 각 본 비트의 절대오차

        // 시각화(마커)
        readonly List<Image> _markers = new List<Image>(); // 본 비트 개수만큼 생성
        RectTransform _trackRect;
        float _patternStartT; // 본 비트 첫 시각(이 패스 기준)
        float _patternEndT;   // 본 비트 마지막 시각(이 패스 기준)

        // UI 효과 원본
        Color _pulseOrigColor = Color.white;
        Vector3 _pulseOrigScale = Vector3.one;
        Coroutine _coJudge;

        public System.Action<int, bool, float> OnTrialEnd;     // (trial#, success, trialAvgAbsErr)
        public System.Action<int, int, float> OnGameFinished;  // (totalTrials, correctTrials, avgAbsErrAcrossTrials)

        void Awake() {
            if (tapButton) tapButton.onClick.AddListener(OnTap);
            if (pulseImage) {
                _pulseOrigColor = pulseImage.color;
                _pulseOrigScale = pulseImage.transform.localScale;
            }
            if (promptText && string.IsNullOrEmpty(promptText.text)) promptText.text = "비트에 맞춰 탭하세요";
            if (judgeText) { judgeText.text = ""; judgeText.alpha = 0f; }
            if (countInText) { countInText.text = ""; countInText.alpha = 0f; }
            _trackRect = beatTrack ? beatTrack : null;
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

                // UI
                if (bpmText) bpmText.text = $"{pattern.bpm} BPM";
                UpdateProgressText();
                AuralRehab.Application.ServiceHub.I.Caption.ShowTop($"{pattern.bpm} BPM • 프리뷰 후 따라 탭하세요");

                // 1) 프리뷰 패스 (카운트인 = countInBeats, 입력/판정 없음)
                BuildTimeline(pattern, countInBeats);
                BuildOrRefreshMarkers(pattern);            // 마커는 프리뷰에도 생성/정렬(색상 idle)
                yield return RunPass(pattern, scoring:false);

                // 2) 간격 대기
                yield return WaitSmart(Mathf.Max(0f, gapAfterPreview));

                // 3) 스코어링 패스 (카운트인 = scoringCountInBeats, 입력/판정 포함)
                BuildTimeline(pattern, Mathf.Max(0, scoringCountInBeats));
                // 스코어링 버퍼 초기화
                _hit  = new bool[pattern.beats];
                _miss = new bool[pattern.beats];
                _err  = new float[pattern.beats];
                for (int i = 0; i < pattern.beats; i++) { _hit[i] = false; _miss[i] = false; _err[i] = 0f; }
                // 마커 색 초기화
                ResetMarkersToIdle(pattern.beats);

                yield return RunPass(pattern, scoring:true);

                // 성과 집계(스코어링 패스 결과)
                int hits = 0; float sumErr = 0f;
                for (int i = 0; i < _hit.Length; i++) if (_hit[i]) { hits++; sumErr += _err[i]; }
                float hitRatio = (_hit.Length > 0) ? (hits / (float)_hit.Length) : 0f;
                bool success = hitRatio >= requiredHitRatio;
                float avgErr = (hits > 0) ? (sumErr / hits) : hitWindow;

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

        // ---- 타임라인 구축(패스 단위) ----
        void BuildTimeline(Pattern p, int countInForThisPass) {
            _elapsed = 0f;
            _acceptInput = false;
            _countInThisPass = Mathf.Max(0, countInForThisPass);
            _beatTimes.Clear();

            float interval = 60f / Mathf.Max(20, p.bpm);

            // 카운트인
            for (int i = 0; i < _countInThisPass; i++) {
                _beatTimes.Add(i * interval);
            }
            _patternStartT = (_beatTimes.Count > 0) ? _beatTimes[_beatTimes.Count - 1] + interval : 0f;

            // 본 비트
            for (int i = 0; i < p.beats; i++) {
                _beatTimes.Add(_patternStartT + i * interval);
            }
            _patternEndT = _patternStartT + (p.beats - 1) * interval;

            // UI 초기화
            if (judgeText) { judgeText.text = ""; judgeText.alpha = 0f; }
            if (cursor) cursor.gameObject.SetActive(_trackRect != null);
            if (countInText) { countInText.text = ""; countInText.alpha = 0f; }
        }

        // ---- 마커 생성/정렬/초기화 ----
        void BuildOrRefreshMarkers(Pattern p) {
            if (beatTrack == null || markerPrefab == null) return;

            while (_markers.Count < p.beats) {
                var m = Instantiate(markerPrefab, beatTrack);
                _markers.Add(m);
            }
            while (_markers.Count > p.beats) {
                var last = _markers[_markers.Count - 1];
                if (last) Destroy(last.gameObject);
                _markers.RemoveAt(_markers.Count - 1);
            }

            float w = beatTrack.rect.width;
            for (int i = 0; i < p.beats; i++) {
                var m = _markers[i]; if (!m) continue;
                var rt = m.rectTransform;
                var anchored = rt.anchoredPosition;
                anchored.x = Mathf.Lerp(0f, w, (p.beats <= 1 ? 0f : i / (float)(p.beats - 1)));
                anchored.y = 0f;
                rt.anchoredPosition = anchored;
                m.color = markerIdle;
                rt.localScale = Vector3.one;
            }
        }
        void ResetMarkersToIdle(int beats) {
            for (int i = 0; i < beats && i < _markers.Count; i++) {
                var m = _markers[i]; if (!m) continue;
                m.color = markerIdle;
                m.rectTransform.localScale = Vector3.one;
            }
        }

        // ---- 패스 실행(프리뷰/스코어링 공용) ----
        IEnumerator RunPass(Pattern p, bool scoring) {
            // 오디오/비주얼 트리거 인덱스(카운트인+본비트 공통 타임라인 사용)
            int nextAudio = 0;
            int nextVisual = 0;
            int nextBeatForText = 0;

            float interval = 60f / Mathf.Max(20, p.bpm);
            bool inputJustEnabled = false;

            while (true) {
                if (!_paused) _elapsed += Dt();

                // 커서 이동(본 비트 구간만)
                UpdateCursor();

                // ----- 오디오 트리거 (audioAdvance만큼 미리 재생) -----
                while (nextAudio < _beatTimes.Count && _elapsed + 1e-5f >= _beatTimes[nextAudio] - audioAdvance) {
                    bool accent;
                    if (nextAudio < _countInThisPass) {
                        accent = (nextAudio % Mathf.Max(1, _countInThisPass) == 0);
                        ShowCountIn(nextAudio); // 카운트인 텍스트(프리뷰/스코어링 공용)
                    } else {
                        accent = p.accentFirst && ((nextAudio - _countInThisPass) % p.beats == 0);
                    }
                    PlayClick(accent);
                    nextAudio++;
                }

                // ----- 비주얼 트리거 (visualAdvance만큼 미리) -----
                while (nextVisual < _beatTimes.Count && _elapsed + 1e-5f >= _beatTimes[nextVisual] - visualAdvance) {
                    bool isPatternBeat = (nextVisual >= _countInThisPass);
                    if (isPatternBeat) {
                        int idx = nextVisual - _countInThisPass;
                        HighlightMarker(idx, 0.08f, 1.12f);
                        Pulse(p.accentFirst && (idx % p.beats == 0), 0.08f, 1.08f);
                    } else {
                        Pulse(false, 0.06f, 1.05f); // 카운트인 펄스(약하게)
                    }
                    nextVisual++;
                }

                // ----- 카운트인 종료 → 입력 허용(스코어링 패스에서만) -----
                if (scoring && !inputJustEnabled && _elapsed >= (_countInThisPass * interval) - 1e-5f) {
                    _acceptInput = true;
                    inputJustEnabled = true;
                    if (countInText) { countInText.text = ""; countInText.alpha = 0f; }
                }

                // ----- 스코어링: 미스 처리(판정 오프셋 고려) -----
                if (scoring) UpdateMisses(p);

                // 루프 종료: 모든 비주얼/오디오 처리 후, 타임라인 종료 판단
                if (nextVisual >= _beatTimes.Count && nextAudio >= _beatTimes.Count) {
                    // 여유 약간
                    yield return WaitSmart(0.15f);
                    _acceptInput = false;
                    break;
                }

                yield return null;
            }
        }

        void UpdateCursor() {
            if (beatTrack == null || cursor == null) return;
            float w = beatTrack.rect.width;

            if (_elapsed <= _patternStartT) {
                cursor.anchoredPosition = new Vector2(0f, cursor.anchoredPosition.y); return;
            }
            if (_elapsed >= _patternEndT) {
                cursor.anchoredPosition = new Vector2(w, cursor.anchoredPosition.y); return;
            }
            float t01 = Mathf.InverseLerp(_patternStartT, _patternEndT, _elapsed);
            cursor.anchoredPosition = new Vector2(Mathf.Lerp(0f, w, t01), cursor.anchoredPosition.y);
        }

        // 판정/미스는 judgeTimeOffset을 반영한 목표시각으로 처리
        float JudgeBeatTime(int beatIdxInPattern) {
            return _beatTimes[_countInThisPass + beatIdxInPattern] + judgeTimeOffset;
        }

        void UpdateMisses(Pattern p) {
            if (_hit == null || _miss == null) return;
            int beats = _hit.Length;
            for (int i = 0; i < beats; i++) {
                if (_hit[i] || _miss[i]) continue;
                float bt = JudgeBeatTime(i);
                if (_elapsed > bt + hitWindow + 1e-5f) {
                    _miss[i] = true;
                    SetMarkerColor(i, markerMiss);
                    ShowJudge("Miss", colorMiss);
                }
            }
        }

        void ShowCountIn(int beatIdxInThisPass) {
            if (countInText == null || _countInThisPass <= 0) return;
            if (beatIdxInThisPass >= _countInThisPass) return;

            // 예: 4박 카운트인 → 0:"3", 1:"2", 2:"1", 3:"Go"
            string msg = (beatIdxInThisPass < _countInThisPass - 1)
                ? ((_countInThisPass - 1) - beatIdxInThisPass).ToString()
                : "Go";
            countInText.text = msg;
            countInText.alpha = 1f;
            StopCoroutine(nameof(CoFadeTMP));
            StartCoroutine(CoFadeTMP(countInText, 0.2f));
        }

        IEnumerator CoFadeTMP(TMP_Text t, float dur) {
            float a0 = t.alpha, tt = 0f;
            while (tt < dur) {
                if (!_paused) tt += Dt();
                float k = Mathf.Clamp01(tt / dur);
                k = k * k * (3f - 2f * k);
                t.alpha = Mathf.Lerp(a0, 0f, k);
                yield return null;
            }
            t.alpha = 0f;
        }

        void HighlightMarker(int idx, float dur, float scale) {
            if (_markers == null || idx < 0 || idx >= _markers.Count) return;
            var m = _markers[idx]; if (!m) return;
            StopCoroutine(nameof(CoPulseMarker));
            StartCoroutine(CoPulseMarker(m.rectTransform, m, dur, scale));
        }

        IEnumerator CoPulseMarker(RectTransform rt, Image m, float dur, float scale) {
            var s0 = rt.localScale;
            var s1 = s0 * scale;
            var c0 = markerIdle;
            var c1 = markerBeat;

            float t = 0f;
            while (t < dur) {
                if (!_paused) t += Dt();
                float k = Mathf.Clamp01(t / dur);
                k = k * k * (3f - 2f * k);
                rt.localScale = Vector3.Lerp(s0, s1, k);
                m.color = Color.Lerp(c0, c1, k);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        void SetMarkerColor(int idx, Color c) {
            if (_markers == null || idx < 0 || idx >= _markers.Count) return;
            var m = _markers[idx]; if (!m) return;
            m.color = c;
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
            var tg = pulseImage; if (!tg) yield break;
            Color start = _pulseOrigColor;
            Color end = start; end.a = Mathf.Clamp01(start.a * (accent ? 1.8f : 1.35f));
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

        // ---- 입력 처리 & 판정 ----
        void OnTap() {
            if (!_acceptInput || _paused || _hit == null) return;

            float tNow = _elapsed;
            int beats = _hit.Length;

            int bestIdx = -1;
            float bestAbs = float.MaxValue;

            for (int i = 0; i < beats; i++) {
                if (_hit[i] || _miss[i]) continue;
                float bt = JudgeBeatTime(i); // 판정 시간 오프셋 반영
                float d = Mathf.Abs(tNow - bt);
                if (d < bestAbs) { bestAbs = d; bestIdx = i; }
            }

            if (bestIdx >= 0 && bestAbs <= hitWindow) {
                _hit[bestIdx] = true;
                _err[bestIdx] = bestAbs;
                SetMarkerColor(bestIdx, ColorForError(bestAbs));
                ShowJudgeTextForError(bestAbs);
                FlashTapButton(0.08f);
            } else {
                ShowJudge("Miss", colorMiss);
                FlashTapButton(0.08f);
            }
        }

        Color ColorForError(float absErr) {
            if (absErr <= 0.05f) return markerHit;                 // 초록
            if (absErr <= 0.10f) return new Color(0.5f, 0.9f, 1f); // 하늘색
            return new Color(1f, 0.85f, 0.5f);                     // 노랑
        }

        void ShowJudgeTextForError(float absErr) {
            if (absErr <= 0.05f) ShowJudge("Perfect", colorPerfect);
            else if (absErr <= 0.10f) ShowJudge("Good", colorGood);
            else ShowJudge("Ok", colorOk);
        }

        void ShowJudge(string txt, Color c) {
            if (!judgeText) return;
            if (_coJudge != null) StopCoroutine(_coJudge);
            _coJudge = StartCoroutine(CoJudge(txt, c));
        }

        IEnumerator CoJudge(string txt, Color c) {
            judgeText.text = txt;
            judgeText.color = c;
            judgeText.alpha = 1f;
            float t = 0f;
            while (t < judgeFadeDuration) {
                if (!_paused) t += Dt();
                float k = Mathf.Clamp01(t / judgeFadeDuration);
                k = k * k * (3f - 2f * k);
                judgeText.alpha = Mathf.Lerp(1f, 0f, k);
                yield return null;
            }
            judgeText.alpha = 0f;
        }

        void FlashTapButton(float dur) {
            if (!tapButton) return;
            var g = tapButton.targetGraphic; if (!g) return;
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
        public void Pause() {
            _paused = true;
            if (tapButton) tapButton.interactable = false;
            if (audioSource) audioSource.Pause();
        }
        public void Resume() {
            _paused = false;
            if (tapButton && _acceptInput) tapButton.interactable = true;
            if (audioSource) audioSource.UnPause();
        }
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