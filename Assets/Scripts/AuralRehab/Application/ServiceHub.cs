using UnityEngine;
using AuralRehab.Core.Session;
using AuralRehab.Core.Metrics;
using AuralRehab.Core.Achievements;
using AuralRehab.Core.UICommon;

namespace AuralRehab.Application {
    public class ServiceHub : MonoBehaviour {
        public static ServiceHub I { get; private set; }

        // 필수 서비스 인스턴스
        public SessionService Session { get; private set; }
        public MetricsStore Metrics { get; private set; }
        public AchievementSvc Achievements { get; private set; }
        public CaptionManager Caption { get; private set; }

        [SerializeField] bool createCaptionCanvasAtRuntime = true;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);

            Session = new SessionService();
            Metrics = new MetricsStore();
            Achievements = new AchievementSvc(Session);

            if (createCaptionCanvasAtRuntime && Caption == null)
            {
                var go = new GameObject("Caption");      // 빈 GO
                go.transform.SetParent(transform, false);
                Caption = go.AddComponent<AuralRehab.Core.UICommon.CaptionManager>(); // 나머지는 내부에서 구성
            }
        }
    }
}