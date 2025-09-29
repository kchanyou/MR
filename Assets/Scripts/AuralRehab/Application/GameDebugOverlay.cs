using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AuralRehab.GamePlay;

namespace AuralRehab.Application {
    /// <summary>
    /// 개발용 디버그 오버레이. F1로 토글.
    /// GameResultBus.Get 의존 제거: GameHostController가 NotifyResult로 결과를 넘겨주면 내부에 보관해 표시.
    /// </summary>
    public class GameDebugOverlay : MonoBehaviour {
        Canvas _canvas;
        Image _panel;
        TMP_Text _text;
        bool _visible = true;

        GameHostController _host;

        // 최근 결과를 내부 저장
        bool _hasSummary = false;
        GameResultBus.Summary _lastSummary;

        public static GameDebugOverlay Create(Transform parent, TMP_FontAsset font) {
            var go = new GameObject("GameDebugOverlay");
            go.transform.SetParent(parent, false);
            var ov = go.AddComponent<GameDebugOverlay>();
            ov.BuildUI(font);
            return ov;
        }

        void BuildUI(TMP_FontAsset font) {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(transform, false);
            _panel = panelGO.AddComponent<Image>();
            _panel.color = new Color(0f, 0f, 0f, 0.45f);
            var prt = _panel.rectTransform;
            prt.anchorMin = new Vector2(0f, 1f);
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 1f);
            prt.anchoredPosition = new Vector2(16, -16);
            prt.sizeDelta = new Vector2(560, 240);

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(panelGO.transform, false);
            _text = textGO.AddComponent<TextMeshProUGUI>();
            if (font) { _text.font = font; if (font.material) _text.fontSharedMaterial = font.material; }
            _text.enableWordWrapping = false;
            _text.raycastTarget = false;
            _text.fontSize = 20;
            var trt = _text.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(12, -12);
            trt.sizeDelta = new Vector2(536, 216);
        }

        public void Bind(GameHostController host) { _host = host; }

        public void ToggleVisible() {
            _visible = !_visible;
            if (_panel) _panel.enabled = _visible;
            if (_text) _text.enabled = _visible;
        }

        public void Tick(GameHostController host, IPausableGame pausable, G1OddOneOutPitch g1, G2MelodyDirection g2, G3BeatJump g3) {
            if (!_visible || _text == null || host == null) return;

            var sb = new StringBuilder(512);
            sb.AppendLine("DEV OVERLAY");
            sb.AppendLine("F1 Overlay  F2 Retry  F3 Next  F4 Prev  F5 Campaign  1~6 ForceMode  P Pause  ` SlowMo");
            sb.AppendLine("—");
            sb.AppendLine("현재 라우팅/라벨은 상단 헤더를 참조하세요.");
            sb.Append("Pause: ").AppendLine(pausable!=null && pausable.IsPaused ? "On" : "Off");

            if (g3 != null && g3.isActiveAndEnabled) {
                sb.AppendLine("[G3] 인스펙터 튜닝");
                sb.AppendLine("  hitWindow / audioAdvance / visualAdvance / judgeTimeOffset / gapAfterPreview");
            } else if (g2 != null && g2.isActiveAndEnabled) {
                sb.AppendLine("[G2] 진행 중");
            } else if (g1 != null && g1.isActiveAndEnabled) {
                sb.AppendLine("[G1] 진행 중");
            }

            if (_hasSummary) {
                var s = _lastSummary;
                sb.AppendLine("—");
                sb.AppendLine($"Result  success={s.success}  acc={(s.totalTrials>0?(s.correct/(float)s.totalTrials):0f):P0}  metric={s.avgReaction:0.000}s");
                sb.AppendLine($"         {s.campaign} Stage{s.stage}  trials={s.totalTrials} correct={s.correct}");
            }

            sb.AppendLine("—");
            sb.Append("TimeScale ").Append(Time.timeScale.ToString("0.##"));

            _text.text = sb.ToString();
        }

        // GameHostController → resultOverlay.Show(...) 직전에 호출됨
        public void NotifyResult(GameResultBus.Summary s) {
            _hasSummary = true;
            _lastSummary = s;
        }
    }
}