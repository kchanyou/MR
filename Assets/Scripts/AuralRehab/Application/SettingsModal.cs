using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.Application {
    /// <summary>
    /// 햄버거 버튼으로 열리는 설정 모달(뒤로가기/재시도/계속)
    /// - 씬에 배치하고 인스펙터로 레퍼런스를 연결
    /// - CanvasGroup 페이드, Raycast 차단
    /// </summary>
    public class SettingsModal : MonoBehaviour {
        [Header("Refs (Assign in Inspector)")]
        [SerializeField] Canvas canvas;
        [SerializeField] CanvasGroup group;
        [SerializeField] Button btnResume;
        [SerializeField] Button btnRetry;
        [SerializeField] Button btnExit;
        [SerializeField] TMP_Text titleText;

        [Header("Visual")]
        [SerializeField] float fadeDuration = 0.15f;
        [SerializeField] int sortingOrder = 30500; // Caption(30000) 위, Fader(32000) 아래

        System.Action _onResume, _onRetry, _onExit;
        Coroutine _co;

        void Reset() {
            canvas = GetComponent<Canvas>();
            group  = GetComponent<CanvasGroup>();
        }

        void Awake() {
            if (canvas) { canvas.overrideSorting = true; canvas.sortingOrder = sortingOrder; }
            if (group)  { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }

            if (btnResume) btnResume.onClick.AddListener(() => _onResume?.Invoke());
            if (btnRetry)  btnRetry.onClick.AddListener(()  => _onRetry?.Invoke());
            if (btnExit)   btnExit.onClick.AddListener(()   => _onExit?.Invoke());
        }

        public void Setup(System.Action onResume, System.Action onRetry, System.Action onExit) {
            _onResume = onResume; _onRetry = onRetry; _onExit = onExit;
        }

        public void Open() {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(FadeTo(1f, true));
        }

        public void Close() {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(FadeTo(0f, false));
        }

        IEnumerator FadeTo(float target, bool interact) {
            if (!group) yield break;
            float start = group.alpha;
            float t = 0f;
            group.blocksRaycasts = true; // 즉시 차단
            while (t < fadeDuration) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeDuration);
                k = k * k * (3f - 2f * k);
                group.alpha = Mathf.Lerp(start, target, k);
                yield return null;
            }
            group.alpha = target;
            group.interactable = interact;
            group.blocksRaycasts = interact;
        }
    }
}