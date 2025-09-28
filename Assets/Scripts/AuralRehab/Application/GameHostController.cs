using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using AuralRehab.Core.Data;
using AuralRehab.GamePlay;

namespace AuralRehab.Application {
    /// <summary>
    /// 게임 호스트(같은 씬 결과 오버레이 + 자동 시작 + 설정 모달)
    /// - G1/G2/G3 전용 구현
    /// - G4~G6 임시 스텁(순차 교체 예정)
    /// </summary>
    public class GameHostController : MonoBehaviour {
        [Header("UI Refs (Assign in Inspector)")]
        [SerializeField] TMP_FontAsset uiFont;
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text hintText;
        [SerializeField] Button btnBack;
        [SerializeField] Button btnRetry;
        [SerializeField] Button btnStart; // 자동 시작이면 비활성
        [SerializeField] Button btnMenu;  // 햄버거

        [Header("Play Area")]
        [SerializeField] RectTransform playArea;

        [Header("Prefabs / Components")]
        [SerializeField] G1OddOneOutPitch  g1Prefab;        // G1
        [SerializeField] G2MelodyDirection g2Prefab;        // G2
        [SerializeField] G3BeatJump        g3Prefab;        // G3
        [SerializeField] TwoChoiceMinigame twoChoicePrefab; // 임시(기타 모드)
        [SerializeField] ResultOverlay resultOverlay;       // 같은 씬 결과
        [SerializeField] SettingsModal settingsModal;       // 설정 모달

        [Header("Rules (Common)")]
        [SerializeField, Range(0f,1f)] float clearAccuracy = 0.6f;
        [SerializeField] int   trialsPerGame = 8;
        [SerializeField] float autoStartDelay = 3f;

        CampaignId _campaign;
        int _stage;
        GameMode _modePrimary;
        GameMode _modeAlt;

        // 런타임 인스턴스
        G1OddOneOutPitch  _g1;
        G2MelodyDirection _g2;
        G3BeatJump        _g3;
        TwoChoiceMinigame _two;
        IPausableGame     _pausable;
        bool _gameStarted;

        void Awake() {
            EnsureMainCamera();
            EnsureServiceHub();

            _campaign = GameRouter.SelectedCampaign;
            _stage    = Mathf.Clamp(GameRouter.SelectedStage, 1, 8);
            (_modePrimary, _modeAlt) = ResolveModes(_campaign, _stage);

            ApplyFontIfSet(headerText);
            ApplyFontIfSet(hintText);

            if (btnBack)  btnBack.onClick.AddListener(OnExitToStageSelect);
            if (btnRetry) btnRetry.onClick.AddListener(OnRetry);
            if (btnStart) btnStart.onClick.AddListener(OnStartGame);
            if (btnMenu)  btnMenu.onClick.AddListener(OnOpenMenu);

            if (resultOverlay) resultOverlay.HideImmediate();
            if (settingsModal) settingsModal.Setup(OnCloseMenu, OnRetry, OnExitToStageSelect);

            headerText.text = BuildHeaderLabel(_campaign, _stage, _modePrimary, _modeAlt);
            ServiceHub.I.Caption.ShowTop($"게임이 {autoStartDelay:0}초 후 자동 시작됩니다.");
            if (btnStart) btnStart.gameObject.SetActive(false);
        }

        void Start() { StartCoroutine(AutoStartAfterDelay()); }

        IEnumerator AutoStartAfterDelay() {
            float t = 0f;
            while (t < autoStartDelay) { t += Time.unscaledDeltaTime; yield return null; }
            if (!_gameStarted) OnStartGame();
        }

        void EnsureMainCamera() {
            if (Camera.main == null) {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
        }

        void EnsureServiceHub() {
            if (ServiceHub.I == null) {
                var go = new GameObject("ServiceHub");
                go.AddComponent<ServiceHub>();
            }
            if (uiFont != null) {
                ServiceHub.I.Caption.SetFont(uiFont);
                var tmp = ServiceHub.I.Caption.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp && uiFont.material) tmp.fontSharedMaterial = uiFont.material;
            }
        }

        void ApplyFontIfSet(TMP_Text t) {
            if (!t || !uiFont) return;
            t.font = uiFont;
            if (uiFont.material) t.fontSharedMaterial = uiFont.material;
        }

