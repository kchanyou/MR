using UnityEngine.SceneManagement;

namespace AuralRehab.Application {
    public static class Scenes {
        public const string Title           = "00_Title";
        public const string Login           = "01_Login";
        public const string CharacterSelect = "02_CharacterSelect";
        public const string StageSelect     = "03_StageSelect";
        public const string Game            = "10_Game";
        public const string Result          = "11_Result";

        // 별칭(기존 코드 호환용)
        public const string PlayHost        = Game;

        public static bool IsInBuild(string sceneName) {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName) return true;
            }
            return false;
        }
    }
}