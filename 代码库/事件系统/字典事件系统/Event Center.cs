using System.Collections.Generic;
using UnityEngine.Events;

// 事件中心：负责统一管理所有游戏事件
// 继承 BaseSingleton 实现单例访问
public class EventCenter : BaseSingleton<EventCenter>
{
    EventCenter(){}

    // 存储所有事件
    // Key：事件类型
    // Value：事件信息（包含监听函数）
    private Dictionary<EventType, EventInfoBase> eventDic = new();

    #region 无参数事件

    // 添加无参数事件监听
    public void AddEventListener(EventType type, UnityAction action)
    {
        // 如果事件已经存在
        if (eventDic.TryGetValue(type, out var info))
        {
            // 转换为无参事件类型并添加监听
            if (info is EventInfo eventInfo)
                eventInfo.actions += action;
        }
        else
        {
            // 如果事件不存在，则创建新的事件
            eventDic.Add(type, new EventInfo(action));
        }
    }

    // 移除无参数事件监听
    public void RemoveEventListener(EventType type, UnityAction action)
    {
        if (eventDic.TryGetValue(type, out var info))
        {
            if (info is EventInfo eventInfo)
            {
                // 移除监听函数
                eventInfo.actions -= action;

                // 如果没有监听者了，就删除这个事件
                if (eventInfo.actions == null)
                    eventDic.Remove(type);
            }
        }
    }

    // 触发无参数事件
    public void EventTrigger(EventType type)
    {
        if (eventDic.TryGetValue(type, out var info))
        {
            if (info is EventInfo eventInfo)
                // 执行所有监听函数
                eventInfo.actions?.Invoke();
        }
    }

    #endregion


    #region 泛型事件（带参数）

    // 添加带参数的事件监听
    public void AddEventListener<T>(EventType type, UnityAction<T> action)
    {
        if (eventDic.TryGetValue(type, out var info))
        {
            // 转换为泛型事件类型
            if (info is EventInfo<T> eventInfo)
                eventInfo.actions += action;
        }
        else
        {
            // 创建新的泛型事件
            eventDic.Add(type, new EventInfo<T>(action));
        }
    }

    // 移除带参数的事件监听
    public void RemoveEventListener<T>(EventType type, UnityAction<T> action)
    {
        if (eventDic.TryGetValue(type, out var info))
        {
            if (info is EventInfo<T> eventInfo)
            {
                eventInfo.actions -= action;

                // 如果监听为空则删除事件
                if (eventInfo.actions == null)
                    eventDic.Remove(type);
            }
        }
    }

    // 触发带参数的事件
    public void EventTrigger<T>(EventType type, T arg)
    {
        if (eventDic.TryGetValue(type, out var info))
        {
            if (info is EventInfo<T> eventInfo)
                // 将参数传递给所有监听者
                eventInfo.actions?.Invoke(arg);
        }
    }

    #endregion


    // 清空所有事件
    public void Clear()
    {
        eventDic.Clear();
    }

    // 清空指定事件
    public void Clear(EventType type)
    {
        eventDic.Remove(type);
    }
}


// 事件信息基类（用于统一存储）
public abstract class EventInfoBase { }


// 无参数事件信息
public class EventInfo : EventInfoBase
{
    // 存储所有监听函数
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}


// 带参数事件信息
public class EventInfo<T> : EventInfoBase
{
    // 存储所有监听函数
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}


// 事件类型枚举（用于区分不同事件）
public enum EventType
{
    Event_1,
    Event_2
}