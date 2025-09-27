using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AuralRehab.Core.Data;

namespace AuralRehab.Application {
    /// <summary>
    /// 스테이지 선택 화면(씬 배치형 + 캠페인별 테마 적용):
    /// - 배치는 씬에서 직접 배치
    /// - 선택된 캠페인(A/B/C)에 따라 테마(배경/타일/색/보스 강조/보석 아이콘)를 적용
    /// - 진행도(잠금/해제/클리어)는 ProgressStore(JSON)로 분리 유지
    /// </summary>
    public class StageSelectController : MonoBehaviour {
        [Header("UI Refs (Assign in Inspector)")]
        [SerializeField] TMP_FontAsset uiFont;     // 선택 사항(전역/현재 화면 폰트 주입)
        [SerializeField] TMP_Text titleText;
        [SerializeField] Button btnBack;
        [SerializeField] Button btnResetProgress;  // 선택(개발용)
        [SerializeField] Image backgroundImage;     // 전체 배경 Image

        [Space(6)]
        [SerializeField] StageTileUI[] tiles = new StageTileUI[8]; // 8개 연결

        [Header("Flow")]
        [SerializeField] string gameScene = Scenes.Game;
        [SerializeField] float fadeOutDuration = 0.35f;

        [Header("Themes (Assign per Campaign)")]
        [SerializeField] StageSelectTheme themeA;
        [SerializeField] StageSelectTheme themeB;
        [SerializeField] StageSelectTheme themeC;

        CampaignId _campaign;
        StageSelectTheme _theme;

        void Awake() {
            EnsureMainCamera();
            EnsureServiceHub();

            _campaign = GameRouter.SelectedCampaign;
            _theme    = GetTheme(_campaign);

            // 폰트 적용(선택)
            ApplyFontIfSet(titleText);
            foreach (var t in tiles) ApplyFontTile(t);

            // 버튼
            if (btnBack) btnBack.onClick.AddListener(OnBack);
            if (btnResetProgress) btnResetProgress.onClick.AddListener(() => { ProgressStore.ResetAll(); RefreshTiles(); });

            // 타일 클릭 핸들러 등록 + stageNumber 보정
            for (int i = 0; i < tiles.Length; i++) {
                var t = tiles[i];
                if (t == null) continue;
                if (t.stageNumber <= 0) t.stageNumber = i + 1;
                var localStage = t.stageNumber;
                if (t.button != null) t.button.onClick.AddListener(() => OnPick(localStage));
            }

            ApplyThemeVisuals();
            RefreshTiles();

            ServiceHub.I.Caption.ShowTop($"{_campaign} 캠페인: 스테이지를 선택하세요.");
        }

        // ----------------- Theme -----------------
        StageSelectTheme GetTheme(CampaignId id) => id switch {
            CampaignId.A => themeA,
            CampaignId.B => themeB,
            CampaignId.C => themeC,
            _            => themeA
        };

        void ApplyThemeVisuals() {
            if (_theme == null) return;

            // 배경
            if (backgroundImage != null) {
                backgroundImage.sprite = _theme.backgroundSprite;
                backgroundImage.color  = _theme.backgroundColor;
                backgroundImage.enabled = (_theme.backgroundSprite != null) || _theme.backgroundColor.a > 0f;
                backgroundImage.preserveAspect = true;
            }

            // 타일 공통 스프라이트/색 초기값
            foreach (var t in tiles) {
                if (t == null) continue;
                if (t.background != null) {
                    t.background.sprite = _theme.tileNormal != null ? _theme.tileNormal : t.background.sprite;
                    t.background.color  = _theme.tileTintNormal;
                }
                // 보석 아이콘은 기본 비활성화해 두고, 밑에서 선택적으로 켠다.
                if (t.rewardIcon != null) t.rewardIcon.enabled = false;
            }
        }

