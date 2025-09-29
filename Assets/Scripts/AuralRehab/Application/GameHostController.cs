using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using AuralRehab.Core.Data;
using AuralRehab.GamePlay;

namespace AuralRehab.Application {
    /// <summary>
    /// 게임 호스트(같은 씬 결과 오버레이 + 자동 시작 + 설정 모달)
    /// - G1~G3 연결, G4~G6은 임시 스텁
    /// - Dev/Debug: 인스펙터 오버라이드, 단축키, 오버레이
    /// </summary>
    public class GameHostController : MonoBehaviour {
        [Header("UI Refs (Assign in Inspector)")]
        [SerializeField] TMP_FontAsset uiFont;
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text hintText;
        [SerializeField] Button btnBack;
        [SerializeField] Button btnRetry;
        [SerializeField] Button btnStart;
        [SerializeField] Button btnMenu;

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

        // ---------- Dev / Debug ----------
        [Header("Dev / Debug (Optional)")]
        [SerializeField] bool devMode = false;
        [SerializeField] bool devShowOverlay = true;
        [SerializeField] bool devOverrideRoute = false;
        [SerializeField] CampaignId devCampaign = CampaignId.A;
        [SerializeField, Range(1,8)] int devStage = 1;
        [SerializeField] GameMode devModeOverride = GameMode.None; // None이면 스테이지 규칙 사용
        [SerializeField] int   devTrialsOverride = 0;               // 0 이하 = 미사용
        [SerializeField] float devAutoStartDelay = -1f;            // < 0 = 미사용
        [SerializeField] bool devDeterministic = false;
        [SerializeField] int  devRandomSeed = 12345;
        [SerializeField] bool devVerboseLog = false;

        // ---------- State ----------
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

        // Overlay
        GameDebugOverlay _overlay;

        void Awake() {
            EnsureMainCamera();
            EnsureServiceHub();
            ApplyDevRouting();

            ApplyFontIfSet(headerText);
            ApplyFontIfSet(hintText);

            if (btnBack)  btnBack.onClick.AddListener(OnExitToStageSelect);
            if (btnRetry) btnRetry.onClick.AddListener(OnRetry);
            if (btnStart) btnStart.onClick.AddListener(OnStartGame);
            if (btnMenu)  btnMenu.onClick.AddListener(OnOpenMenu);

            if (resultOverlay) resultOverlay.HideImmediate();
            if (settingsModal) settingsModal.Setup(OnCloseMenu, OnRetry, OnExitToStageSelect);

            headerText.text = BuildHeaderLabel(_campaign, _stage, _modePrimary, _modeAlt);
            ServiceHub.I.Caption.ShowTop($"게임이 {GetAutoStartDelay():0}초 후 자동 시작됩니다.");
            if (btnStart) btnStart.gameObject.SetActive(false);

            if (devMode && devShowOverlay) {
                _overlay = GameDebugOverlay.Create(transform, uiFont);
                _overlay.Bind(this);
            }
        }

        void Start() { StartCoroutine(AutoStartAfterDelay()); }

        // ---------- Dev Routing ----------
        void ApplyDevRouting() {
            if (devMode && devDeterministic) UnityEngine.Random.InitState(devRandomSeed);

            _campaign = GameRouter.SelectedCampaign;
            _stage    = Mathf.Clamp(GameRouter.SelectedStage, 1, 8);

            if (devMode && devOverrideRoute) {
                _campaign = devCampaign;
                _stage    = Mathf.Clamp(devStage, 1, 8);
            }

            (_modePrimary, _modeAlt) = ResolveModes(_campaign, _stage);

            if (devMode && devOverrideRoute && devModeOverride != GameMode.None) {
                _modePrimary = devModeOverride;
                _modeAlt     = GameMode.None;
            }

            if (devMode && devTrialsOverride > 0) trialsPerGame = devTrialsOverride;
            if (devMode && devAutoStartDelay >= 0f) autoStartDelay = devAutoStartDelay;

            LogDev($"Route = {_campaign}/{_stage} • Mode={_modePrimary}{(_modeAlt!=GameMode.None?("+"+_modeAlt):"")}, Trials={trialsPerGame}, Delay={autoStartDelay:0.##}");
        }

        float GetAutoStartDelay() => Mathf.Max(0f, autoStartDelay);

