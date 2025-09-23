using UnityEngine;
using System.Collections;

/// <summary>
/// 게임 성능을 모니터링하고 최적화하는 매니저
/// </summary>
public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance;

    [Header("성능 모니터링")]
    [SerializeField] private bool showFPS = false;
    [SerializeField] private bool enablePerformanceOptimization = true;

    [Header("FPS 설정")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private float lowFPSThreshold = 30f;

    [Header("메모리 관리")]
    [SerializeField] private float garbageCollectionInterval = 30f;
    [SerializeField] private bool autoGarbageCollection = true;

    // 성능 모니터링 변수
    private float deltaTime = 0.0f;
    private int frameCount = 0;
    private float timeCount = 0.0f;
    private float currentFPS = 0.0f;
    private bool isLowPerformance = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePerformanceManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePerformanceManager()
    {
        // 타겟 프레임레이트 설정
        Application.targetFrameRate = targetFrameRate;

        // VSync 설정 (모바일에서는 보통 1, PC에서는 0)
#if UNITY_MOBILE
        QualitySettings.vSyncCount = 1;
#else
        QualitySettings.vSyncCount = 0;
#endif

        // 자동 가비지 컬렉션 시작
        if (autoGarbageCollection)
        {
            StartCoroutine(AutoGarbageCollection());
        }

        Debug.Log("Performance Manager 초기화 완료");
    }

    private void Update()
    {
        // FPS 계산
        CalculateFPS();

        // 성능 최적화 체크
        if (enablePerformanceOptimization)
        {
            CheckPerformanceOptimization();
        }
    }

    private void CalculateFPS()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        timeCount += Time.unscaledDeltaTime;
        frameCount++;

        if (timeCount >= 1.0f)
        {
            currentFPS = frameCount / timeCount;
            frameCount = 0;
            timeCount = 0.0f;

            // 저성능 체크
            isLowPerformance = currentFPS < lowFPSThreshold;
        }
    }

    private void CheckPerformanceOptimization()
    {
        if (isLowPerformance)
        {
            // 저성능 상황에서 최적화 적용
            ApplyLowPerformanceOptimizations();
        }
    }

    private void ApplyLowPerformanceOptimizations()
    {
        // 파티클 시스템 품질 하향
        var particles = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in particles)
        {
            if (ps.main.maxParticles > 50)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Min(main.maxParticles, 50);
            }
        }

        // 오디오 품질 조정
        if (AudioManager.Instance != null)
        {
            // 3D 오디오 비활성화 등
        }

        Debug.Log("저성능 최적화 적용됨");
    }

    private IEnumerator AutoGarbageCollection()
    {
        while (true)
        {
            yield return new WaitForSeconds(garbageCollectionInterval);

            // 메모리 사용량 체크
            long memoryUsage = System.GC.GetTotalMemory(false);
            if (memoryUsage > 100 * 1024 * 1024) // 100MB 이상
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
                Debug.Log($"가비지 컬렉션 실행 - 메모리 사용량: {memoryUsage / (1024 * 1024)}MB");
            }
        }
    }

    /// <summary>
    /// 현재 FPS를 반환합니다
    /// </summary>
    public float GetCurrentFPS()
    {
        return currentFPS;
    }

    /// <summary>
    /// 저성능 상태인지 확인합니다
    /// </summary>
    public bool IsLowPerformance()
    {
        return isLowPerformance;
    }

    /// <summary>
    /// 성능 최적화를 수동으로 실행합니다
    /// </summary>
    public void ForceOptimization()
    {
        ApplyLowPerformanceOptimizations();
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// FPS 표시 토글
    /// </summary>
    public void ToggleFPSDisplay()
    {
        showFPS = !showFPS;
    }

    private void OnGUI()
    {
        if (showFPS)
        {
            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(0, 0, w, h * 2 / 50);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 50;
            style.normal.textColor = currentFPS < lowFPSThreshold ? Color.red : Color.green;

            float msec = deltaTime * 1000.0f;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, currentFPS);
            GUI.Label(rect, text, style);
        }
    }
}
