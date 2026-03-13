using System.Collections.Generic;
using UnityEngine;

// 对象池管理器
// 负责：
// 1 管理所有对象池
// 2 提供获取对象的方法
// 3 提供回收对象的方法
public class PoolManager : BaseManager<PoolManager>
{
    // 是否在 Hierarchy 中创建对象池结构
    // true：
    // Pool
    //    Cube Pool
    //    Bullet Pool
    // false：
    // 不创建父物体，性能稍微更好
    public bool isOpenLayout = true;

    // 构造函数（私有）
    // 因为这个类继承了单例 BaseManager
    PoolManager(){}

    // 对象池字典
    // key  ：对象名字（例如 "Cube"）
    // value：PoolData（该类型对象的对象池）
    Dictionary<string, PoolData> poolDictionary = new();

    // 对象池总父物体
    // 用来在 Hierarchy 里统一管理所有池对象
    GameObject pool;

    // 获取对象
    // name：Resources里的Prefab名字
    // maxNumber：池子最大允许创建数量
    public GameObject GetGameObject(string name, int maxNumber = 10)
    {
        // 如果还没有创建总对象池，并且开启了布局
        if(pool == null && isOpenLayout)
            pool = new("Pool");

        GameObject obj;

        // 情况1：
        // 没有该对象池
        // 或者对象池里没有可用对象
        // 并且当前使用数量小于最大数量
        if(!poolDictionary.ContainsKey(name) || 
           poolDictionary[name].Count == 0 && poolDictionary[name].UsedCount < maxNumber)
        {
            // 创建新的对象
            obj = Object.Instantiate(Resources.Load<GameObject>(name));

            // 统一名字
            obj.name = name;

            // 如果池子还不存在
            if(!poolDictionary.ContainsKey(name))
            {
                // 创建一个新的对象池
                poolDictionary.Add(name,new(pool, name, obj));
            }
            else
            {
                // 如果池子已经存在
                // 只需要记录这个对象正在被使用
                poolDictionary[name].PushUsedList(obj);
            }
        }
        else
        {
            // 从对象池中取出对象
            obj = poolDictionary[name].Pop();
        }

        return obj;
    }

    // 回收对象
    // 把对象重新放回对象池
    public void PushGameObject(GameObject gameObject)
    {
        poolDictionary[gameObject.name].Push(gameObject);
    }

    // 清空对象池
    // 一般用于切换场景
    public void ClearPool()
    {
        poolDictionary.Clear();
        pool = null;
    }
}

public class PoolData
{
    // 用栈存储“可用对象”
    // Stack特点：
    // 后放进去的对象会优先被取出（LIFO）
    Stack<GameObject> dataStack = new();

    // 当前正在使用的对象
    List<GameObject> usedList = new();

    // 该对象池的根节点
    GameObject rootObj;

    // 当前可用对象数量
    public int Count => dataStack.Count;

    // 当前使用中的对象数量
    public int UsedCount => usedList.Count;

    // 构造函数
    public PoolData(GameObject pool, string name, GameObject usedObj)
    {
        // 如果没有开启Hierarchy布局
        if(!PoolManager.Instance.isOpenLayout)
            return;

        // 创建子节点
        // 比如：Cube Pool
        rootObj = new(name + " Pool");

        // 设置父物体
        rootObj.transform.SetParent(pool.transform);

        // 把创建的对象记录为“正在使用”
        PushUsedList(usedObj);
    }

    // 从对象池取出对象
    public GameObject Pop()
    {
        GameObject obj;

        // 如果池里有对象
        if(Count > 0)
        {
            // 从栈中取出
            obj = dataStack.Pop();

            // 加入正在使用列表
            usedList.Add(obj);
        }
        else
        {
            // 如果池空了
            // 这里直接复用一个正在使用的对象
            obj = usedList[0];

            usedList.RemoveAt(0);
            usedList.Add(obj);
        }

        // 激活对象
        obj.SetActive(true);

        // 如果开启布局
        // 把对象从池子节点移出
        if(PoolManager.Instance.isOpenLayout)
            obj.transform.SetParent(null);

        return obj;
    }

    // 回收对象
    public void Push(GameObject obj)
    {
        // 关闭对象
        obj.SetActive(false);

        // 放回栈
        dataStack.Push(obj);

        // 如果开启布局
        // 放回对象池节点
        if(PoolManager.Instance.isOpenLayout)
            obj.transform.SetParent(rootObj.transform);

        // 从使用列表移除
        usedList.Remove(obj);
    }

    // 记录对象正在被使用
    public void PushUsedList(GameObject obj)
    {
        usedList.Add(obj);
    }
}