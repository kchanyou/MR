using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AuralRehab.Core.UICommon {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class CaptionManager : MonoBehaviour {
        Canvas _canvas; CanvasScaler _scaler; GraphicRaycaster _raycaster;
        RectTransform _panel; Text _text;

        [SerializeField, Range(5,30)] int charsPerSec = 14;
        [SerializeField] Vector2 referenceResolution = new(1080,1920);
        [SerializeField] int sortingOrder = 32760;

        void Awake() {
            // 필수 컴포넌트 확보(RequireComponent로 이미 존재)
            _canvas = GetComponent<Canvas>();
            _scaler = GetComponent<CanvasScaler>();
            _raycaster = GetComponent<GraphicRaycaster>();

            // 캔버스/스케일러 설정
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = referenceResolution;

            BuildUIFresh();
            Show(""); // 초기화
        }

        void OnDestroy() { StopAllCoroutines(); }

        void BuildUIFresh() {
            // 기존 자식 정리
            for (int i = transform.childCount - 1; i >= 0; i--) {
                var ch = transform.GetChild(i);
                if (ch) DestroyImmediate(ch.gameObject);
            }

            // Panel
            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(transform, false);
            _panel = panelGO.GetComponent<RectTransform>();
            var img = panelGO.GetComponent<Image>();
            img.color = new Color(0,0,0,0.6f);
            _panel.anchorMin = new Vector2(0.05f, 0.05f);
            _panel.anchorMax = new Vector2(0.95f, 0.20f);
            _panel.offsetMin = Vector2.zero; _panel.offsetMax = Vector2.zero;

            // Text
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(panelGO.transform, false);
            _text = textGO.GetComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // 중요: 내장 폰트 교체
            _text.alignment = TextAnchor.MiddleCenter;
            _text.fontSize = 34;
            _text.color = Color.white;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Truncate;

            var tr = _text.rectTransform;
            tr.anchorMin = new Vector2(0.05f, 0.15f);
            tr.anchorMax = new Vector2(0.95f, 0.85f);
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        }

        public void Show(string msg) {
            if (_text == null) BuildUIFresh(); // 도중 파괴 대비
            StopAllCoroutines();
            StartCoroutine(TypeOut(msg));
        }

        IEnumerator TypeOut(string s) {
            if (_text == null) yield break;
            _text.text = "";
            float dt = 1f / Mathf.Max(1, charsPerSec);
            foreach (var ch in s) {
                _text.text += ch;
                yield return new WaitForSecondsRealtime(dt);
            }
        }

        public void SetSpeed(int cps) { charsPerSec = Mathf.Clamp(cps, 5, 30); }
    }
}