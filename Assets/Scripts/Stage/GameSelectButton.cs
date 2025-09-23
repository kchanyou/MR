using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 게임 선택 버튼 - 캐릭터별 게임 선택 처리
/// </summary>
public class GameSelectButton : MonoBehaviour
{
    [Header("버튼 설정")]
    [SerializeField] private string gameType; // "Dolphin", "Penguin", "Otamatone"
    [SerializeField] private string sceneName = "2_Stage"; // 이동할 씬 이름

    [Header("UI 컴포넌트")]
    [SerializeField] private Button selectButton;
    [SerializeField] private Image characterIcon;
    [SerializeField] private Text characterName;
    [SerializeField] private Text characterDescription;
    [SerializeField] private Image backgroundImage;

    [Header("진행도 표시")]
    [SerializeField] private Slider overallProgressSlider;
    [SerializeField] private Text progressText;
    [SerializeField] private Text achievementCountText;

    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.green;

    [Header("애니메이션 설정")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;

    private bool isHovered = false;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        SetupButton();
        UpdateProgressInfo();
    }

    private void SetupButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnButtonClick);
        }

        // 캐릭터별 정보 설정
        SetupCharacterInfo();
    }

    private void SetupCharacterInfo()
    {
        switch (gameType)
        {
            case "Dolphin":
                if (characterName != null) characterName.text = "돌고래";
                if (characterDescription != null) characterDescription.text = "바다 친구와 함께하는 소리 구별 게임";
                if (backgroundImage != null) backgroundImage.color = new Color(0.2f, 0.7f, 1.0f, 0.3f);
                break;

            case "Penguin":
                if (characterName != null) characterName.text = "펭귄";
                if (characterDescription != null) characterDescription.text = "얼음 친구와 함께하는 리듬 게임";
                if (backgroundImage != null) backgroundImage.color = new Color(0.9f, 0.9f, 1.0f, 0.3f);
                break;

            case "Otamatone":
                if (characterName != null) characterName.text = "오타마톤";
                if (characterDescription != null) characterDescription.text = "신기한 오타마톤과 함께하는 악기 게임";
                if (backgroundImage != null) backgroundImage.color = new Color(1.0f, 0.6f, 0.2f, 0.3f);
                break;
        }
    }

    private void UpdateProgressInfo()
    {
        if (DataManager.Instance == null) return;

        // 전체 진행도 계산 (8개 스테이지)
        int totalStages = 8; // 각 캐릭터당 4 + 4 스테이지
        int completedStages = 0;
        int totalAchievements = 0;

        string[] modes = { "Mode1", "Mode2" };

        foreach (string mode in modes)
        {
            for (int i = 0; i < 4; i++)
            {
                StageProgress progress = DataManager.Instance.GetStageProgress(gameType, mode, i);
                if (progress.bestScore > 0)
                {
                    completedStages++;
                }

                if (DataManager.Instance.IsAchievementUnlocked(gameType, mode, i))
                {
                    totalAchievements++;
                }
            }
        }

        // 진행도 슬라이더 업데이트
        if (overallProgressSlider != null)
        {
            float progress = (float)completedStages / (float)totalStages;
            overallProgressSlider.value = progress;
        }

        // 진행도 텍스트 업데이트
        if (progressText != null)
        {
            progressText.text = $"진행도: {completedStages}/{totalStages} ({(float)completedStages / totalStages * 100:F0}%)";
        }

        // 업적 개수 업데이트
        if (achievementCountText != null)
        {
            achievementCountText.text = $"업적: {totalAchievements}/8";
        }
    }

    public void OnButtonClick()
    {
        // 게임 타입 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCurrentGameType(gameType);
        }

        // 선택 효과 재생
        StartCoroutine(SelectionEffect());

        // 씬 전환
        Invoke(nameof(LoadStageScene), 0.5f);
    }

    private void LoadStageScene()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene(sceneName);
        }
    }

    private IEnumerator SelectionEffect()
    {
        // 선택 색상으로 변경
        if (backgroundImage != null)
        {
            backgroundImage.color = selectedColor;
        }

        // 스케일 애니메이션
        for (float t = 0; t < animationDuration; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(1.0f, hoverScale, t / animationDuration);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        for (float t = 0; t < animationDuration; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(hoverScale, 1.0f, t / animationDuration);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // 마우스 호버 이벤트 (선택사항)
    public void OnPointerEnter()
    {
        if (!isHovered)
        {
            isHovered = true;
            StartCoroutine(HoverEffect(true));
        }
    }

    public void OnPointerExit()
    {
        if (isHovered)
        {
            isHovered = false;
            StartCoroutine(HoverEffect(false));
        }
    }

    private IEnumerator HoverEffect(bool isEntering)
    {
        Color targetColor = isEntering ? hoverColor : normalColor;
        float targetScale = isEntering ? hoverScale : 1.0f;

        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }

        for (float t = 0; t < animationDuration; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(transform.localScale.x / originalScale.x, targetScale, t / animationDuration);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale * targetScale;
    }

    /// <summary>
    /// 외부에서 진행도 정보를 새로고침할 때 호출
    /// </summary>
    public void RefreshProgressInfo()
    {
        UpdateProgressInfo();
    }
}
