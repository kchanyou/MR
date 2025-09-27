using UnityEngine;
using AuralRehab.Application;

namespace AuralRehab.Application {
    public class ManagerTest : MonoBehaviour {
        ServiceHub H => ServiceHub.I;
        string log = "";

        void OnGUI() {
            GUI.Label(new Rect(10,10,500,25), "T01: 매니저 테스트");
            if (GUI.Button(new Rect(10,40,180,35), "자막 표시")) {
                H.Caption.Show("테스트 자막입니다. 한 글자씩 출력됩니다.");
                Append("Caption.Show 호출");
            }
            if (GUI.Button(new Rect(200,40,180,35), "자막 속도 ×2")) {
                H.Caption.SetSpeed(24);
                Append("Caption 속도 24cps");
            }

            if (GUI.Button(new Rect(10,90,180,35), "A 스테이지1 클리어")) {
                H.Session.MarkStageCleared(CampaignId.A, 1, score:100);
                var unlocked = H.Achievements.UnlockStageClear(CampaignId.A, 1);
                Append($"A1 클리어 기록. 업적 신규={unlocked}");
            }
            if (GUI.Button(new Rect(200,90,180,35), "A 스테이지2 잠금 확인")) {
                bool unlocked = H.Session.IsStageUnlocked(CampaignId.A, 2);
                Append($"A2 잠금해제 상태={unlocked}");
            }

            if (GUI.Button(new Rect(10,140,180,35), "더미 지표 저장")) {
                H.Metrics.Push(new Core.Metrics.TrialResult{
                    gameId="G1", campaignId="A", stage=1, trialIndex=0,
                    isCorrect=true, accuracy=1f, rtMs=800, unixMs=System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                H.Metrics.Flush();
                Append("metrics.json Flush 완료");
            }

            if (GUI.Button(new Rect(10,190,180,35), "진행도 리셋")) {
                H.Session.ResetProgress();
                Append("player.json 초기화");
            }

            GUI.TextArea(new Rect(10,240,560,180), log);
        }
        void Append(string s){ log = $"{System.DateTime.Now:HH:mm:ss}  {s}\n" + log; }
    }
}