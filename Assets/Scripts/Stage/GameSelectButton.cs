using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ���� ���� ��ư - ĳ���ͺ� ���� ���� ó��
/// </summary>
public class GameSelectButton : MonoBehaviour
{
    [Header("��ư ����")]
    [SerializeField] private string gameType; // "Dolphin", "Penguin", "Otamatone"
    [SerializeField] private string sceneName = "2_Stage"; // �̵��� �� �̸�

    [Header("UI ������Ʈ")]
    [SerializeField] private Button selectButton;
    [SerializeField] private Image characterIcon;
    [SerializeField] private Text characterName;
    [SerializeField] private Text characterDescription;
    [SerializeField] private Image backgroundImage;

    [Header("���൵ ǥ��")]
    [SerializeField] private Slider overallProgressSlider;
    [SerializeField] private Text progressText;
    [SerializeField] private Text achievementCountText;

    [Header("���� ����")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.green;

    [Header("�ִϸ��̼� ����")]
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

        // ĳ���ͺ� ���� ����
        SetupCharacterInfo();
    }

    private void SetupCharacterInfo()
    {
        switch (gameType)
        {
            case "Dolphin":
                if (characterName != null) characterName.text = "����";
                if (characterDescription != null) characterDescription.text = "�ٴ� ģ���� �Բ��ϴ� �Ҹ� ���� ����";
                if (backgroundImage != null) backgroundImage.color = new Color(0.2f, 0.7f, 1.0f, 0.3f);
                break;

            case "Penguin":
                if (characterName != null) characterName.text = "���";
                if (characterDescription != null) characterDescription.text = "���� ģ���� �Բ��ϴ� ���� ����";
                if (backgroundImage != null) backgroundImage.color = new Color(0.9f, 0.9f, 1.0f, 0.3f);
                break;

            case "Otamatone":
                if (characterName != null) characterName.text = "��Ÿ����";
                if (characterDescription != null) characterDescription.text = "�ű��� ��Ÿ����� �Բ��ϴ� �Ǳ� ����";
                if (backgroundImage != null) backgroundImage.color = new Color(1.0f, 0.6f, 0.2f, 0.3f);
                break;
        }
    }

    private void UpdateProgressInfo()
    {
        if (DataManager.Instance == null) return;

        // ��ü ���൵ ��� (8�� ��������)
        int totalStages = 8; // �� ĳ���ʹ� 4 + 4 ��������
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

        // ���൵ �����̴� ������Ʈ
        if (overallProgressSlider != null)
        {
            float progress = (float)completedStages / (float)totalStages;
            overallProgressSlider.value = progress;
        }

        // ���൵ �ؽ�Ʈ ������Ʈ
        if (progressText != null)
        {
            progressText.text = $"���൵: {completedStages}/{totalStages} ({(float)completedStages / totalStages * 100:F0}%)";
        }

        // ���� ���� ������Ʈ
        if (achievementCountText != null)
        {
            achievementCountText.text = $"����: {totalAchievements}/8";
        }
    }

    public void OnButtonClick()
    {
        // ���� Ÿ�� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCurrentGameType(gameType);
        }

        // ���� ȿ�� ���
        StartCoroutine(SelectionEffect());

        // �� ��ȯ
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
        // ���� �������� ����
        if (backgroundImage != null)
        {
            backgroundImage.color = selectedColor;
        }

        // ������ �ִϸ��̼�
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

    // ���콺 ȣ�� �̺�Ʈ (���û���)
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
    /// �ܺο��� ���൵ ������ ���ΰ�ħ�� �� ȣ��
    /// </summary>
    public void RefreshProgressInfo()
    {
        UpdateProgressInfo();
    }
}
