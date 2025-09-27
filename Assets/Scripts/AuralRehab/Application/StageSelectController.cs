using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AuralRehab.Core.Data;

namespace AuralRehab.Application {
    /// <summary>
    /// 스테이지 선택 화면(1~8):
    /// - 잠금/해제/클리어 상태 표시
    /// - 잠금 클릭 시 캡션 안내, 해제된 스테이지 클릭 시 Game으로 이동
    /// - 진행도: ProgressStore(로컬 JSON)
    /// - TMP 전용 UI, ServiceHub/Caption/Fader 자동 사용
    /// </summary>
    public class StageSelectController : MonoBehaviour {
        [Header("UI")]
        public TMP_FontAsset uiFont;                 // PretendardVariable SDF 등
        public Vector2 referenceResolution = new(1080, 1920);

        [Header("Flow")]
        public string gameScene = Scenes.Game;
        public float fadeOutDuration = 0.35f;

        // 색상
        Color _tileNormal   = new Color(1, 1, 1, 0.10f);
        Color _tileLocked   = new Color(1, 1, 1, 0.04f);
        Color _tileCleared  = new Color(0.20f, 0.80f, 0.35f, 0.28f);
        Color _textNormal   = Color.white;
        Color _textLocked   = new Color(1, 1, 1, 0.45f);

        Canvas _canvas;
        CampaignId _campaign;
        StageTile[] _tiles = new StageTile[8];

        void Awake() {
            EnsureMainCamera();
            EnsureServiceHub();
            _campaign = GameRouter.SelectedCampaign; // 기본값 A

            BuildUI();
            RefreshTiles();

            ServiceHub.I.Caption.ShowTop($"{_campaign} 캠페인: 스테이지를 선택하세요.");
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
            // 폰트 동기화(선택)
            if (uiFont != null) {
                ServiceHub.I.Caption.SetFont(uiFont);
                var tmp = ServiceHub.I.Caption.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null && uiFont.material != null) tmp.fontSharedMaterial = uiFont.material;
            }
        }

