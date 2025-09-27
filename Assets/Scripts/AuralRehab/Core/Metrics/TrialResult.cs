using System;
using AuralRehab.Application;

namespace AuralRehab.Core.Metrics {
    [Serializable]
    public class TrialResult {
        public string userId = "local";
        public string gameId;            // "G1".."G6"
        public string campaignId;        // "A","B","C"
        public int stage;                // 1..8
        public int trialIndex;
        public bool isCorrect;
        public float accuracy;           // 0..1
        public float rtMs;
        public long unixMs;
    }
}
