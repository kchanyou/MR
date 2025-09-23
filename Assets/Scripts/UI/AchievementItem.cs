using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ���� ���� ������ UI
/// </summary>
public class AchievementItem : MonoBehaviour
{
    [Header("UI ������Ʈ")]
    [SerializeField] private Image achievementIcon;
    [SerializeField] private Text achievementName;
    [SerializeField] private Text stageName;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Text progressText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject unlockedStamp;
    [SerializeField] private Text unlockDateText;

    [Header("���� ����")]
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = new Color(1.0f, 0.84f, 0.0f, 1.0f); // ��� ����

    private AchievementData achievementData;

    /// <summary>
    /// ���� �������� �����մϴ�.
    /// </summary>
    public void Setup(AchievementData data)
    {
        achievementData = data;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // ���� �̸�
        if (achievementName != null)
        {
            achievementName.text = achievementData.achievementName;
            achievementName.color = achievementData.isUnlocked ? unlockedColor : lockedColor;
        }

        // �������� �̸�
        if (stageName != null)
        {
            stageName.text = achievementData.stageName;
        }

        // ���൵ �����̴�
        if (progressSlider != null)
        {
            float progress = (float)achievementData.currentScore / (float)achievementData.targetScore;
            progressSlider.value = Mathf.Clamp01(progress);
        }

        // ���൵ �ؽ�Ʈ
        if (progressText != null)
        {
            progressText.text = $"{achievementData.currentScore} / {achievementData.targetScore}";
        }

        // ���/�ر� ����
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!achievementData.isUnlocked);
        }

        if (unlockedStamp != null)
        {
            unlockedStamp.SetActive(achievementData.isUnlocked);
        }

        // �ر� ��¥
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

        // ���� ������
        if (achievementIcon != null)
        {
            LoadAchievementIcon();
        }
    }

    private void LoadAchievementIcon()
    {
        // Resources���� ���� ������ �ε�
        Sprite iconSprite = Resources.Load<Sprite>($"UI/Achievements/{achievementData.achievementIcon}");

        if (iconSprite != null)
        {
            achievementIcon.sprite = iconSprite;
        }
        else
        {
            // �⺻ ������ ���
            achievementIcon.sprite = Resources.Load<Sprite>("UI/Achievements/default_achievement");
        }

        // ��� ���¸� ȸ������ ǥ��
        achievementIcon.color = achievementData.isUnlocked ? Color.white : Color.gray;
    }

    /// <summary>
    /// ���� �ر� �ִϸ��̼�
    /// </summary>
    public void PlayUnlockAnimation()
    {
        StartCoroutine(UnlockAnimationCoroutine());
    }

    private IEnumerator UnlockAnimationCoroutine()
    {
        // ������ �ִϸ��̼�
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

        // ���� ����
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
