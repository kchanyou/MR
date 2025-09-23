using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 개별 업적 아이템 UI
/// </summary>
public class AchievementItem : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Image achievementIcon;
    [SerializeField] private Text achievementName;
    [SerializeField] private Text stageName;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text progressText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedStamp;
    [SerializeField] private Text unlockDateText;

    [Header("색상 설정")]
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = new Color(1.0f, 0.84f, 0.0f, 1.0f); // 골드 색상

    private AchievementData achievementData;

    /// <summary>
    /// 업적 아이템을 설정합니다.
    /// </summary>
    public void Setup(AchievementData data)
    {
        achievementData = data;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 업적 이름
        if (achievementName != null)
        {
            achievementName.text = achievementData.achievementName;
            achievementName.color = achievementData.isUnlocked ? unlockedColor : lockedColor;
        }

        // 스테이지 이름
        if (stageName != null)
        {
            stageName.text = achievementData.stageName;
        }

        // 진행도 슬라이더
        if (progressSlider != null)
        {
            float progress = (float)achievementData.currentScore / (float)achievementData.targetScore;
            progressSlider.value = Mathf.Clamp01(progress);
        }

        // 진행도 텍스트
        if (progressText != null)
        {
            progressText.text = $"{achievementData.currentScore} / {achievementData.targetScore}";
        }

        // 잠금/해금 상태
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!achievementData.isUnlocked);
        }

        if (unlockedStamp != null)
        {
            unlockedStamp.SetActive(achievementData.isUnlocked);
        }

        // 해금 날짜
        if (unlockDateText != null)
        {
            if (achievementData.isUnlocked && achievementData.unlockDate != default(System.DateTime))
            {
                unlockDateText.text = achievementData.unlockDate.ToString("yyyy.MM.dd");
                unlockDateText.gameObject.SetActive(true);
            }
            else
            {
                unlockDateText.gameObject.SetActive(false);
            }
        }

        // 업적 아이콘
        if (achievementIcon != null)
        {
            LoadAchievementIcon();
        }
    }

    private void LoadAchievementIcon()
    {
        // Resources에서 업적 아이콘 로드
        Sprite iconSprite = Resources.Load<Sprite>($"UI/Achievements/{achievementData.achievementIcon}");

        if (iconSprite != null)
        {
            achievementIcon.sprite = iconSprite;
        }
        else
        {
            // 기본 아이콘 사용
            achievementIcon.sprite = Resources.Load<Sprite>("UI/Achievements/default_achievement");
        }

        // 잠긴 상태면 회색으로 표시
        achievementIcon.color = achievementData.isUnlocked ? Color.white : Color.gray;
    }

    /// <summary>
    /// 업적 해금 애니메이션
    /// </summary>
    public void PlayUnlockAnimation()
    {
        StartCoroutine(UnlockAnimationCoroutine());
    }

    private IEnumerator UnlockAnimationCoroutine()
    {
        // 스케일 애니메이션
        Vector3 originalScale = transform.localScale;

        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(1.0f, 1.2f, t / 0.3f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            float scale = Mathf.Lerp(1.2f, 1.0f, t / 0.3f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;

        // 색상 변경
        if (achievementName != null)
        {
            achievementName.color = unlockedColor;
        }

        if (achievementIcon != null)
        {
            achievementIcon.color = Color.white;
        }
    }
}
