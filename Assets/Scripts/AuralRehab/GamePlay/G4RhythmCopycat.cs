using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G4: 리듬 따라치기
    /// - Pattern = BPM + Segment[]
    /// - Segment: beats(박자 길이), isRest(쉼표 여부)
    /// - 프리뷰: 온셋마다 클릭/하이라이트(버튼 비활성화)
    /// - 스코어링: 동일 타임라인에 맞춰 탭하면 온셋당 판정
    /// </summary>
    public class G4RhythmCopycat : MonoBehaviour, IPausableGame {
        [System.Serializable]
        public struct Segment {
            public float beats;   // 1=4분음표, 0.5=8분, 0.75=점8분 ...
            public bool isRest;   // 쉼표 여부
            public Segment(float beats, bool isRest) { this.beats = Mathf.Max(0.05f, beats); this.isRest = isRest; }
        }
        [System.Serializable]
        public struct Pattern {
            public int bpm;
            public Segment[] segs;
            public Pattern(int bpm, params Segment[] segs) { this.bpm = Mathf.Max(20, bpm); this.segs = segs; }
        }

        [Header("UI (Assign)")]
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] TMP_Text bpmText;
        [SerializeField] TMP_Text countInText;
        [SerializeField] TMP_Text judgeText;
        [SerializeField] Button   tapButton;

        [Header("Track Visuals")]
        [SerializeField] RectTransform beatTrack;
        [SerializeField] Image markerPrefab;      // 온셋 마커
        [SerializeField] RectTransform cursor;    // 진행 커서
        [SerializeField] Image pulseImage;        // 선택(박마다 깜빡)

        [Header("Colors")]
        [SerializeField] Color markerIdle = new Color(1,1,1,0.35f);
        [SerializeField] Color markerBeat = Color.white;
        [SerializeField] Color markerHit  = new Color(0.2f,1f,0.4f);
        [SerializeField] Color markerMiss = new Color(1f,0.35f,0.35f);
        [SerializeField] Color colorPerfect = new Color(0.4f, 1f, 0.8f);
        [SerializeField] Color colorGood    = new Color(0.6f, 0.9f, 1f);
        [SerializeField] Color colorOk      = new Color(1f, 0.9f, 0.5f);
        [SerializeField] Color colorMiss    = new Color(1f, 0.5f, 0.5f);

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip clickNormal;
        [SerializeField] AudioClip clickAccent;   // 프리뷰/스코어링 첫 온셋 강조용
        [SerializeField] AudioClip scoringCueClip; // 스코어링 시작 큐(선택)
        [SerializeField, Range(0f,1f)] float scoringCueVolume = 1f;

        [Header("Timing")]
        [SerializeField, Range(0.05f, 0.30f)] float hitWindow = 0.15f;
        [SerializeField] int scoringCountInBeats = 0; // 0이면 큐만 사용 가능
        [SerializeField, Range(0f, 3f)] float gapAfterPreview = 2.0f;
        [SerializeField, Range(0f, 3f)] float gapBeforePreview = 2.0f;
        [SerializeField, Range(-0.2f, 0.2f)] float audioAdvance = 0f;
        [SerializeField, Range(-0.2f, 0.2f)] float visualAdvance = 0f;
        [SerializeField, Range(-0.2f, 0.2f)] float judgeTimeOffset = 0f;
        [SerializeField, Range(0.05f, 1.0f)] float judgeFadeDuration = 0.25f;
        [SerializeField] bool useUnscaledTime = true;

        [Header("Rules")]
        [SerializeField, Min(1)] int totalTrials = 8;
        [SerializeField, Range(0.1f, 1f)] float requiredOnsetHitRatio = 0.6f;

        // 외부 세팅
        List<Pattern> _patterns = new List<Pattern>();

        // 게임 상태
        int   _trialIndex;
        int   _correctTrials;
        float _sumAvgAbsErr;
        bool  _paused;

        // 패스 상태
        bool  _acceptInput;
        float _elapsed;
        float _interval;           // 1박 길이(초)
        int   _countInThisPass;

        // 타임라인(온셋만 저장: 쉼표는 제외)
        readonly List<float> _onsetTimes = new List<float>(); // 절대 시각(패스 기준)
        readonly List<int>   _onsetSegIdx = new List<int>();  // 어떤 세그먼트에서 나왔는지

        // 판정 버퍼
        bool[]  _hit;
        bool[]  _miss;
        float[] _err;

        // 시각화
        readonly List<Image> _markers = new List<Image>();
        RectTransform _trackRect;
        float _timelineStartT, _timelineEndT;
        Color _pulseOrigColor = Color.white;
        Vector3 _pulseOrigScale = Vector3.one;
        Coroutine _coJudge;

        public System.Action<int,int,float> OnGameFinished;      // (totalTrials, correctTrials, avgAbsErr)
        public System.Action<int,bool,float> OnTrialEnd;         // (trial#, success, avgErr)

        void Awake() {
            if (tapButton) tapButton.onClick.AddListener(OnTap);
            if (beatTrack) _trackRect = beatTrack;
            if (pulseImage) { _pulseOrigColor = pulseImage.color; _pulseOrigScale = pulseImage.transform.localScale; }
            if (judgeText) { judgeText.text=""; judgeText.alpha=0f; }
            if (countInText) { countInText.text=""; countInText.alpha=0f; }
        }

        // -------- 외부 API --------
        public void SetPatterns(IEnumerable<Pattern> patterns) {
            _patterns.Clear();
            if (patterns != null) _patterns.AddRange(patterns);
        }
        public void SetUseUnscaledTime(bool on) => useUnscaledTime = on;
        public void SetHitWindowSeconds(float s) => hitWindow = Mathf.Clamp(s, 0.05f, 0.30f);
        public void SetRequiredHitRatio(float r) => requiredOnsetHitRatio = Mathf.Clamp01(r);
        public void SetTotalTrials(int n) => totalTrials = Mathf.Max(1, n);

        public void StartGame() {
            StopAllCoroutines();
            _trialIndex = 0; _correctTrials = 0; _sumAvgAbsErr = 0f;
            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop() {
            while (_trialIndex < totalTrials) {
                var pat = GetPatternForTrial(_trialIndex);
                _interval = 60f / Mathf.Max(20, pat.bpm);

                if (bpmText) bpmText.text = $"{pat.bpm} BPM";
                if (progressText) progressText.text = $"{_trialIndex}/{totalTrials}";
                AuralRehab.Application.ServiceHub.I.Caption.ShowTop("리듬을 듣고 같은 리듬으로 탭하세요");

                // 1) 프리뷰(버튼 비활성)
                BuildTimeline(pat, countInForPreview: 0);
                BuildMarkers(pat);
                if (tapButton) tapButton.interactable = false;
                yield return RunPreview(pat);

                // 2) 프리뷰→스코어링 공백
                if (tapButton) tapButton.interactable = false;
                yield return WaitSmart(gapAfterPreview);

                // 3) 스코어링(카운트인/큐→입력 허용)
                int passCI = Mathf.Max(0, scoringCountInBeats);
                bool needCue = (passCI == 0) && (scoringCueClip != null);
                if (needCue) {
                    // 큐→정확히 1박 후 첫 온셋(= G3와 일관)
                    audioSource?.PlayOneShot(scoringCueClip, scoringCueVolume);
                    yield return WaitSmart(_interval);
                }

                BuildTimeline(pat, passCI);
                PrepareScoringBuffers();
                ResetMarkersToIdle();
                yield return RunScoring(pat);

                // 4) 집계
                int hits=0; float sumErr=0f;
                for (int i=0;i<_hit.Length;i++) if (_hit[i]) { hits++; sumErr+=_err[i]; }
                float ratio = (_hit.Length>0) ? hits/(float)_hit.Length : 0f;
                bool success = ratio >= requiredOnsetHitRatio;
                float avgErr = (hits>0) ? sumErr/hits : hitWindow;
                if (success) _correctTrials++;
                _sumAvgAbsErr += avgErr;
                _trialIndex++;
                OnTrialEnd?.Invoke(_trialIndex, success, avgErr);
                if (progressText) progressText.text = $"{_trialIndex}/{totalTrials}";

                // 5) 스코어링→다음 프리뷰 공백
                if (_trialIndex < totalTrials) {
                    _acceptInput = false;
                    if (tapButton) tapButton.interactable = false;
                    yield return WaitSmart(gapBeforePreview);
                }
            }

            float gameAvg = (_trialIndex>0)? _sumAvgAbsErr/_trialIndex : 0f;
            OnGameFinished?.Invoke(_trialIndex, _correctTrials, gameAvg);
        }

        // ===== 타임라인 =====
        void BuildTimeline(Pattern p, int countInForPreview) {
            _elapsed = 0f; _acceptInput = false;
            _countInThisPass = Mathf.Max(0, countInForPreview);
            _onsetTimes.Clear(); _onsetSegIdx.Clear();

            // 카운트인만큼 시간 선점
            float t = _countInThisPass * _interval;
            _timelineStartT = t;

            // 세그먼트 순회, isRest가 아닌 곳의 시작시각을 온셋으로 추가
            for (int i=0;i<p.segs.Length;i++) {
                var s = p.segs[i];
                if (!s.isRest) { _onsetTimes.Add(t); _onsetSegIdx.Add(i); }
                t += Mathf.Max(0.01f, s.beats) * _interval;
            }
            _timelineEndT = t;

            // UI 초기화
            if (judgeText) { judgeText.text=""; judgeText.alpha=0f; }
            if (countInText) { countInText.text=""; countInText.alpha=0f; }
            if (cursor) cursor.gameObject.SetActive(beatTrack != null);
        }

        void BuildMarkers(Pattern p) {
            if (!beatTrack || !markerPrefab) return;

            // 필요 개수 = 온셋 개수
            int need = CountOnsets(p);
            while (_markers.Count < need) _markers.Add(Instantiate(markerPrefab, beatTrack));
            while (_markers.Count > need) { var last=_markers[_markers.Count-1]; if (last) Destroy(last.gameObject); _markers.RemoveAt(_markers.Count-1); }

            float w = beatTrack.rect.width;
            for (int i=0;i<need;i++) {
                var m = _markers[i];
                var rt = m.rectTransform;
                float x01 = (_onsetTimes.Count>1) ? Mathf.InverseLerp(_timelineStartT, _timelineEndT, _onsetTimes[i]) : 0f;
                rt.anchoredPosition = new Vector2(Mathf.Lerp(0f, w, x01), 0f);
                rt.localScale = Vector3.one;
                m.color = markerIdle;
            }
        }
        int CountOnsets(Pattern p) {
            int n=0; foreach (var s in p.segs) if (!s.isRest) n++; return n;
        }
        void ResetMarkersToIdle() {
            for (int i=0;i<_markers.Count;i++) { var m=_markers[i]; if (m) { m.color = markerIdle; m.rectTransform.localScale = Vector3.one; } }
        }

        // ===== 프리뷰 =====
        IEnumerator RunPreview(Pattern p) {
            int iAudio = 0; int iVisual = 0;
            while (true) {
                if (!_paused) _elapsed += Dt();
                UpdateCursor();

                // 카운트인 오디오/텍스트
                while (iAudio < _onsetTimes.Count && _elapsed + 1e-5f >= _onsetTimes[iAudio] - audioAdvance) {
                    bool accent = (iAudio == 0);
                    PlayClick(accent);
                    iAudio++;
                }
                // 비주얼
                while (iVisual < _onsetTimes.Count && _elapsed + 1e-5f >= _onsetTimes[iVisual] - visualAdvance) {
                    HighlightMarker(iVisual, 0.08f, 1.12f);
                    Pulse(iVisual==0, 0.08f, 1.08f);
                    iVisual++;
                }

                if (_elapsed >= _timelineEndT + 0.05f) break;
                yield return null;
            }
        }

        // ===== 스코어링 =====
        void PrepareScoringBuffers() {
            int N = _onsetTimes.Count;
            _hit = new bool[N]; _miss = new bool[N]; _err = new float[N];
            for (int i=0;i<N;i++){ _hit[i]=false; _miss[i]=false; _err[i]=0f; }
        }

        IEnumerator RunScoring(Pattern p) {
            int iAudio = 0; int iVisual = 0;

            // 카운트인
            for (int c=0; c<_countInThisPass; c++) {
                ShowCountIn(c);
                PlayClick(false);
                yield return WaitSmart(_interval);
            }
            if (countInText) { countInText.text=""; countInText.alpha=0f; }

            // 입력 허용
            _acceptInput = true;
            if (tapButton) tapButton.interactable = true;

            // 본 패스
            while (true) {
                if (!_paused) _elapsed += Dt();
                UpdateCursor();

                // 오디오
                while (iAudio < _onsetTimes.Count && _elapsed + 1e-5f >= _onsetTimes[iAudio] - audioAdvance) {
                    PlayClick(iAudio==0);
                    iAudio++;
                }
                // 비주얼
                while (iVisual < _onsetTimes.Count && _elapsed + 1e-5f >= _onsetTimes[iVisual] - visualAdvance) {
                    HighlightMarker(iVisual, 0.08f, 1.12f);
                    Pulse(iVisual==0, 0.08f, 1.08f);
                    iVisual++;
                }

                // 미스 처리
                for (int i=0;i<_onsetTimes.Count;i++) {
                    if (_hit[i] || _miss[i]) continue;
                    if (_elapsed > _onsetTimes[i] + judgeTimeOffset + hitWindow + 1e-5f) {
                        _miss[i] = true;
                        SetMarkerColor(i, markerMiss);
                        ShowJudge("Miss", colorMiss);
                    }
                }

                // 종료
                if (_elapsed >= _timelineEndT + 0.05f) { _acceptInput=false; break; }
                yield return null;
            }
        }

        // ===== 입력/판정 =====
        void OnTap() {
            if (!_acceptInput || _paused || _hit == null) return;
            float tNow = _elapsed;

            int best = -1; float bestAbs = float.MaxValue;
            for (int i=0;i<_onsetTimes.Count;i++) {
                if (_hit[i] || _miss[i]) continue;
                float tgt = _onsetTimes[i] + judgeTimeOffset;
                float d = Mathf.Abs(tNow - tgt);
                if (d < bestAbs) { bestAbs = d; best = i; }
            }

            if (best >= 0 && bestAbs <= hitWindow) {
                _hit[best] = true;
                _err[best] = bestAbs;
                SetMarkerColor(best, ColorForError(bestAbs));
                ShowJudgeText(bestAbs);
                FlashTap(0.08f);
            } else {
                ShowJudge("Miss", colorMiss);
                FlashTap(0.08f);
            }
        }

        // ===== 시각/오디오 유틸 =====
        void UpdateCursor() {
            if (!beatTrack || !cursor) return;
            float w = beatTrack.rect.width;
            if (_elapsed <= _timelineStartT) { cursor.anchoredPosition = new Vector2(0f, cursor.anchoredPosition.y); return; }
            if (_elapsed >= _timelineEndT)   { cursor.anchoredPosition = new Vector2(w, cursor.anchoredPosition.y); return; }
            float t01 = Mathf.InverseLerp(_timelineStartT, _timelineEndT, _elapsed);
            cursor.anchoredPosition = new Vector2(Mathf.Lerp(0f, w, t01), cursor.anchoredPosition.y);
        }

        void PlayClick(bool accent) {
            if (!audioSource) return;
            var clip = accent && clickAccent ? clickAccent : clickNormal;
            if (!clip) return;
            audioSource.PlayOneShot(clip, 1f);
        }

        void ShowCountIn(int idx) {
            if (!countInText) return;
            int remain = (_countInThisPass-1) - idx;
            countInText.text = (remain>=0)? (remain==0? "Go" : remain.ToString()) : "";
            countInText.alpha = 1f;
            StopCoroutine(nameof(CoFadeTMP)); StartCoroutine(CoFadeTMP(countInText, 0.2f));
        }

        void HighlightMarker(int i, float dur, float scale) {
            if (i<0 || i>=_markers.Count) return;
            var m = _markers[i]; if (!m) return;
            StopCoroutine(nameof(CoPulseMarker));
            StartCoroutine(CoPulseMarker(m.rectTransform, m, dur, scale));
        }

        IEnumerator CoPulseMarker(RectTransform rt, Image m, float dur, float scale) {
            var s0 = rt.localScale; var s1 = s0 * scale;
            var c0 = markerIdle; var c1 = markerBeat;
            float t=0f;
            while (t<dur) {
                if (!_paused) t += Dt();
                float k = Smooth01(t/dur);
                rt.localScale = Vector3.Lerp(s0, s1, k);
                m.color = Color.Lerp(c0, c1, k);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        IEnumerator CoFadeTMP(TMP_Text t, float dur) {
            float a0=t.alpha, tt=0f;
            while (tt<dur) { if (!_paused) tt+=Dt(); t.alpha=Mathf.Lerp(a0,0f,Smooth01(tt/dur)); yield return null; }
            t.alpha=0f;
        }

        void SetMarkerColor(int i, Color c){ if (i<0 || i>=_markers.Count) return; var m=_markers[i]; if (m) m.color=c; }

        void Pulse(bool accent, float dur, float scale) {
            if (!pulseImage) return;
            StopCoroutine(nameof(CoPulse));
            StartCoroutine(CoPulse(accent, dur, scale));
        }
        IEnumerator CoPulse(bool accent, float dur, float scale) {
            var img=pulseImage; if (!img) yield break;
            var c0=_pulseOrigColor; var c1=c0; c1.a=Mathf.Clamp01(c0.a*(accent?1.8f:1.35f));
            var s0=_pulseOrigScale; var s1=s0*(accent?scale*1.05f:scale);
            float t=0f;
            while (t<dur){ if(!_paused) t+=Dt(); float k=Smooth01(t/dur); img.color=Color.Lerp(c0,c1,k); img.transform.localScale=Vector3.Lerp(s0,s1,k); yield return null; }
            img.color=_pulseOrigColor; img.transform.localScale=_pulseOrigScale;
        }

        void FlashTap(float dur) {
            if (!tapButton) return; var g=tapButton.targetGraphic; if (!g) return;
            StopCoroutine(nameof(CoFlash)); StartCoroutine(CoFlash(g, dur));
        }
        IEnumerator CoFlash(Graphic g, float dur) {
            var c0=g.color; var c1=c0; c1.a=Mathf.Clamp01(c0.a*0.6f);
            float t=0f; while (t<dur){ if(!_paused) t+=Dt(); float k=Smooth01(t/dur); g.color=Color.Lerp(c0,c1,k); yield return null; } g.color=c0;
        }

        void ShowJudgeText(float absErr) {
            if (absErr<=0.05f) ShowJudge("Perfect", colorPerfect);
            else if (absErr<=0.10f) ShowJudge("Good", colorGood);
            else ShowJudge("Ok", colorOk);
        }
        void ShowJudge(string txt, Color c) {
            if (!judgeText) return;
            if (_coJudge!=null) StopCoroutine(_coJudge);
            _coJudge = StartCoroutine(CoJudge(txt,c));
        }
        IEnumerator CoJudge(string txt, Color c) {
            judgeText.text = txt; judgeText.color = c; judgeText.alpha = 1f;
            float t=0f; while (t<judgeFadeDuration){ if(!_paused) t+=Dt(); judgeText.alpha = Mathf.Lerp(1f,0f,Smooth01(t/judgeFadeDuration)); yield return null; }
            judgeText.alpha = 0f;
        }

        // -------- 공통 --------
        Pattern GetPatternForTrial(int idx) {
            if (_patterns==null || _patterns.Count==0) return new Pattern(80, new Segment(1,false), new Segment(0.5f,false), new Segment(0.5f,false), new Segment(1,false));
            return _patterns[idx % _patterns.Count];
        }
        float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float Smooth01(float x){ x=Mathf.Clamp01(x); return x*x*(3f-2f*x); }
        IEnumerator WaitSmart(float sec){ float t=0f; sec=Mathf.Max(0f,sec); while(t<sec){ if(!_paused) t+=Dt(); yield return null; } }

        Color ColorForError(float e){ if (e<=0.05f) return markerHit; if (e<=0.10f) return new Color(0.5f,0.9f,1f); return new Color(1f,0.85f,0.5f); }

        // IPausableGame
        public void Pause(){ _paused = true; if (tapButton) tapButton.interactable=false; if (audioSource) audioSource.Pause(); }
        public void Resume(){ _paused = false; if (tapButton) tapButton.interactable=_acceptInput; if (audioSource) audioSource.UnPause(); }
        public bool IsPaused => _paused;
    }
}