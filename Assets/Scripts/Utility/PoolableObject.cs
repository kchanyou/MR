using UnityEngine;

/// <summary>
/// Ǯ�� �ý��ۿ��� �����Ǵ� ������Ʈ�� �⺻ Ŭ����
/// </summary>
public class PoolableObject : MonoBehaviour
{
    [Header("Ǯ�� ����")]
    public string PoolTag;
    public bool AutoReturnToPool = false;
    public float AutoReturnDelay = 5.0f;

    private bool isPooled = false;

    /// <summary>
    /// Ǯ���� ������ �� ȣ��˴ϴ�
    /// </summary>
    public virtual void OnSpawn()
    {
        isPooled = true;

        if (AutoReturnToPool)
        {
            ObjectPool.Instance?.ReturnToPoolDelayed(PoolTag, gameObject, AutoReturnDelay);
        }
    }

    /// <summary>
    /// Ǯ�� ��ȯ�� �� ȣ��˴ϴ�
    /// </summary>
    public virtual void OnReturn()
    {
        isPooled = false;

        // ������Ʈ ���� ����
        ResetComponents();
    }

    /// <summary>
    /// ������Ʈ���� �ʱ� ���·� �����մϴ�
    /// </summary>
    protected virtual void ResetComponents()
    {
        // ��ƼŬ �ý��� ����
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            ps.Stop();
            ps.Clear();
        }

        // ����� ����
        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in audioSources)
        {
            audio.Stop();
        }

        // �ִϸ����� ����
        Animator[] animators = GetComponentsInChildren<Animator>();
        foreach (Animator animator in animators)
        {
            animator.Rebind();
        }

        // ������ٵ� ����
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// �������� Ǯ�� ��ȯ�մϴ�
    /// </summary>
    public void ReturnToPool()
    {
        if (isPooled && !string.IsNullOrEmpty(PoolTag))
        {
            ObjectPool.Instance?.ReturnToPool(PoolTag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        // �ڵ� ��ȯ �ڷ�ƾ�� �ִٸ� ����
        if (ObjectPool.Instance != null)
        {
            StopAllCoroutines();
        }
    }
}