        // ----------------- Progress & Tiles -----------------
        void RefreshTiles() {
            int maxCleared = ProgressStore.GetMaxCleared(_campaign);

            for (int i = 0; i < tiles.Length; i++) {
                var t = tiles[i];
                if (t == null) continue;

                int stage = Mathf.Clamp(t.stageNumber <= 0 ? i + 1 : t.stageNumber, 1, 8);
                bool unlocked = ProgressStore.IsUnlocked(_campaign, stage);
                bool cleared  = stage <= maxCleared;
                bool isBoss   = (stage == 8);

                // 상태 반영: 스프라이트 + 틴트
                if (t.background != null && _theme != null) {
                    if (cleared && _theme.tileCleared != null) t.background.sprite = _theme.tileCleared;
                    else if (!unlocked && _theme.tileLocked != null) t.background.sprite = _theme.tileLocked;
                    else if (_theme.tileNormal != null) t.background.sprite = _theme.tileNormal;

                    Color baseTint = cleared ? _theme.tileTintCleared : (unlocked ? _theme.tileTintNormal : _theme.tileTintLocked);
                    // 보스 강조
                    if (_theme.useBossAccent && isBoss) baseTint = Color.Lerp(baseTint, _theme.bossAccent, 0.7f);
                    t.background.color = baseTint;
                }

                // 버튼 상호작용
                if (t.button != null) t.button.interactable = unlocked;

                // 텍스트
                if (t.num != null) {
                    t.num.text = stage.ToString();
                    t.num.color = unlocked ? _theme.textNormal : _theme.textLocked;
                }
                if (t.sub != null) {
                    bool isBossStage; string label = GetStageLabel(_campaign, stage, out isBossStage);
                    t.sub.text  = isBossStage ? $"레벨 {stage} • 보스전: {label}" : $"레벨 {stage} • {label}";
                    t.sub.color = unlocked ? _theme.subTextNormal : _theme.textLocked;
                }

                // 클리어 마크
                if (t.clearedCheck != null) {
                    if (string.IsNullOrEmpty(t.clearedCheck.text)) t.clearedCheck.text = "✓";
                    t.clearedCheck.enabled = cleared;
                    t.clearedCheck.color   = _theme.textNormal;
                }

                // 선택적 보석/보상 아이콘
                if (t.rewardIcon != null && _theme != null) {
                    bool showReward = _theme.rewardStages != null && _theme.rewardStages.Contains(stage) && _theme.rewardSprite != null;
                    t.rewardIcon.enabled = showReward;
                    if (showReward) {
                        t.rewardIcon.sprite = _theme.rewardSprite;
                        t.rewardIcon.preserveAspect = true;
                        t.rewardIcon.color = Color.white;
                    }
                }
            }
        }

        // ----------------- Flow -----------------
        void OnPick(int stage) {
            bool unlocked = ProgressStore.IsUnlocked(_campaign, stage);
            if (!unlocked) {
                ServiceHub.I.Caption.ShowTop("잠금된 레벨입니다. 이전 레벨을 먼저 완료하세요.");
                return;
            }

            GameRouter.SelectStage(stage);
            bool isBoss; string label = GetStageLabel(_campaign, stage, out isBoss);
            ServiceHub.I.Caption.ShowTop($"{_campaign} - 레벨 {stage} • {(isBoss ? "보스전: " : "")}{label}");

            StartCoroutine(GoPlay());
        }

        IEnumerator GoPlay() {
            if (ServiceHub.I?.Fader != null) {
                ServiceHub.I.Fader.PlayFadeOut(fadeOutDuration, Color.black);
                yield return new WaitForSecondsRealtime(fadeOutDuration * 0.95f);
            }
            try { SceneManager.LoadScene(gameScene); }
            catch { Debug.Log($"씬 {gameScene} 이(가) 빌드에 없습니다."); }
        }

        void OnBack() {
            try { SceneManager.LoadScene(Scenes.CharacterSelect); }
            catch { Debug.Log($"씬 {Scenes.CharacterSelect} 이(가) 빌드에 없습니다."); }
        }

        // ----------------- Helpers -----------------
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
            if (uiFont) {
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

        void ApplyFontTile(StageTileUI t) {
            if (t == null) return;
            if (t.num) ApplyFontIfSet(t.num);
            if (t.sub) ApplyFontIfSet(t.sub);
            if (t.clearedCheck) ApplyFontIfSet(t.clearedCheck);
        }

        // 캠페인별 라벨
        string GetStageLabel(CampaignId id, int stage, out bool isBoss) {
            isBoss = (stage == 8);
            switch (id) {
                case CampaignId.A:
                    if (stage <= 4) return "거품 속 다른 소리 찾기 (G1)";
                    if (stage <= 7) return "멜로디 모양 맞히기 (G2)";
                    return "G1 + G2 혼합";
                case CampaignId.B:
                    if (stage <= 4) return "박자에 맞춰 점프 (G3)";
                    if (stage <= 7) return "리듬 따라치기 (G4)";
                    return "G3 + G4 혼합";
                case CampaignId.C:
                    if (stage <= 4) return "다른 악기 찾기 (G5)";
                    if (stage <= 7) return "악기 맞히기 (G6)";
                    return "G5 + G6 혼합";
                default:
                    if (stage <= 4) return "거품 속 다른 소리 찾기 (G1)";
                    if (stage <= 7) return "멜로디 모양 맞히기 (G2)";
                    return "혼합";
            }
        }
    }

    [System.Serializable]
    public class StageTileUI {
        [Min(1)] public int stageNumber = 1;   // 1~8
        public Button   button;                // 클릭 영역
        public Image    background;            // 타일 배경(스프라이트 교체 대상)
        public TMP_Text num;                   // 큰 숫자
        public TMP_Text sub;                   // 보조 텍스트
        public TMP_Text clearedCheck;          // 클리어 마크(✓)
        public Image    rewardIcon;            // 선택적 보상 아이콘(예: 보석)
    }
}