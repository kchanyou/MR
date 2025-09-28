using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using AuralRehab.Core.Data;
using AuralRehab.GamePlay;

namespace AuralRehab.Application {
    /// <summary>
    /// 게임 호스트(같은 씬 결과 오버레이 + 자동 시작 + 설정 모달)
    /// - 씬 로드 후 3초 자동 시작
    /// - 햄버거 버튼으로 설정 모달(계속/재시도/뒤로가기)
    /// - G1은 전용 컴포넌트, 나머지는 스텁 → 차례대로 교체 예정
    /// </summary>
    public class GameHostController : MonoBehaviour {
        [Header("UI Refs (Assign in Inspector)")]
        [SerializeField] TMP_FontAsset uiFont;     // 선택 사항
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text hintText;
        [SerializeField] Button btnBack;           // 옵션(상단 뒤로가기 버튼이 별도 있으면 연결)
        [SerializeField] Button btnRetry;          // 옵션
        [SerializeField] Button btnStart;          // 자동 시작으로 숨김/미사용 가능
        [SerializeField] Button btnMenu;           // 햄버거 버튼

        [Header("Play Area")]
        [SerializeField] RectTransform playArea;

        [Header("Prefabs / Components")]
        [SerializeField] G1OddOneOutPitch g1Prefab;        // G1 전용
        [SerializeField] TwoChoiceMinigame twoChoicePrefab; // 임시(기타 모드)
        [SerializeField] ResultOverlay resultOverlay;       // 같은 씬 결과 오버레이
        [SerializeField] SettingsModal settingsModal;       // 설정 모달

        [Header("Rules (Common)")]
        [SerializeField, Range(0f,1f)] float clearAccuracy = 0.6f;
        [SerializeField] int trialsPerGame = 8;
        [SerializeField] float autoStartDelay = 3f;

        CampaignId _campaign;
        int _stage;
        GameMode _modePrimary;
        GameMode _modeAlt;

        // 런타임 인스턴스
        G1OddOneOutPitch _g1;
        TwoChoiceMinigame _two;
        IPausableGame _pausable;
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

            // 자동 시작 안내
            ServiceHub.I.Caption.ShowTop($"게임이 {autoStartDelay:0}초 후 자동 시작됩니다.");
            if (btnStart) btnStart.gameObject.SetActive(false); // 자동 시작이므로 비활성화
        }

        void Start() {
            StartCoroutine(AutoStartAfterDelay());
        }

        IEnumerator AutoStartAfterDelay() {
            // 일시정지에 영향받지 않도록 Realtime 대기
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

            if (_modePrimary == GameMode.G1) {
                RunG1();
            } else {
                RunTwoChoiceStub(_modePrimary);
            }
        }

        void OnOpenMenu() {
            // 게임 일시정지
            _pausable?.Pause();
            settingsModal?.Open();
        }

        void OnCloseMenu() {
            settingsModal?.Close();
            _pausable?.Resume();
        }

        // ---------------- G1 ----------------
        void RunG1() {
            if (_g1 == null) _g1 = CreateG1(playArea);

            int interval = GetIntervalForStage(_campaign, _stage);
            _g1.SetInterval(interval);
            _g1.SetUseUnscaledTime(true); // 모달 정지 루프는 내부에서 처리

#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(_g1);
            var p = so.FindProperty("totalTrials");
            if (p != null) { p.intValue = Mathf.Max(1, trialsPerGame); so.ApplyModifiedPropertiesWithoutUndo(); }
#endif
            _pausable = _g1;

            _g1.OnGameFinished = (total, correct, avgRt) => {
                bool success = (total > 0) && (correct / (float)total >= clearAccuracy);
                if (success) ProgressStore.SetCleared(_campaign, _stage);

                var summary = new GameResultBus.Summary {
                    campaign = _campaign, stage = _stage,
                    totalTrials = total, correct = correct,
                    avgReaction = avgRt, success = success
                };
                GameResultBus.Set(summary);
                ShowResult(summary);
            };

            ServiceHub.I.Caption.ShowTop("3개의 소리를 듣고, 다른 소리를 탭하세요.");
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
            // A: 1=완전5도, 2=장3도, 3=장2도, 4=반음 (A의 1~4를 G1로 사용)
            switch (stage) {
                case 1: return 7; case 2: return 4; case 3: return 2; default: return 1;
            }
        }

        // -------------- Stub fallback --------------
        void RunTwoChoiceStub(GameMode mode) {
            if (_two == null) _two = CreateTwoChoice(playArea);

            var labels = GetLabelsForMode(mode);
            _two.ConfigureLabels(labels.left, labels.right, labels.prompt);
#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(_two);
            var p  = so.FindProperty("totalTrials"); if (p != null) p.intValue = Mathf.Max(1, trialsPerGame);
            so.ApplyModifiedPropertiesWithoutUndo();
#endif
            _pausable = _two;

            _two.OnGameFinished = (total, correct, avgRt) => {
                bool success = (total > 0) && (correct / (float)total >= clearAccuracy);
                if (success) ProgressStore.SetCleared(_campaign, _stage);

                var summary = new GameResultBus.Summary {
                    campaign = _campaign, stage = _stage,
                    totalTrials = total, correct = correct,
                    avgReaction = avgRt, success = success
                };
                GameResultBus.Set(summary);
                ShowResult(summary);
            };

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
            if (settingsModal) settingsModal.Close(); // 혹시 열려있다면 닫기
            if (resultOverlay != null) {
                resultOverlay.Show(
                    s,
                    onExit:  OnExitToStageSelect,
                    onRetry: OnRetry,
                    onNext:  () => OnNext(s)
                );
            } else {
                ServiceHub.I.Caption.ShowCenter($"정확도 {(s.correct/(float)s.totalTrials*100f):0}% • 반응 {s.avgReaction:0.00}s");
            }
        }

        // -------------- Navigation --------------
        void OnExitToStageSelect() {
            SceneManager.LoadScene(Scenes.StageSelect);
        }
        void OnRetry() {
            SceneManager.LoadScene(Scenes.Game);
        }
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