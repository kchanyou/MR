using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 범용 오브젝트 풀링 시스템 - 파티클, UI 요소 등의 재사용을 위한 풀
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

    [Header("풀 설정")]
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

        // 풀링 가능한 오브젝트에 컴포넌트 추가
        if (obj.GetComponent<PoolableObject>() == null)
        {
            obj.AddComponent<PoolableObject>();
        }

        return obj;
    }

    /// <summary>
    /// 풀에서 오브젝트를 가져옵니다
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
            // 풀이 비어있으면 새로 생성 (확장 가능한 풀인 경우)
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

        // 풀링 오브젝트 초기화
        PoolableObject poolableObj = objectToSpawn.GetComponent<PoolableObject>();
        if (poolableObj != null)
        {
            poolableObj.OnSpawn();
        }

        return objectToSpawn;
    }

    /// <summary>
    /// 오브젝트를 풀로 반환합니다
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            Destroy(obj);
            return;
        }

        // 풀링 오브젝트 정리
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
    /// 지연된 풀 반환 (자동 반환용)
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
    /// 특정 풀의 모든 오브젝트를 비활성화합니다
    /// </summary>
    public void DeactivateAllInPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) return;

        // 활성화된 오브젝트들 찾아서 비활성화
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
