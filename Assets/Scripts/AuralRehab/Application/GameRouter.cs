using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuralRehab.Application {
    public static class GameRouter {
        public static CampaignId SelectedCampaign { get; private set; } = CampaignId.A;
        public static int SelectedStage { get; private set; } = 1;

        public static void SelectCampaign(CampaignId id) { SelectedCampaign = id; }
        public static void SelectStage(int stage1to8) { SelectedStage = Mathf.Clamp(stage1to8, 1, 8); }

        public static void Go(string scene) { SceneManager.LoadScene(scene); }
        public static void GoPlay() { SceneManager.LoadScene(Scenes.Game); }   // 여기 수정
        public static void GoResult() { SceneManager.LoadScene(Scenes.Result); }
    }
}