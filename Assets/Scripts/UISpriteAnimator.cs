using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimator : MonoBehaviour
{
    public Image targetImage;       // UI �̹���
    public Sprite[] frames;         // �ִϸ��̼� �����ӵ�
    public float frameRate = 0.1f;  // �����Ӵ� �ð� (0.1�� = 10fps)

    private int currentFrame;
    private float timer;

    void Update()
    {
        if (frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer -= frameRate;
            currentFrame = (currentFrame + 1) % frames.Length;
            targetImage.sprite = frames[currentFrame];
        }
    }
}
