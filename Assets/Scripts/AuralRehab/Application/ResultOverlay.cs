using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AuralRehab.Application {
    /// <summary>
    /// 게임 종료 후 같은 씬에서 띄우는 결과 오버레이.
    /// - CanvasGroup 페이드인
    /// - 버튼: 나가기(스테이지 선택), 다시하기(현재 스테이지 재시작), 다음 스테이지
    /// - Next 버튼은 '클리어+다음 스테이지 존재'일 때만 활성화
    /// - 씬 배치형: 인스펙터에 레퍼런스를 연결하세요.
    /// </summary>
    public class ResultOverlay : MonoBehaviour {
        [Header("Refs (Assign in Inspector)")]
        [SerializeField] Canvas canvas;            // 별도 캔버스를 쓰면 overrideSorting 권장
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] TMP_Text rewardText;
        [SerializeField] Button btnExit;
        [SerializeField] Button btnRetry;
        [SerializeField] Button btnNext;

        [Header("Visual")]
        [SerializeField] float fadeInDuration = 0.25f;

        System.Action _onExit, _onRetry, _onNext;

        void Reset() {
            group = GetComponent<CanvasGroup>();
            canvas = GetComponent<Canvas>();
        }

        void Awake() {
            if (group != null) {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            if (btnExit)  btnExit.onClick.AddListener(() => _onExit?.Invoke());
            if (btnRetry) btnRetry.onClick.AddListener(() => _onRetry?.Invoke());
            if (btnNext)  btnNext.onClick.AddListener(() => _onNext?.Invoke());
        }

        /// <summary>결과 표시 + 버튼 콜백 연결</summary>
        public void Show(GameResultBus.Summary s, System.Action onExit, System.Action onRetry, System.Action onNext) {
            _onExit = onExit; _onRetry = onRetry; _onNext = onNext;

            float acc = (s.totalTrials > 0) ? (s.correct / (float)s.totalTrials) : 0f;
            if (titleText)  titleText.text = $"{s.campaign} • 레벨 {s.stage} {(s.success ? "클리어" : "실패")}";
            if (detailText) detailText.text =
                $"정확도: {(acc*100f):0}%\n" +
                $"반응속도(평균): {s.avgReaction:0.00}초\n" +
                $"정답 {s.correct}/{s.totalTrials}";
            if (rewardText) rewardText.text = s.success ? "업적 달성 보상 획득" : "";

            // Next 버튼 활성 조건: 클리어했고 8레벨 미만
            bool canNext = s.success && s.stage < 8;
            if (btnNext) btnNext.interactable = canNext;

            StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn() {
            if (group == null) yield break;
            group.interactable = true;
            group.blocksRaycasts = true;
            float t = 0f;
            while (t < fadeInDuration) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeInDuration);
                k = k * k * (3f - 2f * k);
                group.alpha = k;
                yield return null;
            }
            group.alpha = 1f;
        }

        /// <summary>필요 시 코드에서 바로 숨기고 싶을 때</summary>
        public void HideImmediate() {
            if (group == null) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}