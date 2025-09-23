using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// ���� ������Ʈ Ǯ�� �ý��� - ��ƼŬ, UI ��� ���� ������ ���� Ǯ
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public bool expandable = true;
    }

    [Header("Ǯ ����")]
    public Pool[] pools;

    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = CreatePooledObject(pool.prefab);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    private GameObject CreatePooledObject(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        obj.transform.SetParent(transform);

        // Ǯ�� ������ ������Ʈ�� ������Ʈ �߰�
        if (obj.GetComponent<PoolableObject>() == null)
        {
            obj.AddComponent<PoolableObject>();
        }

        return obj;
    }

    /// <summary>
    /// Ǯ���� ������Ʈ�� �����ɴϴ�
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = null;

        if (poolDictionary[tag].Count > 0)
        {
            objectToSpawn = poolDictionary[tag].Dequeue();
        }
        else
        {
            // Ǯ�� ��������� ���� ���� (Ȯ�� ������ Ǯ�� ���)
            Pool pool = System.Array.Find(pools, p => p.tag == tag);
            if (pool != null && pool.expandable)
            {
                objectToSpawn = CreatePooledObject(pool.prefab);
                Debug.Log($"Pool {tag} expanded. Created new object.");
            }
            else
            {
                Debug.LogWarning($"Pool {tag} is empty and not expandable.");
                return null;
            }
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Ǯ�� ������Ʈ �ʱ�ȭ
        PoolableObject poolableObj = objectToSpawn.GetComponent<PoolableObject>();
        if (poolableObj != null)
        {
            poolableObj.OnSpawn();
        }

        return objectToSpawn;
    }

    /// <summary>
    /// ������Ʈ�� Ǯ�� ��ȯ�մϴ�
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            Destroy(obj);
            return;
        }

        // Ǯ�� ������Ʈ ����
        PoolableObject poolableObj = obj.GetComponent<PoolableObject>();
        if (poolableObj != null)
        {
            poolableObj.OnReturn();
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        poolDictionary[tag].Enqueue(obj);
    }

    /// <summary>
    /// ������ Ǯ ��ȯ (�ڵ� ��ȯ��)
    /// </summary>
    public void ReturnToPoolDelayed(string tag, GameObject obj, float delay)
    {
        StartCoroutine(ReturnToPoolCoroutine(tag, obj, delay));
    }

    private IEnumerator ReturnToPoolCoroutine(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(tag, obj);
    }

    /// <summary>
    /// Ư�� Ǯ�� ��� ������Ʈ�� ��Ȱ��ȭ�մϴ�
    /// </summary>
    public void DeactivateAllInPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return;

        // Ȱ��ȭ�� ������Ʈ�� ã�Ƽ� ��Ȱ��ȭ
        PoolableObject[] activeObjects = FindObjectsOfType<PoolableObject>();
        foreach (PoolableObject obj in activeObjects)
        {
            if (obj.gameObject.activeSelf && obj.PoolTag == tag)
            {
                ReturnToPool(tag, obj.gameObject);
            }
        }
    }
}
