using System.Collections.Generic;
using AuralRehab.Application;
using AuralRehab.Core.Session;

namespace AuralRehab.Core.Achievements {
    public class AchievementSvc {
        readonly SessionService _session;
        public HashSet<string> Unlocked { get; } = new HashSet<string>();

        public AchievementSvc(SessionService session) {
            _session = session;
            // 저장된 아이템을 메모리에 반영
            foreach (var it in _session.Data.items) Unlocked.Add(it);
        }

        public string MakeStageClearItemId(CampaignId id, int stage1to8) =>
            $"ITEM_{id}_STAGE_{stage1to8:D2}";

        public bool UnlockStageClear(CampaignId id, int stage1to8) {
            var itemId = MakeStageClearItemId(id, stage1to8);
            if (Unlocked.Contains(itemId)) return false;
            Unlocked.Add(itemId);
            _session.Data.items.Add(itemId);
            _session.Save();
            return true;
        }
    }
}