        IEnumerator AutoStartAfterDelay() {
            float t = 0f;
            float wait = GetAutoStartDelay();
            while (t < wait) { t += Time.unscaledDeltaTime; yield return null; }
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
            SafeInvoke.TryCall(_g1, "SetInterval", interval);
            SafeInvoke.TryCall(_g1, "SetUseUnscaledTime", true);
            SafeInvoke.SetTrialsIfSupported(_g1, GetTrials());

            _pausable = _g1 as IPausableGame;
            // 결과 콜백 바인딩(있을 때만)
            SafeInvoke.TryAssignAction(_g1, "OnGameFinished", new Action<int,int,float>(OnCommonFinished));

            ServiceHub.I.Caption.ShowTop("각 선택지의 소리를 듣고, 다른 소리를 고르세요.");
            _g1.gameObject.SetActive(true);
            SafeInvoke.TryCall(_g1, "StartGame");
        }
        G1OddOneOutPitch CreateG1(RectTransform mount) {
            G1OddOneOutPitch inst = null;
            if (g1Prefab != null) inst = Instantiate(g1Prefab, mount);
            else inst = mount.GetComponentInChildren<G1OddOneOutPitch>(true);
            if (inst == null) inst = gameObject.AddComponent<G1OddOneOutPitch>();
            return inst;
        }
        int GetIntervalForStage(CampaignId id, int stage) {
            switch (stage) { case 1: return 7; case 2: return 4; case 3: return 2; default: return 1; }
        }

        // ---------------- G2 ----------------
        void RunG2() {
            if (_g2 == null) _g2 = CreateG2(playArea);
            var t = GetG2TaskForStage(_campaign, _stage);
            _g2.SetTask(t);
            _g2.SetStep(2);
            SafeInvoke.SetTrialsIfSupported(_g2, GetTrials());
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

            var patterns = GetG3PatternsForStage(_campaign, _stage);
            float hitWindow, reqRatio; GetG3TuningForStage(_campaign, _stage, out hitWindow, out reqRatio);

            _g3.SetPatterns(patterns);
            _g3.SetHitWindowSeconds(hitWindow);
            _g3.SetRequiredHitRatio(reqRatio);
            SafeInvoke.SetTrialsIfSupported(_g3, Mathf.Min(GetTrials(), patterns.Count));
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
            if (id != CampaignId.B) { list.Add(new G3BeatJump.Pattern(4, 60)); return list; }
            switch (Mathf.Clamp(stage, 1, 4)) {
                case 1: list.AddRange(new[]{ new G3BeatJump.Pattern(4,60), new G3BeatJump.Pattern(4,60), new G3BeatJump.Pattern(5,60), new G3BeatJump.Pattern(6,60), new G3BeatJump.Pattern(4,70)}); break;
                case 2: list.AddRange(new[]{ new G3BeatJump.Pattern(4,70), new G3BeatJump.Pattern(5,70), new G3BeatJump.Pattern(4,80), new G3BeatJump.Pattern(6,80), new G3BeatJump.Pattern(5,85)}); break;
                case 3: list.AddRange(new[]{ new G3BeatJump.Pattern(8,80), new G3BeatJump.Pattern(6,85), new G3BeatJump.Pattern(9,88), new G3BeatJump.Pattern(10,90), new G3BeatJump.Pattern(8,90)}); break;
                default:list.AddRange(new[]{ new G3BeatJump.Pattern(4,100),new G3BeatJump.Pattern(6,110),new G3BeatJump.Pattern(4,120),new G3BeatJump.Pattern(5,120),new G3BeatJump.Pattern(8,110)}); break;
            }
            return list;
        }
        void GetG3TuningForStage(CampaignId id, int stage, out float hitWindow, out float requiredRatio) {
            if (id != CampaignId.B) { hitWindow = 0.15f; requiredRatio = 0.6f; return; }
            switch (Mathf.Clamp(stage, 1, 4)) {
                case 1: hitWindow = 0.18f; requiredRatio = 0.55f; break;
                case 2: hitWindow = 0.15f; requiredRatio = 0.60f; break;
                case 3: hitWindow = 0.12f; requiredRatio = 0.65f; break;
                default:hitWindow = 0.10f; requiredRatio = 0.70f; break;
            }
        }

        // -------------- 공통 결과 처리 --------------
        void OnCommonFinished(int total, int correct, float avgMetric) {
            bool success = (total > 0) && (correct / (float)total >= clearAccuracy);
            if (success) ProgressStore.SetCleared(_campaign, _stage);

            var summary = new GameResultBus.Summary {
                campaign = _campaign, stage = _stage,
                totalTrials = total, correct = correct,
                avgReaction = avgMetric,
                success = success
            };
            GameResultBus.Set(summary);
            ShowResult(summary);
            _overlay?.NotifyResult(summary);
        }

