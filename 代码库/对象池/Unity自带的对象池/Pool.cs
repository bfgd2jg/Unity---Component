using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class Pool : BaseAutoSingleton<Pool>
{
    // prefab -> 对应对象池
    readonly Dictionary<GameObject, ObjectPool<GameObject>> poolDictionary = new();

    // prefab -> 对应层级父物体（方便整理层级）
    readonly Dictionary<GameObject, Transform> poolParent = new();

    Transform poolRoot;

    void Awake()
    {
        if (poolRoot != null) return;

        // 所有对象池的总根节点
        poolRoot = transform;
    }

    // 获取对象，可选自动回收时间
    public GameObject Get(GameObject prefab, float autoReleaseTime = -1f)
    {
        // 没有池就创建
        if (!poolDictionary.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        GameObject obj = poolDictionary[prefab].Get();

        // 如果设置了生命周期，则自动回收
        if (autoReleaseTime > 0)
        {
            AutoRelease autoRelease = obj.GetComponent<AutoRelease>();
            if (autoRelease == null)
                autoRelease = obj.AddComponent<AutoRelease>();

            autoRelease.Init(autoReleaseTime);
        }

        return obj;
    }

    // 回收对象，不需要传 prefab
    public void Release(GameObject obj)
    {
        PoolItem item = obj.GetComponent<PoolItem>();

        // 没有归属信息就直接销毁
        if (item == null || !poolDictionary.ContainsKey(item.prefab))
        {
            Destroy(obj);
            Debug.LogError($"对象 {obj.name} 没有 PoolItem 组件或其 prefab 没有对应的对象池，已销毁");  

            return;
        }

        poolDictionary[item.prefab].Release(obj);
    }

    // 创建对象池
    void CreatePool(GameObject prefab)
    {
        // 为该 prefab 创建一个层级节点
        GameObject rootObj = new(prefab.name + " Pool");
        rootObj.transform.SetParent(poolRoot);

        poolParent.Add(prefab, rootObj.transform);

        ObjectPool<GameObject> pool = new ObjectPool<GameObject>
        (
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab);
                obj.name = prefab.name;

                // 记录这个对象属于哪个 prefab
                PoolItem item = obj.GetComponent<PoolItem>();
                if (item == null)
                    item = obj.AddComponent<PoolItem>();

                item.prefab = prefab;

                obj.transform.SetParent(poolParent[prefab]);

                return obj;
            },

            actionOnGet: obj =>
            {
                // 取出时激活
                obj.SetActive(true);
            },

            actionOnRelease: obj =>
            {
                // 回收时关闭并放回池节点下
                obj.SetActive(false);
                obj.transform.SetParent(poolParent[prefab]);
            },

            actionOnDestroy: obj =>
            {
                Destroy(obj);
            },

            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: 1000
        );

        poolDictionary.Add(prefab, pool);
    }

    // 预热对象池，提前创建一定数量对象
    public void Warmup(GameObject prefab, int count)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        List<GameObject> tempList = new List<GameObject>();

        // 先取出，触发实例化
        for (int i = 0; i < count; i++)
        {
            tempList.Add(poolDictionary[prefab].Get());
        }

        // 再全部回收
        foreach (var obj in tempList)
        {
            poolDictionary[prefab].Release(obj);
        }
    }
}
