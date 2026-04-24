using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class BasePanel : MonoBehaviour
{
    // 使用 Dictionary 存储控件，Key 为对象名，Value 为组件列表（解决一个对象多个组件的问题）
    protected readonly Dictionary<string, List<UIBehaviour>> controlDic = new();

    // 默认命名的黑名单，改为 HashSet 提高查询效率
    static readonly HashSet<string> DefaultNameSet = new()
    {
        "Button", "Toggle", "Slider", "Dropdown", "Input Field", "Scroll View", "Panel", 
        "Image", "Raw Image", "Text", "Text (TMP)", "Canvas", "EventSystem", "Background", 
        "Checkmark", "Label", "Viewport", "Content", "Fill Area", "Fill", "Handle", 
        "Handle Slide Area", "Sliding Area", "Scrollbar", "Scrollbar Horizontal", 
        "Scrollbar Vertical", "Text Area", "Placeholder", "Arrow", "Template", "Item", 
        "Item Background", "Item Checkmark", "Item Label"
    };

    protected virtual void Awake()
    {
        FindAllUIBehaviours();
    }

    // 一次性遍历所有 UI 组件，避免多次深度搜索
    void FindAllUIBehaviours()
    {
        // 仅遍历一次层级，获取所有 UIBehaviour 及其子类
        UIBehaviour[] allBehaviours = GetComponentsInChildren<UIBehaviour>(true);

        foreach (var behaviour in allBehaviours)
        {
            string objName = behaviour.gameObject.name;

            // 过滤默认命名
            if (DefaultNameSet.Contains(objName)) continue;

            // 填充字典
            if (!controlDic.TryGetValue(objName, out var list))
            {
                list = new List<UIBehaviour>();
                controlDic.Add(objName, list);
            }
            list.Add(behaviour);

            // 自动绑定事件 (使用模式匹配简化代码)
            BindEvents(objName, behaviour);
        }
    }

    // 给各个组件绑定事件
    void BindEvents(string name, UIBehaviour behaviour)
    {
        switch (behaviour)
        {
            case Button btn:
                btn.onClick.AddListener(() => ClickButton(name));
                break;
            case Slider slider:
                slider.onValueChanged.AddListener((val) => SliderValue(name, val));
                break;
            case Toggle toggle:
                toggle.onValueChanged.AddListener((val) => ToggleValue(name, val));
                break;
            case TMP_InputField input:
                input.onValueChanged.AddListener((val) => InputValue(name, val));
                break;
        }
    }

    // 获取指定名称的 UI 组件
    public T GetUIBehaviour<T>(string name) where T : UIBehaviour
    {
        if (controlDic.TryGetValue(name, out var list))
        {
            foreach (var item in list)
                if (item is T target) return target;
        }
        
        Debug.LogWarning($"[BasePanel] 控件 {name} 中找不到类型 {typeof(T).Name}");
        return null;
    }

    public abstract void ShowUI();
    public abstract void HideUI();

    // 虚方法供子类重写
    protected virtual void ClickButton(string name) { }
    protected virtual void SliderValue(string name, float value) { }
    protected virtual void ToggleValue(string name, bool value) { }
    protected virtual void InputValue(string name, string value) { }
}