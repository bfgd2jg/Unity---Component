using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesManager : BaseSingleton<AddressablesManager>
{
    // 单例构造函数私有化
    private AddressablesManager() { }
    
    // 资源信息类 用于保存资源句柄、引用计数、等待回调列表
    class AssetInfo
    {
        // Addressables 返回的操作句柄
        public AsyncOperationHandle Handle;

        // 引用计数
        // 用于保证多个系统同时使用资源时不会被提前释放
        public int RefCount;

        // 等待队列
        // 当资源正在加载时，后续请求的回调会暂存在这里
        // 等加载完成统一触发
        public List<Action<AsyncOperationHandle>> WaitingCallbacks = new();

        public AssetInfo(AsyncOperationHandle handle)
        {
            Handle = handle;
            RefCount = 1; // 第一次加载时引用计数为1
        }
    }

    // 资源缓存字典
    private readonly Dictionary<string, AssetInfo> resDic = new();

    // 异步加载单个资源
    public void LoadAssetAsync<T>(string name, Action<AsyncOperationHandle<T>> callBack)
    {
        // 生成唯一Key
        string keyName = $"{name}_{typeof(T).FullName}";

        // 如果缓存中已经存在
        if (resDic.TryGetValue(keyName, out AssetInfo info))
        {
            // 增加引用计数
            info.RefCount++;

            // 如果资源已经加载完成
            if (info.Handle.IsDone)
            {
                callBack?.Invoke(info.Handle.Convert<T>());
            }
            else
            {
                // 如果资源还在加载中
                // 将回调加入等待队列
                info.WaitingCallbacks.Add((h) => callBack?.Invoke(h.Convert<T>()));
            }
        }
        else
        {
            // 第一次加载该资源
            var handle = Addressables.LoadAssetAsync<T>(name);

            AssetInfo newInfo = new AssetInfo(handle);

            // 加入缓存
            resDic.Add(keyName, newInfo);

            // 注册加载完成事件
            handle.Completed += (obj) =>
            {
                // 加载成功
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    // 执行首次回调
                    callBack?.Invoke(obj);

                    // 快照回调队列
                    // 防止回调内部再次调用Load导致集合被修改
                    if (newInfo.WaitingCallbacks.Count > 0)
                    {
                        var queue = newInfo.WaitingCallbacks.ToArray();
                        newInfo.WaitingCallbacks.Clear();

                        foreach (var queuedAction in queue)
                            queuedAction?.Invoke(obj);
                    }
                }
                else
                {
                    // 加载失败
                    Debug.LogError($"[Addressables] 加载失败: {keyName}");

                    // 从缓存中移除
                    resDic.Remove(keyName);

                    // 清空等待队列
                    newInfo.WaitingCallbacks.Clear();
                }
            };
        }
    }
    
    // 批量加载资源
    public void LoadAssetsAsync<T>(Addressables.MergeMode mode, Action<T> callBack, params string[] keys)
    {
        if (keys == null || keys.Length == 0) return;

        // 排序Key
        // 防止 [A,B] 和 [B,A] 被认为是不同资源
        var sortedKeys = keys.OrderBy(s => s).ToList();

        string keyName = $"{string.Join("_", sortedKeys)}_{typeof(T).FullName}";

        // 如果缓存中存在
        if (resDic.TryGetValue(keyName, out AssetInfo info))
        {
            info.RefCount++;

            var handle = info.Handle.Convert<IList<T>>();

            if (handle.IsDone)
            {
                // 已加载完成
                foreach (var item in handle.Result)
                    callBack?.Invoke(item);
            }
            else
            {
                // 仍在加载
                info.WaitingCallbacks.Add((h) =>
                {
                    foreach (var item in h.Convert<IList<T>>().Result)
                        callBack?.Invoke(item);
                });
            }
        }
        else
        {
            // 第一次加载
            var handle = Addressables.LoadAssetsAsync<T>(sortedKeys, callBack, mode);

            AssetInfo newInfo = new AssetInfo(handle);

            resDic.Add(keyName, newInfo);

            handle.Completed += (obj) =>
            {
                // 仅成功时执行等待回调
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    if (newInfo.WaitingCallbacks.Count > 0)
                    {
                        var queue = newInfo.WaitingCallbacks.ToArray();
                        newInfo.WaitingCallbacks.Clear();

                        foreach (var queuedAction in queue)
                            queuedAction?.Invoke(obj);
                    }
                }
                else
                {
                    Debug.LogError($"[Addressables] 批量加载失败: {keyName}");

                    resDic.Remove(keyName);

                    newInfo.WaitingCallbacks.Clear();
                }
            };
        }
    }

    // 释放资源 只有引用计数为0时才真正释放
    public void Release<T>(string name)
    {
        string keyName = $"{name}_{typeof(T).FullName}";

        if (resDic.TryGetValue(keyName, out AssetInfo info))
        {
            // 防止计数变成负数
            info.RefCount = Math.Max(0, info.RefCount - 1);

            // 没有任何系统使用该资源时释放
            if (info.RefCount == 0)
            {
                if (info.Handle.IsValid())
                    Addressables.Release(info.Handle);

                resDic.Remove(keyName);
            }
        }
    }

    // 清空所有资源 一般用于切换大场景
    public void Clear()
    {
        foreach (var info in resDic.Values)
        {
            if (info.Handle.IsValid())
                Addressables.Release(info.Handle);
        }

        resDic.Clear();

        // 释放Unity未使用资源
        Resources.UnloadUnusedAssets();
    }
}