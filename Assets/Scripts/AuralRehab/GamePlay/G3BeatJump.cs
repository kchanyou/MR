using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G3: 박자에 맞춰 탭하기
    /// Flow: [프리뷰] → gapAfterPreview → [스코어링(필요 시 큐 사운드→1박 후 첫 비트)] → gapBeforePreview → 다음 트라이얼
    /// - 프리뷰: 패턴 비트 수(p.beats)만큼 들려줌(기본, 카운트인 없음)
    /// - 스코어링 시작 신호(큐): 해당 BPM의 1박 간격으로 첫 비트와 등간격 정렬
    ///   · count-in=0 → 내부적으로 1박 카운트인 추가(무음), 그 지점에 큐 사운드만 재생
    ///   · count-in>0 & showCueEvenWithCountIn → 마지막 count-in 비트를 큐 사운드로 대체(등간격 유지)
    /// - 인스펙터에서 오디오/시각/판정 보정 및 각 구간 공백시간 조정 가능
    /// </summary>
    public class G3BeatJump : MonoBehaviour, IPausableGame {
        [System.Serializable]
        public struct Pattern {
            public int beats;   // 패턴 비트 수(= 스코어링 탭 횟수)
            public int bpm;     // BPM
            public bool accentFirst;
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
        [SerializeField] Button   tapButton;

        [Header("Visual Pulse (Optional)")]
        [SerializeField] Image    pulseImage;

        [Header("Track Visuals")]
        [SerializeField] RectTransform beatTrack;
        [SerializeField] Image markerPrefab;
        [SerializeField] RectTransform cursor;
        [SerializeField] TMP_Text countInText;
        [SerializeField] TMP_Text judgeText;

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
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip   clickNormal;
        [SerializeField] AudioClip   clickAccent;

        [Header("Timing (Core)")]
        [SerializeField, Range(0.05f, 0.30f)] float hitWindow = 0.15f;
        [SerializeField, Range(0.00f, 2.00f)] float preTrialDelay = 0.25f; // 첫 프리뷰 전만 사용
        [SerializeField] int countInBeats = 4;
        [SerializeField] bool useUnscaledTime = true;

        [Header("Preview Options")]
        [Tooltip("프리뷰에서도 카운트인을 사용할지(기본 끔). 끄면 p.beats 만큼만 들려줍니다.")]
        [SerializeField] bool previewUseCountIn = false;

        [Header("Scoring Cue (Start Signal)")]
        [Tooltip("스코어링 시작 직전에 큐 사운드를 재생합니다. 첫 비트와의 간격은 BPM의 1박입니다.")]
        [SerializeField] bool useScoringCue = true;
        [Tooltip("count-in이 있어도 마지막 count-in 비트를 큐 사운드로 대체합니다.")]
        [SerializeField] bool showCueEvenWithCountIn = false;
        [SerializeField] AudioClip scoringCueClip;
        [SerializeField, Range(0f, 1f)] float scoringCueVolume = 1f;
        [Tooltip("큐 텍스트를 표시할 TMP. 비워두면 텍스트 미표시.")]
        [SerializeField] TMP_Text scoringCueText;
        [SerializeField] string scoringCueMessage = "시작";
        [SerializeField] Color scoringCueColor = Color.white;
        [SerializeField, Range(0.1f, 2f)] float scoringCueDuration = 0.6f;

        [Header("Timing (Compensation & Flow)")]
        [Tooltip("오디오 클릭을 미리 내보낼 시간(초). 오디오가 늦게 들리면 값을 늘리세요.")]
        [SerializeField, Range(-0.2f, 0.2f)] float audioAdvance = 0f;
        [Tooltip("마커/펄스를 미리 보여줄 시간(초).")]
        [SerializeField, Range(-0.2f, 0.2f)] float visualAdvance = 0f;
        [Tooltip("판정 타이밍을 이동(초). 오디오가 늦게 들리면 값을 늘리세요(= 목표시각을 뒤로).")]
        [SerializeField, Range(-0.2f, 0.2f)] float judgeTimeOffset = 0f;

        [Tooltip("프리뷰가 끝난 뒤 스코어링 시작까지 무음 간격(초).")]
        [SerializeField, Range(0f, 3f)] float gapAfterPreview = 2.0f;

        [Tooltip("스코어링이 끝난 뒤 다음 프리뷰 시작까지 무음 간격(초).")]
        [SerializeField, Range(0f, 3f)] float gapBeforePreview = 2.0f;

        [Tooltip("스코어링 패스의 카운트인 박 수(0=없음).")]
        [SerializeField, Range(0, 8)] int scoringCountInBeats = 0;

        [Header("Rules")]
        [SerializeField, Min(1)] int totalTrials = 8;
        [SerializeField, Range(0.1f, 1f)] float requiredHitRatio = 0.6f;

        // 패턴
        List<Pattern> _patterns = new List<Pattern>();

        // 상태(게임 전체)
        int   _trialIndex;
        float _sumAvgAbsErr;
        int   _trialCorrectCount;
        bool  _paused;

        // 상태(패스)
        bool  _acceptInput;
        float _elapsed;
        int   _countInThisPass;
        List<float> _beatTimes = new List<float>();

        // 스코어링 버퍼
        bool[] _hit;
        bool[] _miss;
        float[] _err;

        // 시각화
        readonly List<Image> _markers = new List<Image>();
        RectTransform _trackRect;
        float _patternStartT;
        float _patternEndT;

        // UI 효과 원본
        Color _pulseOrigColor = Color.white;
        Vector3 _pulseOrigScale = Vector3.one;
        Coroutine _coJudge;

        // 큐/카운트인 제어 플래그(패스별)
        bool _muteCountInAll;          // count-in 전체를 무음으로(=메트로놈 미재생)
        bool _cueOnLastCountInOnly;    // 마지막 count-in만 메트로놈 대신 큐 재생
        bool _playCueThisBeat;         // 현재 count-in 비트에서 큐를 재생해야 하는가

        public System.Action<int, bool, float> OnTrialEnd;
        public System.Action<int, int, float> OnGameFinished;

        void Awake() {
            if (tapButton) tapButton.onClick.AddListener(OnTap);
            if (pulseImage) {
                _pulseOrigColor = pulseImage.color;
                _pulseOrigScale = pulseImage.transform.localScale;
            }
            if (promptText && string.IsNullOrEmpty(promptText.text)) promptText.text = "비트에 맞춰 탭하세요";
            if (judgeText) { judgeText.text = ""; judgeText.alpha = 0f; }
            if (countInText) { countInText.text = ""; countInText.alpha = 0f; }
            if (scoringCueText) { scoringCueText.text = ""; scoringCueText.alpha = 0f; }
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

                // 1) 프리뷰 패스
                BuildTimeline(pattern, previewUseCountIn ? Mathf.Max(0, countInBeats) : 0);
                BuildOrRefreshMarkers(pattern);
                if (tapButton) tapButton.interactable = false; // 프리뷰 동안 입력 금지
                yield return RunPass(pattern, scoring:false);

                // 2) 프리뷰→스코어링 무음 간격
                if (tapButton) tapButton.interactable = false;
                yield return WaitSmart(Mathf.Max(0f, gapAfterPreview));

                // 3) 스코어링 패스 준비: 큐/카운트인 모드 결정
                int passCI = Mathf.Max(0, scoringCountInBeats);
                _muteCountInAll = false;
                _cueOnLastCountInOnly = false;

                bool wantCue = useScoringCue && (showCueEvenWithCountIn || passCI == 0);
                if (wantCue) {
                    if (passCI == 0) {
                        // count-in이 없는 경우 → 내부적으로 1박 count-in 추가하고 그 비트에서 큐만 재생
                        passCI = 1;
                        _muteCountInAll = true;          // count-in 메트로놈 클릭 미재생
                        _cueOnLastCountInOnly = true;    // 단 하나의 count-in(=마지막)에서 큐 재생
                    } else {
                        // count-in이 있는 경우 → 마지막 count-in 비트에서 메트로놈 대신 큐 재생
                        _muteCountInAll = false;         // 앞선 count-in은 메트로놈/숫자 노출
                        _cueOnLastCountInOnly = true;
                    }
                }

                // 3) 타임라인 구성
                BuildTimeline(pattern, passCI);
                _hit  = new bool[pattern.beats];
                _miss = new bool[pattern.beats];
                _err  = new float[pattern.beats];
                for (int i = 0; i < pattern.beats; i++) { _hit[i] = false; _miss[i] = false; _err[i] = 0f; }
                ResetMarkersToIdle(pattern.beats);

                // 4) 스코어링 패스 실행
                yield return RunPass(pattern, scoring:true);

                // 5) 성과 집계
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

                // 6) 스코어링→다음 프리뷰 무음 간격
                if (_trialIndex < totalTrials) {
                    _acceptInput = false;
                    if (tapButton) tapButton.interactable = false;
                    yield return WaitSmart(Mathf.Max(0f, gapBeforePreview));
                }
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

            // 카운트인(필요 시)
            for (int i = 0; i < _countInThisPass; i++) {
                _beatTimes.Add(i * interval);
            }
            _patternStartT = (_beatTimes.Count > 0) ? _beatTimes[_beatTimes.Count - 1] + interval : 0f;

            // 본 비트: p.beats 만큼
            for (int i = 0; i < p.beats; i++) {
                _beatTimes.Add(_patternStartT + i * interval);
            }
            _patternEndT = _patternStartT + (p.beats - 1) * interval;

            if (judgeText) { judgeText.text = ""; judgeText.alpha = 0f; }
            if (cursor) cursor.gameObject.SetActive(beatTrack != null);
            if (countInText) { countInText.text = ""; countInText.alpha = 0f; }
            if (scoringCueText) { scoringCueText.text = ""; scoringCueText.alpha = 0f; }
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
            int nextAudio = 0;
            int nextVisual = 0;

            float interval = 60f / Mathf.Max(20, p.bpm);
            bool inputJustEnabled = false;

            if (!scoring && tapButton) tapButton.interactable = false;

            while (true) {
                if (!_paused) _elapsed += Dt();

                UpdateCursor();

                // 오디오 트리거
                while (nextAudio < _beatTimes.Count && _elapsed + 1e-5f >= _beatTimes[nextAudio] - audioAdvance) {
                    bool isCountIn = nextAudio < _countInThisPass;
                    _playCueThisBeat = false;

                    if (isCountIn) {
                        // count-in 처리(메트로놈 or 큐 대체)
                        bool isLastCountIn = nextAudio == _countInThisPass - 1;

                        if (_muteCountInAll) {
                            // 전체 count-in 무음 → 마지막 비트에서만 큐 재생
                            if (isLastCountIn) _playCueThisBeat = true;
                        } else if (_cueOnLastCountInOnly && isLastCountIn) {
                            // 정상 count-in이지만 마지막 비트는 큐로 대체
                            _playCueThisBeat = true;
                        }

                        if (_playCueThisBeat) {
                            PlayScoringCueOnce();
                            // count-in 텍스트는 노출하지 않음(큐로 대체)
                        } else {
                            // 일반 count-in: 메트로놈 + 숫자 표기
                            PlayClick(accent:false);
                            ShowCountIn(nextAudio);
                        }
                    } else {
                        // 본 비트: 메트로놈
                        bool accent = p.accentFirst && ((nextAudio - _countInThisPass) % p.beats == 0);
                        PlayClick(accent);
                    }
                    nextAudio++;
                }

                // 비주얼 트리거
                while (nextVisual < _beatTimes.Count && _elapsed + 1e-5f >= _beatTimes[nextVisual] - visualAdvance) {
                    bool isPatternBeat = (nextVisual >= _countInThisPass);
                    if (isPatternBeat) {
                        int idx = nextVisual - _countInThisPass;
                        HighlightMarker(idx, 0.08f, 1.12f);
                        Pulse(p.accentFirst && (idx % p.beats == 0), 0.08f, 1.08f);
                    } else {
                        Pulse(false, 0.06f, 1.05f);
                    }
                    nextVisual++;
                }

                // 스코어링 시작 시 입력 허용(= count-in 구간 종료 시)
                if (scoring && !inputJustEnabled && _elapsed >= (_countInThisPass * interval) - 1e-5f) {
                    _acceptInput = true;
                    inputJustEnabled = true;
                    if (tapButton) tapButton.interactable = true;
                    if (countInText) { countInText.text = ""; countInText.alpha = 0f; }
                }

                // 스코어링: 미스 처리
                if (scoring) UpdateMisses(p);

                // 종료
                if (nextVisual >= _beatTimes.Count && nextAudio >= _beatTimes.Count) {
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

        void PlayScoringCueOnce() {
            if (audioSource && scoringCueClip) {
                audioSource.PlayOneShot(scoringCueClip, Mathf.Clamp01(scoringCueVolume));
            }
            if (scoringCueText) {
                scoringCueText.color = scoringCueColor;
                scoringCueText.text = scoringCueMessage;
                scoringCueText.alpha = 1f;
                StopCoroutine(nameof(CoFadeTMP));
                StartCoroutine(CoFadeTMP(scoringCueText, scoringCueDuration));
            }
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

        // ---- 입력 & 판정 ----
        void OnTap() {
            if (!_acceptInput || _paused || _hit == null) return;

            float tNow = _elapsed;
            int beats = _hit.Length;

            int bestIdx = -1;
            float bestAbs = float.MaxValue;

            for (int i = 0; i < beats; i++) {
                if (_hit[i] || _miss[i]) continue;
                float bt = JudgeBeatTime(i);
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
            if (absErr <= 0.05f) return markerHit;
            if (absErr <= 0.10f) return new Color(0.5f, 0.9f, 1f);
            return new Color(1f, 0.85f, 0.5f);
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
            if (tapButton) tapButton.interactable = _acceptInput;
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