        // ----------- Start / Pause -----------
        void OnStartGame() {
            if (_gameStarted) return;
            _gameStarted = true;

            switch (_modePrimary) {
                case GameMode.G1: RunG1(); break;
                case GameMode.G2: RunG2(); break;
                case GameMode.G3: RunG3(); break;
                default:          RunTwoChoiceStub(_modePrimary); break;
            }
        }

        void OnOpenMenu() { _pausable?.Pause(); settingsModal?.Open(); }
        void OnCloseMenu() { settingsModal?.Close(); _pausable?.Resume(); }

        // ---------------- G1 ----------------
        void RunG1() {
            if (_g1 == null) _g1 = CreateG1(playArea);

            int interval = GetIntervalForStage(_campaign, _stage);
            _g1.SetInterval(interval);
            _g1.SetUseUnscaledTime(true);
            _pausable = _g1;

            _g1.OnGameFinished = OnCommonFinished;

            ServiceHub.I.Caption.ShowTop("각 선택지의 소리를 듣고, 다른 소리를 고르세요.");
            _g1.gameObject.SetActive(true);
            _g1.StartGame();
        }
        G1OddOneOutPitch CreateG1(RectTransform mount) {
            G1OddOneOutPitch inst = null;
            if (g1Prefab != null) inst = Instantiate(g1Prefab, mount);
            else inst = mount.GetComponentInChildren<G1OddOneOutPitch>(true);
            if (inst == null) inst = gameObject.AddComponent<G1OddOneOutPitch>();
            return inst;
        }
        int GetIntervalForStage(CampaignId id, int stage) {
            // A: 1=완전5도, 2=장3도, 3=장2도, 4=반음
            switch (stage) { case 1: return 7; case 2: return 4; case 3: return 2; default: return 1; }
        }

        // ---------------- G2 ----------------
        void RunG2() {
            if (_g2 == null) _g2 = CreateG2(playArea);

            var t = GetG2TaskForStage(_campaign, _stage);
            _g2.SetTask(t);
            _g2.SetStep(2);
            _g2.SetTotalTrials(trialsPerGame);
            _g2.SetUseUnscaledTime(true);
            var labels = GetG2Labels(t);
            _g2.ConfigureLabels(labels.left, labels.right, labels.prompt);

            _pausable = _g2;
            _g2.OnGameFinished = OnCommonFinished;

            ServiceHub.I.Caption.ShowTop("멜로디를 듣고 올바른 방향 패턴을 고르세요.");
            _g2.gameObject.SetActive(true);
            _g2.StartGame();
        }
        G2MelodyDirection CreateG2(RectTransform mount) {
            G2MelodyDirection inst = null;
            if (g2Prefab != null) inst = Instantiate(g2Prefab, mount);
            else inst = mount.GetComponentInChildren<G2MelodyDirection>(true);
            if (inst == null) inst = gameObject.AddComponent<G2MelodyDirection>();
            return inst;
        }
        G2MelodyDirection.TaskType GetG2TaskForStage(CampaignId id, int stage) {
            if (id != CampaignId.A) return G2MelodyDirection.TaskType.UpDownSimple;
            if (stage <= 5) return G2MelodyDirection.TaskType.UpDownSimple;
            if (stage == 6) return G2MelodyDirection.TaskType.HillValley;
            return G2MelodyDirection.TaskType.TripleChange;
        }
        (string left, string right, string prompt) GetG2Labels(G2MelodyDirection.TaskType t) {
            switch (t) {
                case G2MelodyDirection.TaskType.UpDownSimple:  return ("상승", "하강", "멜로디의 전체 방향을 고르세요");
                case G2MelodyDirection.TaskType.HillValley:    return ("상승→하강(산)", "하강→상승(골짜기)", "패턴을 고르세요");
                default:                                       return ("상승→하강→상승", "하강→상승→하강", "복합 패턴을 고르세요");
            }
        }

