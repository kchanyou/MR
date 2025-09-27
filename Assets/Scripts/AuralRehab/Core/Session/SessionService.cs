using System;
using System.Collections.Generic;
using AuralRehab.Core;
using AuralRehab.Application;

namespace AuralRehab.Core.Session {
    [Serializable]
    public class StageState {
        public bool cleared;
        public int bestScore; // 필요 시 사용
        public long lastClearedAt;
    }

    [Serializable]
    public class CampaignState {
        public string campaignId = "A";
        public StageState[] stages = new StageState[8]; // 1..8
        public CampaignState() {
            for (int i = 0; i < stages.Length; i++) stages[i] = new StageState();
            // 기본: 스테이지1만 해제
            stages[0].cleared = false;
        }
        public bool IsUnlocked(int stageIndex) {
            if (stageIndex == 0) return true;
            return stages[stageIndex - 1].cleared;
        }
    }

    [Serializable]
    public class PlayerData {
        public string userId = "local";
        public Dictionary<string, CampaignState> campaigns = new Dictionary<string, CampaignState>{
            { "A", new CampaignState{campaignId="A"} },
            { "B", new CampaignState{campaignId="B"} },
            { "C", new CampaignState{campaignId="C"} },
        };
        public List<string> items = new List<string>(); // 업적으로 획득한 아이템
    }

    public class SessionService {
        const string SaveFile = "player.json";
        public PlayerData Data { get; private set; }

        public SessionService() {
            if (!JsonStorage.TryLoad(SaveFile, out PlayerData pd))
                pd = new PlayerData();
            Data = pd;
            Save();
        }

        public void Save() => JsonStorage.Save(SaveFile, Data);

        public CampaignState GetCampaign(CampaignId id) =>
            Data.campaigns[id.ToString()];

        public bool IsStageUnlocked(CampaignId id, int stage1to8) {
            var c = GetCampaign(id);
            int idx = stage1to8 - 1;
            return c.IsUnlocked(idx);
        }

        public void MarkStageCleared(CampaignId id, int stage1to8, int score = 0) {
            int idx = stage1to8 - 1;
            var c = GetCampaign(id);
            var s = c.stages[idx];
            s.cleared = true;
            if (score > s.bestScore) s.bestScore = score;
            s.lastClearedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Save();
        }

        // 테스트/디버그용
        public void ResetProgress() {
            Data = new PlayerData();
            Save();
        }
    }
}