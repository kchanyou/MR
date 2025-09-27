using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using AuralRehab.Core.Data;

namespace AuralRehab.Application {
    /// <summary>
    /// 로그인 스텁(씬 배치형):
    /// - 씬에 배치된 TMP, 버튼, 인풋을 인스펙터로 연결
    /// - 프로필을 로컬 JSON으로 저장 후 다음 씬 이동
    /// - ServiceHub.Fader로 페이드아웃
    /// </summary>
    public class LoginController : MonoBehaviour {
        [Header("UI Refs (Assign in Inspector)")]
        [SerializeField] TMP_FontAsset uiFont;              // 선택 사항(전역 폰트 주입)
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_InputField inputNickname;
        [SerializeField] TMP_Text messageText;
        [SerializeField] Button btnStartWithName;
        [SerializeField] Button btnStartGuest;
        [SerializeField] Button btnGoogle;                  // 미구현이면 비워도 됨
        [SerializeField] Button btnApple;                   // 미구현이면 비워도 됨

        [Header("Flow")]
        [SerializeField] string nextScene = Scenes.CharacterSelect;
        [SerializeField] float fadeOutDuration = 0.35f;

        void Awake() {
            EnsureMainCamera();
            EnsureServiceHub();

            // 폰트 적용(선택): 연결된 TMP에만 적용
            ApplyFontIfSet(titleText);
            if (messageText) ApplyFontIfSet(messageText);
            if (inputNickname) {
                ApplyFontIfSet(inputNickname.textComponent);
                if (inputNickname.placeholder is TMP_Text ph) ApplyFontIfSet(ph);
            }

            // 이벤트 연결
            if (btnStartWithName) btnStartWithName.onClick.AddListener(OnStartWithName);
            if (btnStartGuest)    btnStartGuest.onClick.AddListener(OnStartGuest);

            ServiceHub.I.Caption.ShowTop("로그인 방식을 선택하세요.");
        }

        void EnsureMainCamera() {
            if (Camera.main == null) {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
        }

        void EnsureServiceHub() {
            if (ServiceHub.I == null) {
                var go = new GameObject("ServiceHub");
                go.AddComponent<ServiceHub>();
            }
            if (uiFont) {
                ServiceHub.I.Caption.SetFont(uiFont);
                var tmp = ServiceHub.I.Caption.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp && uiFont.material) tmp.fontSharedMaterial = uiFont.material;
            }
        }

        void ApplyFontIfSet(TMP_Text t) {
            if (!t || !uiFont) return;
            t.font = uiFont;
            if (uiFont.material) t.fontSharedMaterial = uiFont.material;
        }

        void OnStartGuest() {
            StartCoroutine(SaveAndGo("게스트"));
        }

        void OnStartWithName() {
            var name = inputNickname ? inputNickname.text.Trim() : "";
            if (string.IsNullOrEmpty(name)) {
                if (messageText) messageText.text = "닉네임을 입력하세요.";
                ServiceHub.I.Caption.ShowTop("닉네임을 입력하세요.");
                return;
            }
            StartCoroutine(SaveAndGo(name));
        }

        IEnumerator SaveAndGo(string name) {
            if (btnStartGuest) btnStartGuest.interactable = false;
            if (btnStartWithName) btnStartWithName.interactable = false;

            var profile = ProfileData.Create(name);
            string path = Path.Combine(UnityEngine.Application.persistentDataPath, "profile.json");
            bool ok = LocalJson.TrySave(path, profile, pretty: true);
            if (messageText) messageText.text = ok ? "저장 완료" : "저장 실패";

            if (ServiceHub.I?.Fader != null) {
                ServiceHub.I.Fader.PlayFadeOut(fadeOutDuration, Color.black);
                yield return new WaitForSecondsRealtime(fadeOutDuration * 0.95f);
            }
            try { SceneManager.LoadScene(nextScene); }
            catch { Debug.Log($"다음 씬 {nextScene} 이(가) 빌드에 없습니다."); }
        }
    }
}