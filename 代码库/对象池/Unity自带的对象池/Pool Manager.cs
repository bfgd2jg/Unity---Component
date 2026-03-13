using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : BaseAutoSingleton<PoolTool>
{
    // 所有对象池
    Dictionary<GameObject, ObjectPool<GameObject>> poolDictionary = new();
    Dictionary<GameObject, Transform> poolParent = new();

    // 场景中的对象池根节点
    Transform poolRoot;

    void Awake()
    {
        // 创建对象池根节点
        GameObject root = new GameObject("Pool Tool");
        poolRoot = root.transform;
    }

    // 获取对象
    public GameObject Get(GameObject prefab)
    {
        // 如果池不存在就创建
        if (!poolDictionary.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }

        return poolDictionary[prefab].Get();
    }

    // 回收对象
    public void Release(GameObject obj, GameObject prefab)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            Destroy(obj);
            return;
        }

        poolDictionary[prefab].Release(obj);
    }

    // 创建对象池
    void CreatePool(GameObject prefab)
    {
        // 创建父节点
        GameObject rootObj = new(prefab.name + " Pool");
        rootObj.transform.SetParent(poolRoot);

        // 保存父节点
        poolParent.Add(prefab, rootObj.transform);

        ObjectPool<GameObject> pool = new ObjectPool<GameObject>
        (
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab);
                obj.name = prefab.name;

                // 放到对应的池节点
                obj.transform.SetParent(poolParent[prefab]);

                return obj;
            },

            actionOnGet: obj =>
            {
                obj.SetActive(true);
            },

            actionOnRelease: obj =>
            {
                obj.SetActive(false);

                // 回收到对应池
                obj.transform.SetParent(poolParent[prefab]);
            },

            actionOnDestroy: obj =>
            {
                Destroy(obj);
            },

            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 100
        );

        poolDictionary.Add(prefab, pool);
    }
}