        void BuildUI() {
            // Canvas
            var cvsGO = new GameObject("StageSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cvsGO.transform.SetParent(transform, false);
            _canvas = cvsGO.GetComponent<Canvas>();
            var scaler = cvsGO.GetComponent<CanvasScaler>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 30000;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;

            // Title
            var title = CreateTMP(cvsGO.transform, "스테이지 선택", 48, TextAlignmentOptions.Center, bold:true);
            Anchor(title.rectTransform, 0.88f, 0.96f, 0.1f, 0.9f);

            // 그리드 2행 × 4열
            var grid = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
            grid.SetParent(cvsGO.transform, false);
            Anchor(grid, 0.28f, 0.80f, 0.07f, 0.93f);

            // 8개 타일 생성
            for (int i = 0; i < 8; i++) {
                int stageNum = i + 1;
                _tiles[i] = CreateTile(grid, stageNum, () => OnPick(stageNum));
            }
            // 배치
            for (int row = 0; row < 2; row++) {
                for (int col = 0; col < 4; col++) {
                    int idx = row * 4 + col;
                    var rt = _tiles[idx].root;
                    float xMin = 0.00f + col * 0.25f;
                    float xMax = xMin + 0.25f;
                    float yMax = 1.00f - row * 0.50f;
                    float yMin = yMax - 0.50f;
                    Place(rt, xMin, xMax, yMin, yMax);
                }
            }

            // 하단 버튼
            var btnBack = CreateButton(cvsGO.transform, "뒤로", OnBack);
            Anchor(btnBack.GetComponent<RectTransform>(), 0.06f, 0.14f, 0.10f, 0.40f);

            // 진행도 초기화(개발용) 버튼 원하면 주석 해제
            // var btnReset = CreateButton(cvsGO.transform, "진행도 초기화", () => { ProgressStore.ResetAll(); RefreshTiles(); });
            // Anchor(btnReset.GetComponent<RectTransform>(), 0.06f, 0.14f, 0.60f, 0.90f);
        }

        // ---------- UI 생성 헬퍼 ----------
        TMP_Text CreateTMP(Transform parent, string text, float size, TextAlignmentOptions align, bool bold=false) {
            var go = new GameObject("TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (uiFont != null) { t.font = uiFont; if (uiFont.material) t.fontSharedMaterial = uiFont.material; }
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = _textNormal;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.enableWordWrapping = true;
            t.raycastTarget = false;
            return t;
        }

        Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick) {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = new Color(1, 1, 1, 0.08f);
            var btn = go.GetComponent<Button>(); if (onClick != null) btn.onClick.AddListener(onClick);

            var txt = CreateTMP(go.transform, label, 28, TextAlignmentOptions.Center, bold:true);
            Anchor(txt.rectTransform, 0f, 1f, 0f, 1f);
            return btn;
        }

        StageTile CreateTile(Transform parent, int stageNumber, UnityEngine.Events.UnityAction onClick) {
            var root = new GameObject($"Stage{stageNumber}", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            root.SetParent(parent, false);

            var img = root.GetComponent<Image>(); img.color = _tileNormal;
            var btn = root.GetComponent<Button>(); btn.onClick.AddListener(onClick);

            var big = CreateTMP(root, stageNumber.ToString(), 46, TextAlignmentOptions.Center, bold:true);
            Anchor(big.rectTransform, 0.54f, 0.82f, 0.10f, 0.90f);

            var sub = CreateTMP(root, $"레벨 {stageNumber}", 20, TextAlignmentOptions.Center, bold:false);
            sub.color = new Color(1,1,1,0.8f);
            Anchor(sub.rectTransform, 0.32f, 0.50f, 0.10f, 0.90f);

            // 상태 아이콘(클리어 체크)
            var check = new GameObject("Cleared", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            check.transform.SetParent(root, false);
            if (uiFont != null) { check.font = uiFont; if (uiFont.material) check.fontSharedMaterial = uiFont.material; }
            check.text = "✓";
            check.fontSize = 30;
            check.alignment = TextAlignmentOptions.BottomRight;
            check.color = new Color(1f, 1f, 1f, 0.9f);
            Anchor(check.rectTransform, 0.00f, 0.30f, 0.70f, 0.98f);

            return new StageTile { root = root, image = img, button = btn, num = big, sub = sub, cleared = check };
        }

        void Anchor(RectTransform rt, float yMin, float yMax, float xMin = 0.1f, float xMax = 0.9f) {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        void Place(RectTransform rt, float xMin, float xMax, float yMin, float yMax) {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        // ---------- 상태 반영 ----------
        void RefreshTiles() {
            int maxCleared = ProgressStore.GetMaxCleared(_campaign);
            for (int i = 0; i < 8; i++) {
                int stage = i + 1;
                bool unlocked = ProgressStore.IsUnlocked(_campaign, stage);
                bool cleared  = stage <= maxCleared;

                var t = _tiles[i];
                t.button.interactable = unlocked;
                t.image.color = cleared ? _tileCleared : (unlocked ? _tileNormal : _tileLocked);
                t.num.color = unlocked ? _textNormal : _textLocked;
                t.sub.color = unlocked ? new Color(1,1,1,0.8f) : _textLocked;
                t.cleared.enabled = cleared;
            }
        }

        // ---------- 동작 ----------
        void OnPick(int stage) {
            bool unlocked = ProgressStore.IsUnlocked(_campaign, stage);
            if (!unlocked) {
                ServiceHub.I.Caption.ShowTop("잠금된 레벨입니다. 이전 레벨을 먼저 완료하세요.");
                return;
            }

            GameRouter.SelectStage(stage);
            ServiceHub.I.Caption.ShowTop($"{_campaign} - 레벨 {stage}");

            StartCoroutine(GoPlay());
        }

        IEnumerator GoPlay() {
            if (ServiceHub.I != null && ServiceHub.I.Fader != null) {
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

        // ---------- 내부 타입 ----------
        class StageTile {
            public RectTransform root;
            public Image image;
            public Button button;
            public TMP_Text num;
            public TMP_Text sub;
            public TMP_Text cleared;
        }
    }
}