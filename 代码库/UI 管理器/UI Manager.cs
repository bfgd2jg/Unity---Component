using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public enum E_UILayer
{
    Bottom,
    Middle,
    Top,
    System
}

public class UIManager : BaseSingleton<UIManager>
{
    UIManager(){}

    Canvas canvas;

    // 下层
    Transform bottomLayer;
    // 中层
    Transform middleLayer;
    // 上层
    Transform topLayer;
    // 系统层
    Transform systemLayer;

    bool isInit = false;


    // 容纳所有面板的字典
    Dictionary<string, BasePanel> panelDic = new();

    // 初始化 UI
    public void InitUI(System.Action onComplete = null)
    {
        int loadCount = 0;

        void Check()
        {
            loadCount++;
            if (loadCount >= 2)
            {
                isInit = true;
                onComplete?.Invoke();
            }
        }

        AddressablesManager.Instance.LoadAssetAsync<GameObject>("Canvas",(obj)=>
        {
            GameObject gameObject = Object.Instantiate(obj.Result);
            Object.DontDestroyOnLoad(gameObject);

            canvas = gameObject.GetComponent<Canvas>();

            bottomLayer = canvas.transform.Find("下层");
            middleLayer = canvas.transform.Find("中层");
            topLayer = canvas.transform.Find("上层");
            systemLayer = canvas.transform.Find("系统层");

            Check();

            Debug.Log("Canvas 初始化完毕");
        });

        AddressablesManager.Instance.LoadAssetAsync<GameObject>("EventSystem",(obj)=>
        {
            GameObject gameObject = Object.Instantiate(obj.Result);
            Object.DontDestroyOnLoad(gameObject);

            Check();

            Debug.Log("EventSystem 初始化完毕");
        });
    }

    // 获得要绑定的层级
    public Transform GetLayer(E_UILayer layer)
    {
        if (!isInit)
        {
            Debug.LogError("UI Manager 未初始化完成！");
            return null;
        }
        
        return layer switch
        {
            E_UILayer.Bottom => bottomLayer,
            E_UILayer.Middle => middleLayer,
            E_UILayer.Top => topLayer,
            E_UILayer.System => systemLayer,
            _ => middleLayer,
        };
    }

    // 显示面板
    public void ShowPanel<T>(E_UILayer layer = E_UILayer.Middle , System.Action<T> callback = null) where T : BasePanel
    {
        string name = typeof(T).Name;

        Debug.Log("加载 UI Key: " + name);

        if(panelDic.ContainsKey(name))
        {
            // 如果已经加载了面板数据 显示面板
            panelDic[name].gameObject.SetActive(true);
            panelDic[name].ShowUI();
            callback?.Invoke(panelDic[name] as T);
        }
        else
        {
            // 如果不存在面板 加载面板
            AddressablesManager.Instance.LoadAssetAsync<GameObject>(name,(obj) =>
            {
                Transform layerTransform = GetLayer(layer);

                // 实例化
                GameObject ui = Object.Instantiate(obj.Result, layerTransform, false);

                // 获得继承 BasePanel 的脚本
                T panel = ui.GetComponent<T>();
                panel.ShowUI();
                callback?.Invoke(panel);

                // 添加进字典
                panelDic.Add(name,panel);
            });
        }
    }

    // 隐藏面板
    public void HidePanel<T>() where T : BasePanel
    {
        string name = typeof(T).Name;

        if(panelDic.ContainsKey(name))
        {
            panelDic[name].HideUI();
            // 关闭面板
            panelDic[name].gameObject.SetActive(false);
        }
    }

    // 获取面板 
    public T GetPanel<T>() where T : BasePanel
    {
        string name = typeof(T).Name;

        if(panelDic.ContainsKey(name))
            return panelDic[name] as T;

        return null;
    }

    // 批量卸载所有隐藏的 UI（释放实例 + Addressables 资源）防止炸内存
    public void ReleaseAllHiddenPanels()
    {
        List<string> toRemove = new List<string>();

        foreach (var kv in panelDic)
        {
            string name = kv.Key;
            BasePanel panel = kv.Value;

            // 只释放隐藏的
            if (panel != null && !panel.gameObject.activeSelf)
            {
                Object.Destroy(panel.gameObject);

                AddressablesManager.Instance.Release<GameObject>(name);

                toRemove.Add(name);

                Debug.Log($"正在释放 UI: {name}");
            }
        }

        // 统一移除（避免遍历时修改字典）
        foreach (var key in toRemove)
        {
            panelDic.Remove(key);
        }

        Debug.Log("所有关闭的 UI 已释放");
    }

    // 为控件添加 EventTrigger
    public void AddEventTrigger(UIBehaviour ui, EventTriggerType type ,UnityAction<BaseEventData> callback)
    {
        // 没 EventTrigger 添加一个 EventTrigger
        if (!ui.TryGetComponent<EventTrigger>(out var trigger))
            trigger = ui.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry entry = new()
        {
            eventID = type
        }; 

        // 添加回调
        entry.callback.AddListener(callback);

        // 压入 EventTrigger
        trigger.triggers.Add(entry);
    }
}