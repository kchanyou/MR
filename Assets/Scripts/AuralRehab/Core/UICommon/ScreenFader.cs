using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AuralRehab.Core.UICommon {
    /// <summary>
    /// 화면 전환 페이더(캔버스 오버레이).
    /// - 첫 생성: startOpaqueOnAwake=true면 검정(알파=1)로 시작 → 워밍업 프레임 대기 → 페이드인
    /// - 씬 로드: autoFadeInOnSceneLoaded=true면 페이드인
    /// - 프레임 시간 튐 방지: maxStepSec(기본 0.05s)로 per-frame dt 상한 처리
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-9990)]
    public class ScreenFader : MonoBehaviour {
        [Header("Canvas")]
        [SerializeField] Vector2 referenceResolution = new(1080, 1920);
        [SerializeField] int sortingOrder = 64000; // Caption(32760), TitleOverlay(32000)보다 위

        [Header("Behavior")]
        [SerializeField] bool startOpaqueOnAwake = true;      // 생성 시 알파=1로 시작
        [SerializeField] bool autoFadeInOnAwake = true;       // 첫 씬에서 자동 페이드인
        [SerializeField] int  warmupFramesOnAwake = 2;        // 페이드 시작 전 대기할 프레임 수
        [SerializeField] bool autoFadeInOnSceneLoaded = true; // 이후 씬 로드시 자동 페이드인

        [Header("Fade Defaults")]
        [SerializeField] float defaultDuration = 0.6f;
        [SerializeField] Color defaultColor = Color.black;
        [SerializeField] bool  useUnscaledTime = true;
        [SerializeField] bool  blockRaycastDuringFade = true;
        [SerializeField, Tooltip("프레임 튐 방지: 1프레임에 더해줄 시간의 상한(초)")]
        float maxStepSec = 0.05f; // 20fps 상한

        Canvas _canvas;
        CanvasScaler _scaler;
        GraphicRaycaster _raycaster;
        Image _image;
        bool _isFading;
        Coroutine _co;

        void Awake() {
            BuildOverlayIfNeeded();

            // 생성 직후 알파 설정(요청: 알파=1 시작 권장)
            var c = defaultColor; c.a = startOpaqueOnAwake ? 1f : 0f;
            _image.color = c;
            _image.raycastTarget = startOpaqueOnAwake && blockRaycastDuringFade;

            if (autoFadeInOnAwake) {
                // 초기 프레임 시간 튐 방지를 위해 워밍업 후 페이드인
                if (_co != null) StopCoroutine(_co);
                _co = StartCoroutine(FadeInAfterWarmup(defaultDuration, defaultColor, warmupFramesOnAwake));
            }
        }

        void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene s, LoadSceneMode m) {
            if (autoFadeInOnSceneLoaded) {
                PlayFadeIn(defaultDuration, defaultColor);
            }
        }

        void BuildOverlayIfNeeded() {
            if (_canvas != null && _image != null) return;

            var cvsGO = new GameObject("FaderCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cvsGO.transform.SetParent(transform, false);
            _canvas = cvsGO.GetComponent<Canvas>();
            _scaler = cvsGO.GetComponent<CanvasScaler>();
            _raycaster = cvsGO.GetComponent<GraphicRaycaster>();

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = referenceResolution;

            var imgGO = new GameObject("Image", typeof(RectTransform), typeof(Image));
            imgGO.transform.SetParent(cvsGO.transform, false);
            _image = imgGO.GetComponent<Image>();
            var rt = _image.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            _image.color = new Color(0, 0, 0, 0);
            _image.raycastTarget = false;
        }

        // ----------------- Public API -----------------
        public void PlayFadeIn(float duration = -1f, Color? color = null) {
            BuildOverlayIfNeeded();
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(FadeIn(duration < 0 ? defaultDuration : duration, color ?? defaultColor));
        }

        public void PlayFadeOut(float duration = -1f, Color? color = null) {
            BuildOverlayIfNeeded();
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(FadeOut(duration < 0 ? defaultDuration : duration, color ?? defaultColor));
        }

        public IEnumerator FadeInAfterWarmup(float duration, Color color, int warmupFrames) {
            // 렌더 안정화용 워밍업
            for (int i = 0; i < Mathf.Max(0, warmupFrames); i++) {
                yield return new WaitForEndOfFrame();
            }
            yield return FadeIn(duration, color);
        }

        public IEnumerator FadeIn(float duration, Color color) {
            _isFading = true;
            _image.color = new Color(color.r, color.g, color.b, 1f);
            _image.raycastTarget = blockRaycastDuringFade;

            float t = 0f;
            while (t < duration) {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (dt > maxStepSec) dt = maxStepSec; // 핵심: 프레임 튐 상한
                t += dt;
                float k = 1f - Mathf.Clamp01(t / duration);
                // S-curve 이징
                k = k * k * (3f - 2f * k);
                _image.color = new Color(color.r, color.g, color.b, k);
                yield return null;
            }
            _image.color = new Color(color.r, color.g, color.b, 0f);
            _image.raycastTarget = false;
            _isFading = false;
        }

        public IEnumerator FadeOut(float duration, Color color) {
            _isFading = true;
            _image.color = new Color(color.r, color.g, color.b, 0f);
            _image.raycastTarget = blockRaycastDuringFade;

            float t = 0f;
            while (t < duration) {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                if (dt > maxStepSec) dt = maxStepSec; // 튐 상한
                t += dt;
                float k = Mathf.Clamp01(t / duration);
                k = k * k * (3f - 2f * k);
                _image.color = new Color(color.r, color.g, color.b, k);
                yield return null;
            }
            _image.color = new Color(color.r, color.g, color.b, 1f);
            _image.raycastTarget = true;
            _isFading = false;
        }

        public bool IsFading() => _isFading;

        public void SetReferenceResolution(Vector2 res) {
            referenceResolution = res;
            if (_scaler != null) _scaler.referenceResolution = res;
        }

        public void SetSortingOrder(int order) {
            sortingOrder = order;
            if (_canvas != null) _canvas.sortingOrder = order;
        }

        public void SetDefaults(float duration, Color color) {
            defaultDuration = Mathf.Max(0f, duration);
            defaultColor = color;
        }

        public void SetOptions(bool unscaledTime, bool raycastBlock, float maxStep = 0.05f) {
            useUnscaledTime = unscaledTime;
            blockRaycastDuringFade = raycastBlock;
            maxStepSec = Mathf.Max(0.001f, maxStep);
        }
    }
}