using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.GamePlay {
    /// <summary>
    /// G5: 다른 악기 찾기(3지 선다)
    /// - 프리뷰: A→B→C 순서로 각 선택지의 악기 샘플을 1회씩 재생(입력 불가)
    /// - 입력: 프리뷰 후 버튼 활성화. 다른 소리를 고르면 정답
    /// - 스테이지 규칙:
    ///   Stage1: 거친 계열 구분(현악 vs 관악/타악 등)
    ///   Stage2: 관악 내부 구분(목관 vs 금관)
    ///   Stage3: 유사 악기 그룹 내 변별(예: 바이올린/비올라)
    ///   Stage4: 전통악기 vs 서양악기
    /// - 인스펙터에서 악기 데이터(샘플, 분류)를 등록해 자동 조합
    /// </summary>
    public class G5OddInstrument : MonoBehaviour, IPausableGame {
        // ---- 분류 정의 ----
        public enum Family { Strings, Woodwind, Brass, Percussion, Keyboard, Other }
        public enum Culture { Western, KoreanTraditional }
        [System.Serializable] public class InstrumentEntry {
            public string id;
            public string displayName;
            public Family family;
            public Culture culture = Culture.Western;
            [Tooltip("이 악기의 소리 샘플들(랜덤 선택)")]
            public AudioClip[] samples;
        }
        [System.Serializable] public class SimilarGroup {
            public string groupName;
            public List<InstrumentEntry> members = new List<InstrumentEntry>();
        }
        public enum StageRule { BroadFamily, WindsSplit, SimilarWithinGroup, CultureSplit }

        // ---- UI ----
        [Header("UI (Assign in Inspector)")]
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button   buttonA;
        [SerializeField] Button   buttonB;
        [SerializeField] Button   buttonC;
        [SerializeField] TMP_Text labelA;
        [SerializeField] TMP_Text labelB;
        [SerializeField] TMP_Text labelC;
        [SerializeField] Button   replayAllButton;

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;

        [Header("Timing")]
        [SerializeField, Range(0.1f, 3f)] float interOptionDelay = 0.35f;  // 프리뷰에서 A→B→C 사이 딜레이
        [SerializeField, Range(0.1f, 2f)] float gapAfterPreview  = 0.5f;   // 프리뷰 종료→입력 허용 사이
        [SerializeField] bool useUnscaledTime = true;

        [Header("Rules")]
        [SerializeField, Min(1)] int totalTrials = 8;
        [SerializeField, Range(0.1f, 1f)] float clearAccuracy = 0.6f;

        [Header("Data (Assign in Inspector)")]
        [Tooltip("전체 악기 풀(계열/문화권/샘플 포함)")]
        [SerializeField] List<InstrumentEntry> instruments = new List<InstrumentEntry>();
        [Tooltip("유사 악기 그룹들(예: {바이올린,비올라,첼로}, {플루트,피콜로})")]
        [SerializeField] List<SimilarGroup> similarGroups = new List<SimilarGroup>();

        // ---- 상태 ----
        struct Option {
            public InstrumentEntry inst;
            public AudioClip clip;
        }
        Option[] _options = new Option[3];
        int      _oddIndex;

        int   _trialIndex;
        int   _correct;
        float _sumReaction;
        bool  _paused;
        bool  _acceptInput;

        float _enabledAt;

        public System.Action<int,int,float> OnGameFinished; // (total, correct, avgReaction)

        void Awake() {
            if (promptText && string.IsNullOrEmpty(promptText.text)) promptText.text = "다른 소리를 고르세요";
            if (labelA) labelA.text = "A";
            if (labelB) labelB.text = "B";
            if (labelC) labelC.text = "C";

            if (buttonA) buttonA.onClick.AddListener(()=>OnPick(0));
            if (buttonB) buttonB.onClick.AddListener(()=>OnPick(1));
            if (buttonC) buttonC.onClick.AddListener(()=>OnPick(2));
            if (replayAllButton) replayAllButton.onClick.AddListener(()=> { if (_acceptInput) StartCoroutine(CoPreviewSequence(playSoundOnly:true)); });
            SetButtonsInteractable(false);
        }

        // -------- 외부 API --------
        public void SetTotalTrials(int n){ totalTrials = Mathf.Max(1, n); }
        public void SetUseUnscaledTime(bool on){ useUnscaledTime = on; }
        public void ConfigureStageRule(StageRule rule){ _rule = rule; }

        // Host에서 스테이지 규칙을 전달하지 않으면 기본 매핑 사용
        StageRule _rule = StageRule.BroadFamily;

        public void StartGame() {
            StopAllCoroutines();
            _trialIndex = 0; _correct = 0; _sumReaction = 0f;
            StartCoroutine(GameLoop());
        }

        IEnumerator GameLoop() {
            while (_trialIndex < totalTrials) {
                if (progressText) progressText.text = $"{_trialIndex}/{totalTrials}";
                AuralRehab.Application.ServiceHub.I.Caption.ShowTop("각 선택지를 하이라이트하며 1회씩 들려줍니다.");

                // 문제 만들기
                if (!BuildTrial(_rule)) {
                    Debug.LogWarning("[G5] 유효한 조합을 만들 수 없습니다. 데이터 구성을 확인하세요.");
                    // 데이터 부족 시 안전 탈출
                    break;
                }

                // 프리뷰
                SetButtonsInteractable(false);
                yield return CoPreviewSequence(playSoundOnly:false);

                // 입력 허용
                yield return WaitSmart(gapAfterPreview);
                _acceptInput = true;
                _enabledAt = Time.unscaledTime;
                SetButtonsInteractable(true);
                AuralRehab.Application.ServiceHub.I.Caption.ShowTop("다른 소리를 고르세요.");

                // 선택 대기
                bool answered = false;
                while (!answered) yield return null;

                _trialIndex++;
                if (progressText) progressText.text = $"{_trialIndex}/{totalTrials}";

                // 간단한 텀
                yield return WaitSmart(0.4f);
            }

            float avgReaction = (_correct > 0 && _sumReaction > 0f) ? (_sumReaction / _correct) : 0f;
            OnGameFinished?.Invoke(_trialIndex, _correct, avgReaction);
        }

        // -------- 트라이얼 생성 --------
        bool BuildTrial(StageRule rule) {
            // 후보 필터 함수
            List<InstrumentEntry> PickByFamily(Family f) => instruments.FindAll(x => x != null && x.family == f && HasClip(x));
            List<InstrumentEntry> PickByCulture(Culture c) => instruments.FindAll(x => x != null && x.culture == c && HasClip(x));
            bool HasClip(InstrumentEntry e) => e.samples != null && e.samples.Length > 0 && e.samples[0] != null;

            // 결과 버퍼 초기화
            _options[0] = _options[1] = _options[2] = default;
            _oddIndex = -1;

            switch (rule) {
                case StageRule.BroadFamily: {
                    // 1) 랜덤 패밀리 하나 선택 -> 거기서 2개
                    // 2) 다른 패밀리 하나 선택 -> 거기서 1개
                    var families = new List<Family> { Family.Strings, Family.Woodwind, Family.Brass, Family.Percussion, Family.Keyboard, Family.Other };
                    Shuffle(families);
                    InstrumentEntry a1=null, a2=null, b=null;

                    foreach (var famA in families) {
                        var poolA = PickByFamily(famA);
                        if (poolA.Count < 1) continue;
                        foreach (var famB in families) {
                            if (famB == famA) continue;
                            var poolB = PickByFamily(famB);
                            if (poolB.Count < 1) continue;

                            a1 = poolA[Random.Range(0, poolA.Count)];
                            a2 = poolA[Mathf.Clamp(Random.Range(0, poolA.Count), 0, poolA.Count-1)];
                            // a1과 a2가 같아도 상관없음(샘플만 다르면 청감상 유사) — 원하면 다른 id 보장 로직 추가 가능
                            b  = poolB[Random.Range(0, poolB.Count)];
                            goto BUILT;
                        }
                    }
                    BUILT:
                    if (a1 == null || b == null) return false;
                    return BakeOptions(a1, a2, b);
                }

                case StageRule.WindsSplit: {
                    // 목관 vs 금관
                    var ww = PickByFamily(Family.Woodwind);
                    var br = PickByFamily(Family.Brass);
                    if (ww.Count < 1 || br.Count < 1) return false;

                    bool pairIsWoodwind = Random.value < 0.5f;
                    InstrumentEntry a1, a2, b;
                    if (pairIsWoodwind) {
                        a1 = ww[Random.Range(0, ww.Count)];
                        a2 = ww[Mathf.Clamp(Random.Range(0, ww.Count), 0, ww.Count-1)];
                        b  = br[Random.Range(0, br.Count)];
                    } else {
                        a1 = br[Random.Range(0, br.Count)];
                        a2 = br[Mathf.Clamp(Random.Range(0, br.Count), 0, br.Count-1)];
                        b  = ww[Random.Range(0, ww.Count)];
                    }
                    return BakeOptions(a1, a2, b);
                }

                case StageRule.SimilarWithinGroup: {
                    // 유사 그룹 하나 고르고 그 안에서 A,B 선택(서로 다른 악기)
                    // 그런 다음 "다른 패밀리/문화권"에서 하나를 odd로 넣으면 난이도가 낮아짐.
                    // 기획 예시처럼 비슷한 악기들 간 변별을 원하면: 같은 그룹 내에서 2개 동일, 1개 다른 멤버로 구성.
                    if (similarGroups == null || similarGroups.Count == 0) return false;
                    var g = similarGroups[Random.Range(0, similarGroups.Count)];
                    if (g == null || g.members == null || g.members.Count < 2) return false;

                    // 같은 그룹에서 두 개 동일, 하나는 같은 그룹의 다른 악기
                    var list = new List<InstrumentEntry>(g.members);
                    list.RemoveAll(e => e == null || !HasClip(e));
                    if (list.Count < 2) return false;

                    var a = list[Random.Range(0, list.Count)];
                    var b = a;
                    // a와 다른 멤버 찾기
                    for (int i=0;i<8;i++) {
                        var cand = list[Random.Range(0, list.Count)];
                        if (cand != a) { b = cand; break; }
                    }
                    // 결과: a, a, b
                    return BakeOptions(a, a, b);
                }

                case StageRule.CultureSplit: {
                    var west = PickByCulture(Culture.Western);
                    var kor  = PickByCulture(Culture.KoreanTraditional);
                    if (west.Count < 1 || kor.Count < 1) return false;

                    bool pairIsKorean = Random.value < 0.5f;
                    InstrumentEntry a1, a2, b;
                    if (pairIsKorean) {
                        a1 = kor[Random.Range(0, kor.Count)];
                        a2 = kor[Mathf.Clamp(Random.Range(0, kor.Count), 0, kor.Count-1)];
                        b  = west[Random.Range(0, west.Count)];
                    } else {
                        a1 = west[Random.Range(0, west.Count)];
                        a2 = west[Mathf.Clamp(Random.Range(0, west.Count), 0, west.Count-1)];
                        b  = kor[Random.Range(0, kor.Count)];
                    }
                    return BakeOptions(a1, a2, b);
                }
            }
            return false;
        }

        bool BakeOptions(InstrumentEntry same1, InstrumentEntry same2, InstrumentEntry odd) {
            var tmp = new List<Option>(3);
            tmp.Add(new Option{ inst=same1, clip=PickClip(same1) });
            tmp.Add(new Option{ inst=same2, clip=PickClip(same2) });
            tmp.Add(new Option{ inst=odd,   clip=PickClip(odd)   });

            // 랜덤 셔플 + oddIndex 계산
            int originalOdd = 2; // odd는 tmp[2]
            Shuffle(tmp);
            for (int i=0;i<3;i++) {
                _options[i] = tmp[i];
                if (tmp[i].inst == odd) _oddIndex = i;
            }
            return _oddIndex >= 0;
        }

        AudioClip PickClip(InstrumentEntry e) {
            if (e == null || e.samples == null || e.samples.Length == 0) return null;
            var list = e.samples;
            var idx = Random.Range(0, list.Length);
            return list[idx];
        }

        // -------- 프리뷰 --------
        IEnumerator CoPreviewSequence(bool playSoundOnly) {
            _acceptInput = false;
            SetButtonsInteractable(false);

            // A→B→C 순서로 하이라이트/재생
            for (int i=0;i<3;i++) {
                // 하이라이트 효과(라벨 강조만 간단히)
                TMP_Text t = (i==0)?labelA : (i==1)?labelB : labelC;
                if (!playSoundOnly && t) StartCoroutine(CoBlinkLabel(t, 0.18f));

                var opt = _options[i];
                if (audioSource && opt.clip) audioSource.PlayOneShot(opt.clip, 1f);

                // 샘플 길이만큼 대기(최대 1.5초), 이후 interOptionDelay
                float wait = Mathf.Min(opt.clip ? opt.clip.length : 0.7f, 1.5f);
                yield return WaitSmart(wait + interOptionDelay);
            }
        }

        IEnumerator CoBlinkLabel(TMP_Text t, float dur) {
            float tt=0f; Color c0 = t.color; Color c1=c0; c1.a = Mathf.Clamp01(c0.a * 1.0f);
            t.transform.localScale = Vector3.one;
            while (tt < dur) {
                if (!_paused) tt += Dt();
                float k = Smooth01(tt/dur);
                t.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.12f, k);
                yield return null;
            }
            t.transform.localScale = Vector3.one;
            t.color = c0;
        }

        // -------- 입력 --------
        void OnPick(int idx) {
            if (!_acceptInput) return;

            _acceptInput = false;
            SetButtonsInteractable(false);
            bool correct = (idx == _oddIndex);

            // 반응시간(정답에 한해 평균 산출)
            float rt = Mathf.Max(0f, Time.unscaledTime - _enabledAt);
            if (correct) _sumReaction += rt;
            if (correct) _correct++;

            // 피드백
            string a = _options[0].inst?.displayName ?? "A";
            string b = _options[1].inst?.displayName ?? "B";
            string c = _options[2].inst?.displayName ?? "C";
            string pickName = (idx==0)?a:(idx==1)?b:c;
            string oddName  = (_oddIndex==0)?a:(_oddIndex==1)?b:c;
            var msg = correct ? $"정답: {pickName}" : $"오답: {pickName} • 정답은 {oddName}";
            AuralRehab.Application.ServiceHub.I.Caption.ShowTop(msg);

            // 정답 소리 1회 들려주기(선택)
            var clip = _options[_oddIndex].clip;
            if (audioSource && clip) audioSource.PlayOneShot(clip, 0.9f);
        }

        void SetButtonsInteractable(bool on) {
            if (buttonA) buttonA.interactable = on;
            if (buttonB) buttonB.interactable = on;
            if (buttonC) buttonC.interactable = on;
            if (replayAllButton) replayAllButton.interactable = on; // 입력 중에도 리플레이 허용하려면 true로 유지 가능
        }

        // -------- 유틸/루프 --------
        float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        IEnumerator WaitSmart(float sec){ float t=0f; sec=Mathf.Max(0f,sec); while(t<sec){ if(!_paused) t+=Dt(); yield return null; } }
        float Smooth01(float x){ x=Mathf.Clamp01(x); return x*x*(3f-2f*x); }
        void Shuffle<T>(IList<T> list){ for (int i=list.Count-1;i>0;i--){ int j=Random.Range(0,i+1); (list[i],list[j])=(list[j],list[i]); } }

        // IPausableGame
        public void Pause()  { _paused = true;  if (audioSource) audioSource.Pause();  SetButtonsInteractable(false); }
        public void Resume() { _paused = false; if (audioSource) audioSource.UnPause(); SetButtonsInteractable(true && _acceptInput); }
        public bool IsPaused => _paused;
    }
}