        // ---------------- G3 ----------------
        void RunG3() {
            if (_g3 == null) _g3 = CreateG3(playArea);

            // 스테이지별 패턴/튜닝 주입
            var patterns = GetG3PatternsForStage(_campaign, _stage);
            float hitWindow, reqRatio;
            GetG3TuningForStage(_campaign, _stage, out hitWindow, out reqRatio);

            _g3.SetPatterns(patterns);
            _g3.SetHitWindowSeconds(hitWindow);
            _g3.SetRequiredHitRatio(reqRatio);
            _g3.SetTotalTrials(Mathf.Min(trialsPerGame, patterns.Count));
            _g3.SetUseUnscaledTime(true);

            _pausable = _g3;
            _g3.OnGameFinished = OnCommonFinished;

            ServiceHub.I.Caption.ShowTop("카운트인 후 비트에 맞춰 버튼을 탭하세요.");
            _g3.gameObject.SetActive(true);
            _g3.StartGame();
        }

        G3BeatJump CreateG3(RectTransform mount) {
            G3BeatJump inst = null;
            if (g3Prefab != null) inst = Instantiate(g3Prefab, mount);
            else inst = mount.GetComponentInChildren<G3BeatJump>(true);
            if (inst == null) inst = gameObject.AddComponent<G3BeatJump>();
            return inst;
        }

        List<G3BeatJump.Pattern> GetG3PatternsForStage(CampaignId id, int stage) {
            var list = new List<G3BeatJump.Pattern>();
            // 캠페인 B: 1~4가 G3
            if (id != CampaignId.B) { list.Add(new G3BeatJump.Pattern(4, 60)); return list; }

            switch (Mathf.Clamp(stage, 1, 4)) {
                case 1: // 기본 4박자, BPM 60 중심
                    list.Add(new G3BeatJump.Pattern(4, 60));
                    list.Add(new G3BeatJump.Pattern(4, 60));
                    list.Add(new G3BeatJump.Pattern(5, 60));
                    list.Add(new G3BeatJump.Pattern(6, 60));
                    list.Add(new G3BeatJump.Pattern(4, 70));
                    break;
                case 2: // 속도 변화(70~85)
                    list.Add(new G3BeatJump.Pattern(4, 70));
                    list.Add(new G3BeatJump.Pattern(5, 70));
                    list.Add(new G3BeatJump.Pattern(4, 80));
                    list.Add(new G3BeatJump.Pattern(6, 80));
                    list.Add(new G3BeatJump.Pattern(5, 85));
                    break;
                case 3: // 긴 패턴(80~90, 8~10박)
                    list.Add(new G3BeatJump.Pattern(8, 80));
                    list.Add(new G3BeatJump.Pattern(6, 85)); // 빠르게 6박
                    list.Add(new G3BeatJump.Pattern(9, 88));
                    list.Add(new G3BeatJump.Pattern(10, 90));
                    list.Add(new G3BeatJump.Pattern(8, 90));
                    break;
                case 4: // 빠른 템포(100~120)
                default:
                    list.Add(new G3BeatJump.Pattern(4, 100));
                    list.Add(new G3BeatJump.Pattern(6, 110));
                    list.Add(new G3BeatJump.Pattern(4, 120));
                    list.Add(new G3BeatJump.Pattern(5, 120));
                    list.Add(new G3BeatJump.Pattern(8, 110));
                    break;
            }
            return list;
        }

        void GetG3TuningForStage(CampaignId id, int stage, out float hitWindow, out float requiredRatio) {
            // 난이도에 따라 허용오차/요구정확도 조정
            if (id != CampaignId.B) { hitWindow = 0.15f; requiredRatio = 0.6f; return; }

            switch (Mathf.Clamp(stage, 1, 4)) {
                case 1: hitWindow = 0.18f; requiredRatio = 0.55f; break;
                case 2: hitWindow = 0.15f; requiredRatio = 0.60f; break;
                case 3: hitWindow = 0.12f; requiredRatio = 0.65f; break;
                case 4:
                default: hitWindow = 0.10f; requiredRatio = 0.70f; break;
            }
        }

        // -------------- 공통 결과 처리 --------------
        void OnCommonFinished(int total, int correct, float avgMetric) {
            bool success = (total > 0) && (correct / (float)total >= clearAccuracy);
            if (success) ProgressStore.SetCleared(_campaign, _stage);

            var summary = new GameResultBus.Summary {
                campaign = _campaign, stage = _stage,
                totalTrials = total, correct = correct,
                avgReaction = avgMetric, // G1/G2는 평균 반응시간, G3는 평균 오차(초)를 재사용
                success = success
            };
            GameResultBus.Set(summary);
            ShowResult(summary);
        }

