using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using AuralRehab.Core.UICommon;

namespace AuralRehab.Application {
    /// <summary>
    /// 타이틀: mp4 재생 + 스킵(터치/키/버튼), 종료 또는 스킵 시 다음 씬 이동.
    /// - ServiceHub 없으면 자동 생성
    /// - Skip 버튼/자막 모두 TMP 사용
    /// - Android에서 StreamingAssets는 캐시로 복사 후 file:// 재생
    /// </summary>
    public class TitleController : MonoBehaviour {
        [Header("Video Source")]
        [Tooltip("연결 시 이 Clip으로 재생, 없으면 StreamingAssets/title.mp4 사용")]
        public VideoClip clip;
        [Tooltip("StreamingAssets 내 파일명")]
        public string streamingAssetsFileName = "title.mp4";

        [Header("Playback")]
        public bool  loop         = false;
        public bool  autoAdvance  = true;   // 재생 종료·스킵 시 다음 씬 이동
        public float canSkipDelay = 1.0f;   // 몇 초 뒤 스킵 가능

        [Header("Next Scene")]
        public string nextScene = Scenes.Login; // 다음 씬(없으면 콘솔 경고만)

        [Header("Skip UI")]
        public bool showSkipButton = true;
        public TMP_FontAsset uiFont;        // PretendardVariable SDF 등

        VideoPlayer vp;
        AudioSource audioSrc;
        Canvas overlay;
        Button skipBtn;
        bool canSkip;

        void Awake() {
            EnsureMainCamera();
            EnsureServiceHubAndFont();
            BuildOverlay();
            BuildVideoPlayer();
            PrepareAndPlay();
        }

        void EnsureMainCamera() {
            if (Camera.main == null) {
                var cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
        }

        void EnsureServiceHubAndFont() {
            if (ServiceHub.I == null) {
                var go = new GameObject("ServiceHub");
                go.AddComponent<ServiceHub>(); // 내부에서 CaptionManager 생성
            }
            // 폰트가 있다면 바로 주입(머티리얼까지 고정)
            if (uiFont != null) {
                ServiceHub.I.Caption.SetFont(uiFont);
                var tmp = ServiceHub.I.Caption.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null && uiFont.material != null)
                    tmp.fontSharedMaterial = uiFont.material;
            }
            // 한 프레임 뒤 자막 노출(폰트 주입 완료 보장)
            StartCoroutine(_ShowCaptionOnNextFrame("화면을 터치하면 건너뜁니다."));
        }

        System.Collections.IEnumerator _ShowCaptionOnNextFrame(string msg) {
            yield return null; // 1 frame 대기
            ServiceHub.I.Caption.Show(msg);
        }

        void BuildOverlay() {
            overlay = new GameObject("TitleOverlay",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.sortingOrder = 32000;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            overlay.transform.SetParent(transform, false);

            if (showSkipButton) {
                var btnGO = new GameObject("SkipButton", typeof(Image), typeof(Button));
                btnGO.transform.SetParent(overlay.transform, false);
                var rt = btnGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.75f, 0.06f);
                rt.anchorMax = new Vector2(0.95f, 0.14f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                var img = btnGO.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0.5f);

                skipBtn = btnGO.GetComponent<Button>();
                skipBtn.interactable = false;
                skipBtn.onClick.AddListener(Skip);

                var txt = new GameObject("Text", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                txt.transform.SetParent(btnGO.transform, false);
                if (uiFont) txt.font = uiFont;
                if (uiFont && uiFont.material) txt.fontSharedMaterial = uiFont.material;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
                txt.text = "건너뛰기";
                txt.enableAutoSizing = true;
                txt.fontSizeMin = 18;
                txt.fontSizeMax = 36;

                var tr = txt.rectTransform;
                tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            }
        }

        void BuildVideoPlayer() {
            var go = new GameObject("VideoPlayer", typeof(VideoPlayer), typeof(AudioSource));
            go.transform.SetParent(transform, false);

            vp = go.GetComponent<VideoPlayer>();
            audioSrc = go.GetComponent<AudioSource>();

            vp.playOnAwake = false;
            vp.isLooping = loop;
            vp.renderMode = VideoRenderMode.CameraNearPlane;
            vp.targetCamera = Camera.main;
            vp.waitForFirstFrame = true;
            vp.skipOnDrop = true;

            vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
            vp.EnableAudioTrack(0, true);
            vp.SetTargetAudioSource(0, audioSrc);

            vp.prepareCompleted += OnPrepared;
            vp.errorReceived += (p, e) => { Debug.LogWarning("Video error: " + e); Skip(); };
            vp.loopPointReached += OnEnded;

            if (clip != null) {
                vp.source = VideoSource.VideoClip;
                vp.clip = clip;
            } else {
                vp.source = VideoSource.Url;
                SetUrlFromStreamingAssets();
            }
        }

        void SetUrlFromStreamingAssets() {
            var saPath = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, streamingAssetsFileName);
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(LoadFromStreamingAssetsAndroid(saPath));
#else
            vp.url = saPath;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        System.Collections.IEnumerator LoadFromStreamingAssetsAndroid(string saPath) {
            using (var req = UnityWebRequest.Get(saPath)) {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) {
                    Debug.LogWarning("Video load failed: " + req.error);
                    Skip(); yield break;
                }
                var bytes = req.downloadHandler.data;
                var local = System.IO.Path.Combine(Application.temporaryCachePath, streamingAssetsFileName);
                System.IO.File.WriteAllBytes(local, bytes);
                vp.url = "file://" + local;
                vp.Prepare();
            }
        }
#endif

        void PrepareAndPlay() {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (clip == null) return; // 안드로이드에선 코루틴에서 Prepare 호출
#endif
            vp.Prepare();
        }

        void OnPrepared(VideoPlayer p) {
            p.Play();
            audioSrc.Play();
            StartCoroutine(EnableSkipAfterDelay());
        }

        System.Collections.IEnumerator EnableSkipAfterDelay() {
            yield return new WaitForSecondsRealtime(canSkipDelay);
            canSkip = true;
            if (skipBtn) skipBtn.interactable = true;
        }

        void Update() {
            if (!canSkip) return;
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) {
                Skip();
            }
        }

        void OnEnded(VideoPlayer p) {
            if (autoAdvance) GoNext();
        }

        void Skip() {
            if (!canSkip) return;
            if (vp && vp.isPlaying) vp.Stop();
            if (autoAdvance) GoNext();
        }

        void GoNext() {
            try { SceneManager.LoadScene(nextScene); }
            catch { Debug.Log("다음 씬이 빌드 설정에 없어서 이동을 스킵했습니다."); }
        }
    }
}