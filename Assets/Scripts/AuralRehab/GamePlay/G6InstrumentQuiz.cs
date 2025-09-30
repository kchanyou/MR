using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G6: 악기 소리 맞히기 (다지선다)
    /// - 프리뷰: 문제 대상 악기 샘플 1회 재생(입력 불가, 버튼은 시각적 펄스만)
    /// - 입력: 보기 중 정답 악기명을 선택
    /// - 반응시간/정확도 측정, 일시정지 대응
    /// </summary>
    public class G6InstrumentQuiz : MonoBehaviour, IPausableGame {
        // ==== 분류(가능하면 G5와 동일 개념) ====
        public enum Family { Strings, Woodwind, Brass, Percussion, Keyboard, Other }
        public enum Culture { Western, KoreanTraditional }

        [System.Serializable] public class InstrumentEntry {
            public string id;                 // 유니크 키 권장
            public string displayName;        // 보기 표기
            public Family family;
            public Culture culture = Culture.Western;
            [Tooltip("문제/리플레이용 샘플들(무작위 선택)")]
            public AudioClip[] samples;
        }

        // ==== UI ====
        [Header("UI (Assign in Inspector)")]
        [SerializeField] TMP_Text       promptText;
        [SerializeField] TMP_Text       progressText;
        [SerializeField] Button         replayButton;       // 문제 샘플 다시듣기(입력 허용 중에만)
        [SerializeField] RectTransform  optionsRoot;        // 보기 버튼 부모(그리드 권장)
        [SerializeField] Button         optionButtonPrefab; // 보기 버튼 프리팹(자식에 TMP_Text 포함)

        // ==== Audio ====
        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip  cueStart;               // 스코어링 시작 신호음(선택)

        // ==== Timing ====
        [Header("Timing")]
        [SerializeField, Range(0.0f, 2.0f)] float previewLead      = 0.0f; // 프리뷰 시작 전 약간의 여유
        [SerializeField, Range(0.1f, 3.0f)] float gapAfterPreview  = 0.6f; // 프리뷰 종료→입력 허용까지 무음
        [SerializeField, Range(0.1f, 3.0f)] float gapAfterAnswer   = 0.5f; // 정답 피드백 후 다음 문제까지
        [SerializeField] bool useUnscaledTime = true;

        // ==== 룰 ====
        public enum CultureMix { WesternOnly, Mixed, IncludeKoreanPriority, All }
        [Header("Rules")]
        [SerializeField, Min(1)]       int   totalTrials     = 8;
        [SerializeField, Range(2, 12)] int   choicesPerTrial = 4;
        [SerializeField]               CultureMix cultureMix = CultureMix.Mixed;
        [SerializeField, Range(0.1f,1f)] float clearAccuracy = 0.6f;

        // ==== 데이터 ====
        [Header("Data (Choose one)")]
        [Tooltip("여기에 직접 악기를 등록해서 사용")]
        [SerializeField] List<InstrumentEntry> instruments = new List<InstrumentEntry>();

        [Tooltip("선택: G5 컴포넌트를 참조하면 그 인스펙터의 악기 데이터를 그대로 가져와 사용")]
        [SerializeField] AuralRehab.GamePlay.G5OddInstrument g5Source; // 있으면 가져옴(현재는 수동 권장)

        // ==== 상태 ====
        struct Trial {
            public InstrumentEntry answer;
            public List<InstrumentEntry> options;
        }
        List<Button> _spawnedButtons = new List<Button>();
        List<OptionButtonView> _spawnedViews = new List<OptionButtonView>(); // 생성 직후 캐시
        int   _trialIndex;
        int   _correct;
        float _sumReaction;
        bool  _paused;
        bool  _acceptInput;
        float _enableTime; // 입력 허용 시각

        public System.Action<int,int,float> OnGameFinished; // (total, correct, avgReaction)

        // ===== Unity =====
        void Awake() {
            if (promptText && string.IsNullOrEmpty(promptText.text))
                promptText.text = "들리는 악기가 무엇인지 고르세요";
            if (replayButton) replayButton.onClick.AddListener(()=> { if (_acceptInput) PlayAnswerPreview(); });
        }

        // ===== 외부 API =====
        public void SetUseUnscaledTime(bool on) => useUnscaledTime = on;
        public void SetTotalTrials(int n)        => totalTrials = Mathf.Max(1, n);
        public void SetChoicesPerTrial(int n)    => choicesPerTrial = Mathf.Clamp(n, 2, 12);
        public void SetCultureMix(CultureMix m)  => cultureMix = m;
        public void ConfigureStage(int choices, CultureMix mix) {
            SetChoicesPerTrial(choices);
            SetCultureMix(mix);
        }

        public void StartGame() {
            StopAllCoroutines();
            EnsureDataFromG5IfAny();

            _trialIndex = 0; _correct = 0; _sumReaction = 0f;
            StartCoroutine(GameLoop());
        }

        // ===== 메인 루프 =====
        IEnumerator GameLoop() {
            while (_trialIndex < totalTrials) {
                UpdateProgress();
                ServiceHubCaption($"문제 {_trialIndex+1} / {totalTrials}");

                // 트라이얼 생성
                var trial = BuildTrial();
                if (trial.answer == null || trial.options == null || trial.options.Count < 2) {
                    Debug.LogWarning("[G6] 유효한 문제를 만들 수 없습니다. 데이터 구성을 확인하세요.");
                    break;
                }

                // 보기 버튼 구성/바인딩 (★ 생성 직후 처리 포함)
                BuildOptionButtons(trial.options);

                // 프리뷰
                SetOptionsInteractable(false);
                yield return WaitSmart(previewLead);

                // 프리뷰 시작 시 버튼들에 가벼운 펄스(시각 피드백)
                foreach (var v in _spawnedViews) v?.PreviewPulse();

                // 문제 샘플 1회 재생
                yield return PlayClip(trial.answer);

                // 프리뷰→스코어링 사이 무음
                yield return WaitSmart(gapAfterPreview);

                // 스코어링 시작 신호음(선택)
                if (cueStart) yield return PlayClip(cueStart);

                // 입력 허용
                _acceptInput = true;
                _enableTime = Now();
                SetOptionsInteractable(true);
                ServiceHubCaption("정답을 골라주세요.");

                // 입력 대기
                bool answered = false;
                int pickedIndex = -1;
                System.Action<int> onPick = (idx)=> {
                    if (!_acceptInput) return;
                    _acceptInput = false;
                    pickedIndex = idx;
                    answered = true;
                };
                BindPickHandlers(_spawnedButtons, onPick);

                while (!answered) yield return null;

                // 판정
                var picked = trial.options[pickedIndex];
                bool isCorrect = ReferenceEquals(picked, trial.answer);
                float rt = Mathf.Max(0f, Now() - _enableTime);
                if (isCorrect) { _correct++; _sumReaction += rt; }

                // 시각 피드백(OptionButtonView가 붙어있다면 활용)
                var pickedView = (pickedIndex >= 0 && pickedIndex < _spawnedViews.Count) ? _spawnedViews[pickedIndex] : null;
                pickedView?.ShowJudge(isCorrect);

                if (!isCorrect) {
                    // 정답 버튼도 초록색으로 표시
                    int answerIdx = trial.options.FindIndex(e => ReferenceEquals(e, trial.answer));
                    if (answerIdx >= 0 && answerIdx < _spawnedViews.Count) _spawnedViews[answerIdx]?.ShowJudge(true);
                }

                ServiceHubCaption(isCorrect
                    ? $"정답: {picked.displayName}"
                    : $"오답: {picked.displayName} • 정답은 {trial.answer.displayName}");

                // 정답 샘플 1회 재생
                yield return PlayClip(trial.answer);

                // 다음 문제까지 텀
                yield return WaitSmart(gapAfterAnswer);

                _trialIndex++;
            }

            float avgRt = (_correct > 0 && _sumReaction > 0f) ? (_sumReaction / _correct) : 0f;
            OnGameFinished?.Invoke(_trialIndex, _correct, avgRt);
        }

        // ===== 트라이얼 생성 =====
        Trial BuildTrial() {
            var pool = FilterByCultureMix(GetWorkingList());
            if (pool.Count < choicesPerTrial) {
                // 부족하면 전체 풀로 보완
                var all = GetWorkingListRaw();
                foreach (var e in all) if (!pool.Contains(e)) pool.Add(e);
            }
            // 정답
            var answer = PickRandom(pool);
            // 오답 N-1
            var options = new List<InstrumentEntry>();
            options.Add(answer);
            var tmp = new List<InstrumentEntry>(pool);
            tmp.Remove(answer);
            Shuffle(tmp);
            for (int i=0;i<choicesPerTrial-1 && i<tmp.Count;i++) options.Add(tmp[i]);
            Shuffle(options);

            return new Trial { answer = answer, options = options };
        }

        // 문화권 필터(난이도 성격용)
        List<InstrumentEntry> FilterByCultureMix(List<InstrumentEntry> src) {
            var outList = new List<InstrumentEntry>();
            switch (cultureMix) {
                case CultureMix.WesternOnly:
                    outList.AddRange(src.FindAll(e => e.culture == Culture.Western));
                    break;
                case CultureMix.Mixed:
                    outList.AddRange(src);
                    break;
                case CultureMix.IncludeKoreanPriority:
                    var kor = src.FindAll(e => e.culture == Culture.KoreanTraditional);
                    var wes = src.FindAll(e => e.culture == Culture.Western);
                    outList.AddRange(kor);
                    int need = Mathf.Max(choicesPerTrial, kor.Count + Mathf.CeilToInt(kor.Count * 0.5f));
                    Shuffle(wes);
                    for (int i=0;i<wes.Count && outList.Count<need;i++) outList.Add(wes[i]);
                    foreach (var w in wes) if (!outList.Contains(w)) outList.Add(w);
                    break;
                case CultureMix.All:
                    outList.AddRange(src);
                    break;
            }
            outList.RemoveAll(e => e == null || e.samples == null || e.samples.Length == 0 || e.samples[0]==null);
            return outList;
        }

        // ===== 보기 UI =====
        void BuildOptionButtons(List<InstrumentEntry> options) {
            // 기존 정리
            foreach (var b in _spawnedButtons) if (b) Destroy(b.gameObject);
            _spawnedButtons.Clear();
            _spawnedViews.Clear();

            // === 생성 직후 처리 지점 ===
            for (int i=0;i<options.Count;i++) {
                var btn = Instantiate(optionButtonPrefab, optionsRoot); // 생성
                _spawnedButtons.Add(btn);

                // OptionButtonView로 상태/라벨 세팅
                var view = btn.GetComponent<OptionButtonView>();
                if (view != null) {
                    view.SetLabel(options[i].displayName);
                    view.SetStateNormal(true);
                    view.SetInteractable(false); // 기본 비활성(프리뷰 동안)
                    _spawnedViews.Add(view);
                } else {
                    // 뷰가 없으면 TMP_Text 직접 세팅
                    var label = btn.GetComponentInChildren<TMP_Text>(true);
                    if (label) label.text = options[i].displayName;
                }
            }
        }
        void BindPickHandlers(List<Button> btns, System.Action<int> onPick) {
            for (int i=0;i<btns.Count;i++) {
                int idx = i;
                btns[i].onClick.RemoveAllListeners();
                btns[i].onClick.AddListener(()=> onPick?.Invoke(idx));
            }
        }
        void SetOptionsInteractable(bool on) {
            foreach (var b in _spawnedButtons) if (b) b.interactable = on;
            foreach (var v in _spawnedViews) v?.SetInteractable(on);
            if (replayButton) replayButton.interactable = on; // 문제 다시듣기는 입력 허용 중에만
        }

        // ===== 오디오 =====
        IEnumerator PlayClip(InstrumentEntry e) {
            var clip = PickClip(e);
            if (clip) {
                audioSource?.PlayOneShot(clip, 1f);
                float wait = Mathf.Max(0.05f, Mathf.Min(clip.length, 2.0f));
                yield return WaitSmart(wait);
            }
        }
        IEnumerator PlayClip(AudioClip clip) {
            if (clip) {
                audioSource?.PlayOneShot(clip, 1f);
                float wait = Mathf.Max(0.05f, Mathf.Min(clip.length, 2.0f));
                yield return WaitSmart(wait);
            }
        }
        void PlayAnswerPreview() {
            // 현재 트라이얼의 정답을 다시 들려주고 싶다면 정답 캐시를 보관하도록 확장 가능(지금은 생략).
        }

        // ===== 데이터 소스 보조 =====
        void EnsureDataFromG5IfAny() {
            if (g5Source == null) return;
            // G5의 내부 리스트를 직접 읽지 않는 대신, 동일한 InstrumentEntry 구성을 이 컴포넌트에도 등록해 사용 권장.
        }

        List<InstrumentEntry> GetWorkingList() {
            var list = new List<InstrumentEntry>(GetWorkingListRaw());
            list.RemoveAll(e => e == null || e.samples == null || e.samples.Length == 0 || e.samples[0]==null);
            return list;
        }
        List<InstrumentEntry> GetWorkingListRaw() {
            // 기본은 로컬 instruments 사용
            return instruments;
        }
        AudioClip PickClip(InstrumentEntry e) {
            if (e == null || e.samples == null || e.samples.Length == 0) return null;
            int idx = Random.Range(0, e.samples.Length);
            return e.samples[idx];
        }

        // ===== 공통 유틸 =====
        void UpdateProgress() {
            if (progressText) progressText.text = $"{_trialIndex}/{totalTrials}";
        }
        void ServiceHubCaption(string msg) {
            AuralRehab.Application.ServiceHub.I?.Caption?.ShowTop(msg);
        }
        float Dt()  => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float Now() => useUnscaledTime ? Time.unscaledTime     : Time.time;
        IEnumerator WaitSmart(float sec) { float t=0f; sec=Mathf.Max(0f,sec); while (t<sec){ if(!_paused) t+=Dt(); yield return null; } }
        void Shuffle<T>(IList<T> list) { for (int i=list.Count-1;i>0;i--){ int j=Random.Range(0,i+1); (list[i],list[j])=(list[j],list[i]); } }
        T PickRandom<T>(IList<T> list) {
            if (list == null || list.Count == 0) return default;
            int idx = Random.Range(0, list.Count);
            return list[idx];
        }

        // ===== IPausableGame =====
        public void Pause()  { _paused = true;  if (audioSource) audioSource.Pause();  SetOptionsInteractable(false); }
        public void Resume() { _paused = false; if (audioSource) audioSource.UnPause(); SetOptionsInteractable(true && _acceptInput); }
        public bool IsPaused => _paused;
    }
}