        // -------------- Stub (G4~G6 교체 예정) --------------
        void RunTwoChoiceStub(GameMode mode) {
            if (_two == null) _two = CreateTwoChoice(playArea);

            var labels = GetLabelsForMode(mode);
            _two.ConfigureLabels(labels.left, labels.right, labels.prompt);
            _pausable = _two;

            _two.OnGameFinished = OnCommonFinished;

            ServiceHub.I.Caption.ShowTop("문제를 보고 정답을 고르세요.");
            _two.StartGame();
        }
        TwoChoiceMinigame CreateTwoChoice(RectTransform mount) {
            TwoChoiceMinigame inst = null;
            if (twoChoicePrefab != null) inst = Instantiate(twoChoicePrefab, mount);
            else inst = mount.GetComponentInChildren<TwoChoiceMinigame>(true);
            if (inst == null) inst = gameObject.AddComponent<TwoChoiceMinigame>();
            return inst;
        }

        // -------------- Result (same scene) --------------
        void ShowResult(GameResultBus.Summary s) {
            settingsModal?.Close();
            if (resultOverlay != null) {
                resultOverlay.Show(
                    s,
                    onExit:  OnExitToStageSelect,
                    onRetry: OnRetry,
                    onNext:  () => OnNext(s)
                );
            } else {
                ServiceHub.I.Caption.ShowCenter($"정확도 {(s.correct/(float)s.totalTrials*100f):0}% • 지표 {s.avgReaction:0.00}s");
            }
        }

        // -------------- Navigation --------------
        void OnExitToStageSelect() { SceneManager.LoadScene(Scenes.StageSelect); }
        void OnRetry() { SceneManager.LoadScene(Scenes.Game); }
        void OnNext(GameResultBus.Summary s) {
            if (!(s.success && s.stage < 8)) return;
            GameRouter.SelectStage(Mathf.Clamp(s.stage + 1, 1, 8));
            SceneManager.LoadScene(Scenes.Game);
        }

        // -------------- Helpers --------------
        string BuildHeaderLabel(CampaignId id, int stage, GameMode m, GameMode alt) {
            var label = $"{id} • 레벨 {stage} • {GetModeLabel(m)}";
            if (stage == 8 && alt != GameMode.None) label += $" / {GetModeLabel(alt)}";
            return label;
        }
        string GetModeLabel(GameMode m) {
            switch (m) {
                case GameMode.G1: return "거품: 다른 소리 찾기";
                case GameMode.G2: return "멜로디 방향 맞히기";
                case GameMode.G3: return "박자에 맞춰 점프";
                case GameMode.G4: return "리듬 따라치기";
                case GameMode.G5: return "다른 악기 찾기";
                case GameMode.G6: return "악기 소리 맞히기";
                default:          return "-";
            }
        }
        (GameMode main, GameMode alt) ResolveModes(CampaignId id, int stage) {
            switch (id) {
                case CampaignId.A:
                    if (stage <= 4) return (GameMode.G1, GameMode.None);
                    if (stage <= 7) return (GameMode.G2, GameMode.None);
                    return (GameMode.G1, GameMode.G2);
                case CampaignId.B:
                    if (stage <= 4) return (GameMode.G3, GameMode.None);
                    if (stage <= 7) return (GameMode.G4, GameMode.None);
                    return (GameMode.G3, GameMode.G4);
                case CampaignId.C:
                    if (stage <= 4) return (GameMode.G5, GameMode.None);
                    if (stage <= 7) return (GameMode.G6, GameMode.None);
                    return (GameMode.G5, GameMode.G6);
                default:
                    return (GameMode.G1, GameMode.None);
            }
        }
        (string left, string right, string prompt) GetLabelsForMode(GameMode m) {
            switch (m) {
                case GameMode.G2: return ("상승", "하강", "멜로디의 방향을 고르세요");
                case GameMode.G3: return ("정박", "비정박", "리듬을 구분하세요");
                case GameMode.G4: return ("패턴A", "패턴B", "리듬 패턴을 구분하세요");
                case GameMode.G5: return ("악기1", "악기2", "다른 악기를 고르세요");
                case GameMode.G6: return ("정답", "오답", "악기 소리를 맞히세요");
                default:          return ("A", "B", "정답을 고르세요");
            }
        }
    }

    public enum GameMode { None = 0, G1, G2, G3, G4, G5, G6 }
}