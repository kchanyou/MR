using UnityEngine;
using TMPro;

// 내부 서비스
using AuralRehab.Core.Session;
using AuralRehab.Core.Metrics;
using AuralRehab.Core.Achievements;
// UI
using AuralRehab.Core.UICommon;

namespace AuralRehab.Application {
    /// <summary>
    /// 앱 전역 서비스 허브. DDOL.
    /// - CaptionManager(TMP)와 ScreenFader를 런타임에 구성
    /// - Caption은 기본 상단, 배경 없음
    /// - Fader는 첫 생성 시 및 씬 로드마다 페이드인
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class ServiceHub : MonoBehaviour {
        public static ServiceHub I { get; private set; }

        // Core services
        public SessionService    Session      { get; private set; }
        public MetricsStore      Metrics      { get; private set; }
        public AchievementSvc    Achievements { get; private set; }

        // UI services
        public CaptionManager    Caption      { get; private set; }
        public ScreenFader       Fader        { get; private set; }

        [Header("Caption: Font Injection")]
        [SerializeField] TMP_FontAsset captionFont;
        [SerializeField] string captionFontResourcePath = "";

        [Header("Caption: Appearance")]
        [SerializeField] bool   captionBlur      = true;
        [SerializeField, Range(0f,1f)] float captionOpacity  = 0.6f;
        [SerializeField, Range(0.5f,4f)] float captionBlurSz = 1.2f;

        [Header("Fader: Defaults")]
        [SerializeField] float faderDefaultDuration = 0.6f;
        [SerializeField] Color faderDefaultColor = Color.black;

        void Awake() {
            // 싱글톤
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            // 코어 서비스
            Session      = new SessionService();
            Metrics      = new MetricsStore();
            Achievements = new AchievementSvc(Session);

            // 레거시 캡션 정리
            CleanupLegacyCaptionChildren();

            // CaptionManager 생성
            var capGO = new GameObject("Caption");
            capGO.transform.SetParent(transform, false);
            Caption = capGO.AddComponent<CaptionManager>();
            Caption.SetBackgroundEnabled(false); // 기본 배경 제거

            // 폰트 로딩(직참조 우선 → Resources 폴백)
            var fa = captionFont;
            if (fa == null && !string.IsNullOrEmpty(captionFontResourcePath)) {
                fa = Resources.Load<TMP_FontAsset>(captionFontResourcePath);
            }
            if (fa != null) {
                Caption.SetFont(fa);
                var tmp = Caption.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (tmp != null && fa.material != null) tmp.fontSharedMaterial = fa.material;
            }
            Caption.SetAppearance(captionBlur, captionOpacity, captionBlurSz);

            // ScreenFader 생성
            var faderGO = new GameObject("ScreenFader");
            faderGO.transform.SetParent(transform, false);
            Fader = faderGO.AddComponent<ScreenFader>();
            Fader.SetDefaults(faderDefaultDuration, faderDefaultColor);

            // 중요: 첫 씬에서도 확실히 페이드인 실행
            Fader.PlayFadeIn(faderDefaultDuration, faderDefaultColor);
        }

        void CleanupLegacyCaptionChildren() {
            for (int i = transform.childCount - 1; i >= 0; --i) {
                var ch = transform.GetChild(i);
                if (ch == null) continue;

                if (ch.name.Contains("CaptionCanvas")) {
                    Destroy(ch.gameObject);
                    continue;
                }
                var legacy = ch.GetComponentInChildren<UnityEngine.UI.Text>(true);
                var tmp    = ch.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (legacy != null && tmp == null) {
                    Destroy(ch.gameObject);
                }
            }
        }
    }
}