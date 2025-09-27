using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.Core.UICommon {
    /// <summary>
    /// 항상 켜진 자막 관리자 (TMP 전용)
    /// - 기본 위치: 화면 상단에서 살짝 떨어진 영역
    /// - 배경 패널(블러/반투명) 기본 비활성화
    /// - 호출 시마다 위치를 재설정할 수 있는 오버로드 제공
    /// - 타이핑 완료 후 지정 시간(기본 0.5초) 경과 시 자동으로 숨김
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [DefaultExecutionOrder(-9000)]
    public class CaptionManager : MonoBehaviour {
        Canvas _canvas; CanvasScaler _scaler; GraphicRaycaster _raycaster;
        RectTransform _panel;
        TextMeshProUGUI _tmp;
        Material _blurMat;
        CanvasGroup _group;

        // ===== Typing =====
        [Header("Typing")]
        [SerializeField, Range(5, 30)] int charsPerSec = 14;

        // ===== Auto Hide =====
        [Header("Auto Hide")]
        [SerializeField] bool autoHide = true;
        [SerializeField, Min(0f)] float autoHideDelay = 1.5f; // 타이핑 완료 후 대기 시간
        [SerializeField] bool fadeOutOnHide = false;
        [SerializeField, Min(0f)] float fadeOutDuration = 0.2f;

        // ===== Layout =====
        [Header("Layout")]
        [SerializeField] Vector2 referenceResolution = new(1080, 1920);
        [SerializeField] int sortingOrder = 32760;

        // 기본 위치(정규화): 상단에서 살짝 떨어진 영역
        [SerializeField, Range(0f, 1f)] float defaultYMin = 0.80f;
        [SerializeField, Range(0f, 1f)] float defaultYMax = 0.95f;
        [SerializeField, Range(0f, 0.25f)] float defaultXPadding = 0.05f;
        [SerializeField, Range(0f, 0.20f)] float defaultInnerYPadding = 0.05f;

        // ===== Appearance =====
        [Header("Appearance")]
        [SerializeField] bool enableBackground = false;   // 기본: 배경 미사용
        [SerializeField] bool enableBlur = false;         // 배경을 쓸 때만 의미 있음
        [SerializeField, Range(0, 1)] float panelOpacity = 0.6f;
        [SerializeField, Range(0.5f, 4f)] float blurSize = 1.2f;
        [SerializeField] TMP_FontAsset fontAsset; // PretendardVariable SDF 권장

        Coroutine _typingCo;
        Coroutine _autoHideCo;
        Coroutine _fadeCo;

        void Awake() {
            _canvas = GetComponent<Canvas>();
            _scaler = GetComponent<CanvasScaler>();
            _raycaster = GetComponent<GraphicRaycaster>();

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = referenceResolution;

            BuildUIFresh();
            ApplyPanelRect(defaultYMin, defaultYMax, defaultXPadding, defaultInnerYPadding);
            HideImmediate(); // 초기엔 숨김
        }

        void OnDestroy() { StopAllCoroutines(); }

        // -------------------- UI Build --------------------
        void BuildUIFresh() {
            // 자식 정리
            for (int i = transform.childCount - 1; i >= 0; --i) {
                var ch = transform.GetChild(i);
                if (ch) DestroyImmediate(ch.gameObject);
            }

            // 패널(레이아웃 루트)
            var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panelGO.transform.SetParent(transform, false);
            _panel = panelGO.GetComponent<RectTransform>();
            var img = panelGO.GetComponent<Image>();
            _group = panelGO.GetComponent<CanvasGroup>();
            _group.alpha = 0f; // 시작은 숨김
            _group.interactable = false;
            _group.blocksRaycasts = false;
            ApplyPanelAppearance(img);

            // TMP 텍스트
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(panelGO.transform, false);
            _tmp = textGO.GetComponent<TextMeshProUGUI>();
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.enableWordWrapping = true;
            _tmp.fontSize = 34;
            _tmp.color = Color.white;
            _tmp.raycastTarget = false;

            // 폰트 적용(런타임 동적 로드 대비)
            if (fontAsset != null) {
                _tmp.font = fontAsset;
                if (fontAsset.material != null) _tmp.fontSharedMaterial = fontAsset.material;
            } else {
                // TMP Essential Resources 폴백
                var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (fallback) _tmp.font = fallback;
            }

            // 내부 패딩(패널 내부에서 상하 여백)
            var tr = _tmp.rectTransform;
            tr.anchorMin = new Vector2(0.05f, 0.15f);
            tr.anchorMax = new Vector2(0.95f, 0.85f);
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        }

        void ApplyPanelAppearance(Image img) {
            if (!enableBackground) {
                img.enabled = false;     // 배경 미사용
                img.material = null;
                img.color = new Color(0, 0, 0, 0);
                return;
            }
            img.enabled = true;
            if (enableBlur) {
                var sh = Shader.Find("AuralRehab/UI/BlurPanel");
                if (sh != null) {
                    _blurMat ??= new Material(sh);
                    _blurMat.SetColor("_Color", new Color(1, 1, 1, panelOpacity));
                    _blurMat.SetFloat("_BlurSize", blurSize);
                    img.material = _blurMat;
                    img.color = Color.white; // 머티리얼 알파 사용
                    return;
                }
            }
            img.material = null;
            img.color = new Color(0, 0, 0, panelOpacity); // 단순 반투명
        }

        void ApplyPanelRect(float yMin, float yMax, float xPad, float innerYPad) {
            yMin = Mathf.Clamp01(yMin);
            yMax = Mathf.Clamp01(yMax);
            if (yMax < yMin) { var t = yMin; yMin = yMax; yMax = t; }
            xPad = Mathf.Clamp01(xPad);
            innerYPad = Mathf.Clamp01(innerYPad);

            var minX = Mathf.Clamp01(xPad);
            var maxX = 1f - Mathf.Clamp01(xPad);
            var rt = _panel;

            rt.anchorMin = new Vector2(minX, yMin);
            rt.anchorMax = new Vector2(maxX, yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // 내부 텍스트 패딩 갱신
            if (_tmp != null) {
                var tr = _tmp.rectTransform;
                tr.anchorMin = new Vector2(0.05f, innerYPad);
                tr.anchorMax = new Vector2(0.95f, 1f - innerYPad);
                tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            }
        }

        // -------------------- Public API --------------------
        public void Show(string msg) {
            if (_tmp == null) BuildUIFresh();
            ApplyPanelRect(defaultYMin, defaultYMax, defaultXPadding, defaultInnerYPadding);
            TypeOutStart(msg);
        }

        /// <summary>
        /// 호출 시마다 위치를 조절하는 오버로드.
        /// yMin/yMax/xPadding은 0..1 정규화 값(스크린 비율 기준).
        /// </summary>
        public void Show(string msg, float yMin, float yMax, float xPadding = -1f, float innerYPad = -1f) {
            if (_tmp == null) BuildUIFresh();
            var xp = (xPadding < 0f) ? defaultXPadding : xPadding;
            var ip = (innerYPad < 0f) ? defaultInnerYPadding : innerYPad;
            ApplyPanelRect(yMin, yMax, xp, ip);
            TypeOutStart(msg);
        }

        public void ShowTop(string msg)    => Show(msg, 0.80f, 0.95f);
        public void ShowCenter(string msg) => Show(msg, 0.45f, 0.60f);
        public void ShowBottom(string msg) => Show(msg, 0.05f, 0.20f);

        public void SetSpeed(int cps) { charsPerSec = Mathf.Clamp(cps, 5, 30); }

        public void SetFont(TMP_FontAsset fa) {
            fontAsset = fa;
            if (_tmp != null) {
                _tmp.font = fa;
                if (fa != null && fa.material != null) _tmp.fontSharedMaterial = fa.material;
            }
        }

        /// <summary>배경 사용 여부 토글(Blur 설정과 무관하게 우선)</summary>
        public void SetBackgroundEnabled(bool on) {
            enableBackground = on;
            if (_panel != null) {
                var img = _panel.GetComponent<Image>();
                if (img != null) ApplyPanelAppearance(img);
            }
        }

        /// <summary>배경 파라미터(배경이 켜져 있을 때만 시각적 효과 적용)</summary>
        public void SetAppearance(bool blur, float opacity, float blurSz) {
            enableBlur = blur;
            panelOpacity = Mathf.Clamp01(opacity);
            blurSize = blurSz;
            if (_panel != null) {
                var img = _panel.GetComponent<Image>();
                if (img != null) ApplyPanelAppearance(img);
            }
        }

        /// <summary>기본 위치(Show()에서 사용되는 영역)를 재설정</summary>
        public void SetDefaultPlacement(float yMin, float yMax, float xPadding = 0.05f, float innerYPad = 0.05f) {
            defaultYMin = Mathf.Clamp01(yMin);
            defaultYMax = Mathf.Clamp01(yMax);
            if (defaultYMax < defaultYMin) { var t = defaultYMin; defaultYMin = defaultYMax; defaultYMax = t; }
            defaultXPadding = Mathf.Clamp01(xPadding);
            defaultInnerYPadding = Mathf.Clamp01(innerYPad);
        }

        /// <summary>자동 숨김 설정</summary>
        public void SetAutoHide(bool on, float delaySeconds = -1f, bool fadeOut = false, float fadeSeconds = 0.2f) {
            autoHide = on;
            if (delaySeconds >= 0f) autoHideDelay = delaySeconds;
            fadeOutOnHide = fadeOut;
            fadeOutDuration = Mathf.Max(0f, fadeSeconds);
        }

        // -------------------- Typing / Hide Core --------------------
        void TypeOutStart(string s) {
            // 보이게 설정
            EnsureVisible();

            // 진행 중인 코루틴 정리
            if (_typingCo != null) StopCoroutine(_typingCo);
            if (_autoHideCo != null) StopCoroutine(_autoHideCo);
            if (_fadeCo != null) StopCoroutine(_fadeCo);

            _typingCo = StartCoroutine(TypeOutAndAutoHide(s));
        }

        IEnumerator TypeOutAndAutoHide(string s) {
            _tmp.text = "";
            float dt = 1f / Mathf.Max(1, charsPerSec);
            foreach (var ch in s) {
                _tmp.text += ch;
                yield return new WaitForSecondsRealtime(dt);
            }
            _typingCo = null;

            if (autoHide) {
                _autoHideCo = StartCoroutine(AutoHideAfterDelay());
            }
        }

        IEnumerator AutoHideAfterDelay() {
            yield return new WaitForSecondsRealtime(autoHideDelay);
            if (fadeOutOnHide && fadeOutDuration > 0f) {
                yield return FadeOutGroup(fadeOutDuration);
            } else {
                HideImmediate();
            }
            _autoHideCo = null;
        }

        void EnsureVisible() {
            if (_group == null) return;
            _group.alpha = 1f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        void HideImmediate() {
            if (_group == null) return;
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        IEnumerator FadeOutGroup(float duration) {
            if (_group == null) yield break;
            float t = 0f; float start = _group.alpha;
            while (t < duration) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                // S-curve
                k = k * k * (3f - 2f * k);
                _group.alpha = Mathf.Lerp(start, 0f, k);
                yield return null;
            }
            _group.alpha = 0f;
        }
    }
}