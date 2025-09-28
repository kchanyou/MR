using UnityEngine;

namespace AuralRehab.Application {
    /// <summary>
    /// 게임 → 결과 화면으로 요약 데이터를 넘기는 간단한 버스
    /// </summary>
    public static class GameResultBus {
        public class Summary {
            public CampaignId campaign;
            public int stage;              // 1..8
            public int totalTrials;
            public int correct;
            public float avgReaction;      // sec
            public bool success;           // 클리어 여부(정확도 기준)
        }

        public static Summary Last { get; private set; }
        public static void Set(Summary s) => Last = s;
        public static void Clear() => Last = null;
    }
}