        // -------------- Stub (G4~G6 교체 예정) --------------
        void RunTwoChoiceStub(GameMode mode) {
            if (_two == null) _two = CreateTwoChoice(playArea);
            var labels = GetLabelsForMode(mode);
            SafeInvoke.TryCall(_two, "ConfigureLabels", labels.left, labels.right, labels.prompt);
            SafeInvoke.SetTrialsIfSupported(_two, GetTrials());

            _pausable = _two as IPausableGame;
            SafeInvoke.TryAssignAction(_two, "OnGameFinished", new Action<int,int,float>(OnCommonFinished));

            ServiceHub.I.Caption.ShowTop("문제를 보고 정답을 고르세요.");
            SafeInvoke.TryCall(_two, "StartGame");
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
        int GetTrials() => Mathf.Max(1, trialsPerGame);

        void LogDev(string msg) { if (devMode && devVerboseLog) Debug.Log($"[GameHost] {msg}"); }

        // ---------- Dev Shortcuts ----------
        void Update() {
            if (!devMode) return;

            if (Input.GetKeyDown(KeyCode.F1)) _overlay?.ToggleVisible();
            if (Input.GetKeyDown(KeyCode.F2)) OnRetry();
            if (Input.GetKeyDown(KeyCode.F3)) { GameRouter.SelectStage(Mathf.Clamp(_stage + 1, 1, 8)); SceneManager.LoadScene(Scenes.Game); }
            if (Input.GetKeyDown(KeyCode.F4)) { GameRouter.SelectStage(Mathf.Clamp(_stage - 1, 1, 8)); SceneManager.LoadScene(Scenes.Game); }
            if (Input.GetKeyDown(KeyCode.F5)) {
                var next = (CampaignId)(((int)_campaign + 1) % 3);
                GameRouter.SelectCampaign(next);
                SceneManager.LoadScene(Scenes.Game);
            }
            if (Input.GetKeyDown(KeyCode.P)) {
                if (_pausable != null) {
                    if (_pausable.IsPaused) _pausable.Resume();
                    else _pausable.Pause();
                }
            }
            if (Input.GetKeyDown(KeyCode.BackQuote)) {
                Time.timeScale = (Mathf.Abs(Time.timeScale - 1f) < 0.01f) ? 0.2f : 1f;
                ServiceHub.I.Caption.ShowTop($"TimeScale {Time.timeScale:0.##}");
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) ForceModeAndReload(GameMode.G1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) ForceModeAndReload(GameMode.G2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) ForceModeAndReload(GameMode.G3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) ForceModeAndReload(GameMode.G4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) ForceModeAndReload(GameMode.G5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) ForceModeAndReload(GameMode.G6);

            if (_overlay) _overlay.Tick(this, _pausable, _g1, _g2, _g3);
        }

        void ForceModeAndReload(GameMode m) {
            devOverrideRoute = true; devModeOverride = m;
            GameRouter.SelectStage(_stage);
            GameRouter.SelectCampaign(_campaign);
            SceneManager.LoadScene(Scenes.Game);
        }

        // ---------- 안전 호출 유틸 ----------
        static class SafeInvoke {
            public static bool TryCall(object target, string methodName, params object[] args) {
                if (target == null) return false;
                var t = target.GetType();
                var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var m in methods) {
                    if (m.Name != methodName) continue;
                    var ps = m.GetParameters();
                    if (ps.Length != args.Length) continue;
                    bool ok = true;
                    for (int i = 0; i < ps.Length; i++) {
                        if (args[i] == null) continue;
                        if (!ps[i].ParameterType.IsAssignableFrom(args[i].GetType())) { ok = false; break; }
                    }
                    if (!ok) continue;
                    m.Invoke(target, args);
                    return true;
                }
                return false;
            }
            public static bool TrySet(object target, string member, object value) {
                if (target == null) return false;
                var t = target.GetType();
                var prop = t.GetProperty(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.CanWrite) { prop.SetValue(target, value); return true; }
                var field = t.GetField(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) { field.SetValue(target, value); return true; }
                return false;
            }
            public static void SetTrialsIfSupported(object target, int n) {
                if (TryCall(target, "SetTotalTrials", n)) return;
                if (TryCall(target, "SetTrials", n)) return;
                if (TrySet(target, "totalTrials", n)) return;
                if (TrySet(target, "Trials", n)) return;
                if (TrySet(target, "trialCount", n)) return;
            }
            public static bool TryAssignAction(object target, string fieldOrProp, Delegate del) {
                if (target == null) return false;
                var t = target.GetType();
                // Property 우선
                var prop = t.GetProperty(fieldOrProp, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.CanWrite) { prop.SetValue(target, del); return true; }
                // Field
                var field = t.GetField(fieldOrProp, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) { field.SetValue(target, del); return true; }
                return false;
            }
        }
    }

    public enum GameMode { None = 0, G1, G2, G3, G4, G5, G6 }
}