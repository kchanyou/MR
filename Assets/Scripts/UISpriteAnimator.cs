using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimator : MonoBehaviour
{
    public Image targetImage;       // UI 이미지
    public Sprite[] frames;         // 애니메이션 프레임들
    public float frameRate = 0.1f;  // 프레임당 시간 (0.1초 = 10fps)

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
