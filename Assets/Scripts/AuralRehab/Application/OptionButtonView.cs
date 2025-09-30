using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace AuralRehab.GamePlay {
    public class OptionButtonView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
        [Header("Refs")]
        [SerializeField] Image background;
        [SerializeField] TMP_Text label;
        [SerializeField] CanvasGroup cg;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip sfxClick;
        [SerializeField] AudioClip sfxCorrect;
        [SerializeField] AudioClip sfxWrong;

        [Header("Colors")]
        [SerializeField] Color colorNormal  = new Color32(0x26,0x2A,0x33,0xFF);
        [SerializeField] Color colorHover   = new Color32(0x33,0x38,0x44,0xFF);
        [SerializeField] Color colorPressed = new Color32(0x20,0x24,0x2B,0xFF);
        [SerializeField] Color colorDisabled= new Color32(0x26,0x2A,0x33,0x88);
        [SerializeField] Color colorCorrect = new Color32(0x1E,0x8E,0x3E,0xFF);
        [SerializeField] Color colorWrong   = new Color32(0xC6,0x28,0x28,0xFF);

        [Header("Animation")]
        [SerializeField, Range(0.9f, 1f)] float pressScale = 0.96f;
        [SerializeField, Range(0.05f,0.25f)] float pressLerp = 0.08f;
        [SerializeField, Range(0.02f,0.35f)] float pulseDur = 0.12f;
        [SerializeField, Range(1f, 1.2f)] float pulseScale = 1.05f;
        [SerializeField] bool useUnscaledTime = true;

        Vector3 _scaleDefault;
        bool _interactable = true;

        void Reset() {
            background = GetComponent<Image>();
            label = GetComponentInChildren<TMP_Text>(true);
            cg = GetComponent<CanvasGroup>();
            if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        }
        void Awake() {
            _scaleDefault = transform.localScale;
            SetStateNormal(true);
        }

        public void SetLabel(string text) { if (label) label.text = text; }

        public void SetInteractable(bool on) {
            _interactable = on;
            if (cg) cg.alpha = on ? 1f : 0.6f;
            if (background) background.color = on ? colorNormal : colorDisabled;
        }

        public void SetStateNormal(bool immediate=false) {
            if (background) background.color = colorNormal;
            if (immediate) transform.localScale = _scaleDefault;
        }

        public void PreviewPulse() {
            if (!gameObject.activeInHierarchy) return;
            StopCoroutine(nameof(CoPulse));
            StartCoroutine(CoPulse());
        }

        public void ShowJudge(bool correct) {
            StopCoroutine(nameof(CoShake));
            if (background) background.color = correct ? colorCorrect : colorWrong;
            if (sfx) sfx.PlayOneShot(correct ? sfxCorrect : sfxWrong, 1f);
            if (!correct) StartCoroutine(CoShake());
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!_interactable) return;
            if (sfx && sfxClick) sfx.PlayOneShot(sfxClick, 1f);
            StopCoroutine(nameof(CoScaleTo));
            StartCoroutine(CoScaleTo(_scaleDefault * pressScale));
            if (background) background.color = colorPressed;
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (!_interactable) return;
            StopCoroutine(nameof(CoScaleTo));
            StartCoroutine(CoScaleTo(_scaleDefault));
            if (background) background.color = colorNormal;
        }

        IEnumerator CoScaleTo(Vector3 target) {
            float t=0;
            while (Vector3.Distance(transform.localScale, target) > 0.001f) {
                t += (useUnscaledTime? Time.unscaledDeltaTime : Time.deltaTime);
                transform.localScale = Vector3.Lerp(transform.localScale, target, pressLerp * 60f * (useUnscaledTime? Time.unscaledDeltaTime : Time.deltaTime));
                yield return null;
            }
            transform.localScale = target;
        }

        IEnumerator CoPulse() {
            float t=0, half=pulseDur;
            // up
            while (t < half) {
                t += (useUnscaledTime? Time.unscaledDeltaTime : Time.deltaTime);
                float k = Mathf.SmoothStep(1f, pulseScale, t/half);
                transform.localScale = _scaleDefault * k;
                yield return null;
            }
            // down
            t = 0;
            while (t < half) {
                t += (useUnscaledTime? Time.unscaledDeltaTime : Time.deltaTime);
                float k = Mathf.SmoothStep(pulseScale, 1f, t/half);
                transform.localScale = _scaleDefault * k;
                yield return null;
            }
            transform.localScale = _scaleDefault;
        }

        IEnumerator CoShake() {
            float dur = 0.18f;
            float amp = 8f;
            float t=0;
            var rt = transform as RectTransform;
            Vector2 basePos = rt ? rt.anchoredPosition : Vector2.zero;
            while (t < dur) {
                t += (useUnscaledTime? Time.unscaledDeltaTime : Time.deltaTime);
                float f = Mathf.Sin(t * 60f) * amp * (1f - t/dur);
                if (rt) rt.anchoredPosition = basePos + new Vector2(f, 0);
                yield return null;
            }
            if (rt) rt.anchoredPosition = basePos;
        }
    }
}