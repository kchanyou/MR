using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace AuralRehab.Application {
    /// <summary>
    /// 캐릭터 선택(씬 배치형):
    /// - 카드 UI를 씬에서 직접 배치하고, 아래 필드에 연결
    /// - A/B/C 중 선택 → 시작 버튼 활성화 → 페이드아웃 → StageSelect
    /// </summary>
    public class CharacterSelectController : MonoBehaviour {
        [Header("UI Refs (Assign in Inspector)")]
        [SerializeField] TMP_FontAsset uiFont;      // 선택 사항
        [SerializeField] TMP_Text titleText;

        [SerializeField] CharacterCard cardA;
        [SerializeField] CharacterCard cardB;
        [SerializeField] CharacterCard cardC;

        [SerializeField] Button btnStart;
        [SerializeField] Button btnBack;

        [Header("Flow")]
        [SerializeField] string nextScene = Scenes.StageSelect;
        [SerializeField] float fadeOutDuration = 0.35f;

        [Header("Colors")]
        [SerializeField] Color cardNormal   = new Color(1, 1, 1, 0.08f);
        [SerializeField] Color cardSelected = new Color(0.15f, 0.5f, 1f, 0.35f);
        [SerializeField] Color textNormal   = Color.white;
        [SerializeField] Color textDim      = new Color(1, 1, 1, 0.6f);

        CampaignId? _selected = null;

        void Awake() {
            EnsureMainCamera();
            EnsureServiceHub();

            // 폰트 적용(선택)
            ApplyFontIfSet(titleText);
            ApplyCardFont(cardA);
            ApplyCardFont(cardB);
            ApplyCardFont(cardC);

            // 클릭 이벤트
            if (cardA.button) cardA.button.onClick.AddListener(() => Pick(CampaignId.A));
            if (cardB.button) cardB.button.onClick.AddListener(() => Pick(CampaignId.B));
            if (cardC.button) cardC.button.onClick.AddListener(() => Pick(CampaignId.C));
            if (btnStart) btnStart.onClick.AddListener(OnStart);
            if (btnBack)  btnBack.onClick.AddListener(OnBackToLogin);

            RefreshInteractivity();
            ServiceHub.I.Caption.ShowTop("캐릭터를 선택하세요.");
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

        void ApplyCardFont(CharacterCard c) {
            if (c.title)   ApplyFontIfSet(c.title);
            if (c.subtitle) ApplyFontIfSet(c.subtitle);
        }

        void Pick(CampaignId id) {
            _selected = id;
            GameRouter.SelectCampaign(id);

            SetSelected(cardA, id == CampaignId.A);
            SetSelected(cardB, id == CampaignId.B);
            SetSelected(cardC, id == CampaignId.C);

            ServiceHub.I.Caption.ShowTop($"{id} 캐릭터를 선택했습니다.");
            RefreshInteractivity();
        }

        void SetSelected(CharacterCard c, bool on) {
            if (c.background) c.background.color = on ? cardSelected : cardNormal;
            if (c.title)   c.title.color   = textNormal;
            if (c.subtitle) c.subtitle.color = on ? textNormal : textDim;
        }

        void RefreshInteractivity() {
            if (btnStart) btnStart.interactable = _selected.HasValue;
        }

        void OnStart() {
            if (!_selected.HasValue) return;
            StartCoroutine(GoNext());
        }

        IEnumerator GoNext() {
            if (ServiceHub.I?.Fader != null) {
                ServiceHub.I.Fader.PlayFadeOut(fadeOutDuration, Color.black);
                yield return new WaitForSecondsRealtime(fadeOutDuration * 0.95f);
            }
            try { SceneManager.LoadScene(nextScene); }
            catch { Debug.Log($"다음 씬 {nextScene} 이(가) 빌드에 없습니다."); }
        }

        void OnBackToLogin() {
            try { SceneManager.LoadScene(Scenes.Login); }
            catch { Debug.Log($"씬 {Scenes.Login} 이(가) 빌드에 없습니다."); }
        }

        [System.Serializable]
        public class CharacterCard {
            public Button button;          // 카드 클릭 영역(버튼)
            public Image  background;      // 카드 배경 이미지
            public TMP_Text title;         // 상단 타이틀
            public TMP_Text subtitle;      // 하단 설명
        }
    }
}