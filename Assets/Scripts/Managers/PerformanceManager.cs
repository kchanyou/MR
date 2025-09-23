using UnityEngine;
using System.Collections;

/// <summary>
/// ���� ������ ����͸��ϰ� ����ȭ�ϴ� �Ŵ���
/// </summary>
public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance;

    [Header("���� ����͸�")]
    [SerializeField] private bool showFPS = false;
    [SerializeField] private bool enablePerformanceOptimization = true;

    [Header("FPS ����")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private float lowFPSThreshold = 30f;

    [Header("�޸� ����")]
    [SerializeField] private float garbageCollectionInterval = 30f;
    [SerializeField] private bool autoGarbageCollection = true;

    // ���� ����͸� ����
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
        // Ÿ�� �����ӷ���Ʈ ����
        Application.targetFrameRate = targetFrameRate;

        // VSync ���� (����Ͽ����� ���� 1, PC������ 0)
#if UNITY_MOBILE
        QualitySettings.vSyncCount = 1;
#else
        QualitySettings.vSyncCount = 0;
#endif

        // �ڵ� ������ �÷��� ����
        if (autoGarbageCollection)
        {
            StartCoroutine(AutoGarbageCollection());
        }

        Debug.Log("Performance Manager �ʱ�ȭ �Ϸ�");
    }

    private void Update()
    {
        // FPS ���
        CalculateFPS();

        // ���� ����ȭ üũ
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

            // ������ üũ
            isLowPerformance = currentFPS < lowFPSThreshold;
        }
    }

    private void CheckPerformanceOptimization()
    {
        if (isLowPerformance)
        {
            // ������ ��Ȳ���� ����ȭ ����
            ApplyLowPerformanceOptimizations();
        }
    }

    private void ApplyLowPerformanceOptimizations()
    {
        // ��ƼŬ �ý��� ǰ�� ����
        var particles = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in particles)
        {
            if (ps.main.maxParticles > 50)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Min(main.maxParticles, 50);
            }
        }

        // ����� ǰ�� ����
        if (AudioManager.Instance != null)
        {
            // 3D ����� ��Ȱ��ȭ ��
        }

        Debug.Log("������ ����ȭ �����");
    }

    private IEnumerator AutoGarbageCollection()
    {
        while (true)
        {
            yield return new WaitForSeconds(garbageCollectionInterval);

            // �޸� ��뷮 üũ
            long memoryUsage = System.GC.GetTotalMemory(false);
            if (memoryUsage > 100 * 1024 * 1024) // 100MB �̻�
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
                Debug.Log($"������ �÷��� ���� - �޸� ��뷮: {memoryUsage / (1024 * 1024)}MB");
            }
        }
    }

    /// <summary>
    /// ���� FPS�� ��ȯ�մϴ�
    /// </summary>
    public float GetCurrentFPS()
    {
        return currentFPS;
    }

    /// <summary>
    /// ������ �������� Ȯ���մϴ�
    /// </summary>
    public bool IsLowPerformance()
    {
        return isLowPerformance;
    }

    /// <summary>
    /// ���� ����ȭ�� �������� �����մϴ�
    /// </summary>
    public void ForceOptimization()
    {
        ApplyLowPerformanceOptimizations();
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// FPS ǥ�� ���
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
