using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

// Addressables 场景管理器
public class AddressablesSceneManager : BaseAutoSingleton<AddressablesSceneManager>
{
    #region SceneInfo

    // 场景信息
    class SceneInfo
    {
        public AsyncOperationHandle<SceneInstance> Handle;
        public int RefCount;
        public bool IsPersistent;

        public SceneInfo(AsyncOperationHandle<SceneInstance> handle, bool persistent)
        {
            Handle = handle;
            IsPersistent = persistent;

            // 常驻场景不给卸载机会
            RefCount = persistent ? int.MaxValue : 1;
        }
    }

    #endregion


    #region 字段

    // AddressablesKey -> SceneInfo
    readonly Dictionary<string, SceneInfo> _scenes = new();

    // 主常驻场景
    public string MainSceneKey = "Main";

    #endregion


    #region LoadScene

    // 加载场景，并把新加载的场景设为激活场景
    public async Task<SceneInstance> LoadSceneAsync(
        string key,
        bool persistent = false,
        Action<float> progress = null)
    {
        // 已加载
        if (_scenes.TryGetValue(key, out var info))
        {
            if (!info.IsPersistent)
                info.RefCount++;

            // 如果场景已经加载完成，激活它
            if (info.Handle.IsDone && info.Handle.Status == AsyncOperationStatus.Succeeded)
            {
                Scene loadedScene = info.Handle.Result.Scene;
                if (loadedScene.IsValid())
                    SceneManager.SetActiveScene(loadedScene);
            }

            return info.Handle.Result;
        }

        var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive);

        var newInfo = new SceneInfo(
            handle,
            persistent || key == MainSceneKey
        );

        _scenes.Add(key, newInfo);

        // 进度
        while (!handle.IsDone)
        {
            progress?.Invoke(handle.PercentComplete);
            await Task.Yield();
        }

        progress?.Invoke(1f);

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Scene load failed: {key}");
            _scenes.Remove(key);
            return default;
        }

        // 加载完成后，将新场景设为激活场景
        Scene sceneInstance = handle.Result.Scene;
        if (sceneInstance.IsValid())
            SceneManager.SetActiveScene(sceneInstance);

        return handle.Result;
    }

    #endregion


    #region UnloadScene

    // 卸载场景
    public async Task UnloadSceneAsync(string key)
    {
        if (!_scenes.TryGetValue(key, out var info))
            return;

        if (info.IsPersistent)
            return;

        info.RefCount--;

        if (info.RefCount > 0)
            return;

        if (!info.Handle.IsValid())
            return;

        await Addressables.UnloadSceneAsync(info.Handle).Task;

        _scenes.Remove(key);
    }

    #endregion


    #region Clear

    // 清理所有非常驻场景
    public async Task ClearAsync()
    {
        List<Task> tasks = new();

        foreach (var pair in _scenes)
        {
            var key = pair.Key;
            var info = pair.Value;

            if (info.IsPersistent)
                continue;

            if (!info.Handle.IsValid())
                continue;

            tasks.Add(Addressables.UnloadSceneAsync(info.Handle).Task);
        }

        await Task.WhenAll(tasks);

        List<string> remove = new();

        foreach (var pair in _scenes)
        {
            if (!pair.Value.IsPersistent)
                remove.Add(pair.Key);
        }

        foreach (var key in remove)
            _scenes.Remove(key);
    }

    #endregion


    #region Debug

    // 输出当前加载的场景
    public void PrintScenes()
    {
        Debug.Log("Loaded Scenes:");

        foreach (var pair in _scenes)
        {
            Debug.Log(pair.Key + "  Ref:" + pair.Value.RefCount);
        }
    }

    #endregion
}