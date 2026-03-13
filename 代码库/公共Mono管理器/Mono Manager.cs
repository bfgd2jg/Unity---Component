using UnityEngine.Events;

public class MonoManager : BaseAutoSingleton<MonoManager>
{
    #region 事件
    event UnityAction updataEvent;
    event UnityAction lateUpdataEvent;
    event UnityAction fixedUpdateEvent;
    #endregion

    #region 添加和移除监听器的方法
    public void AddUpdateListener(UnityAction action) =>
        updataEvent += action;

    public void RemoveUpdateListener(UnityAction action) =>
        updataEvent -= action;

    public void AddFixedUpdateListener(UnityAction action) =>
        fixedUpdateEvent += action;

    public void RemoveFixedUpdateListener(UnityAction action) =>
        fixedUpdateEvent -= action;

    public void AddLateUpdateListener(UnityAction action) =>
        lateUpdataEvent += action;

    public void RemoveLateUpdateListener(UnityAction action) =>
        lateUpdataEvent -= action;
    #endregion

    #region Unity 生命周期函数
    void Update() =>
        updataEvent?.Invoke();

    void LateUpdate() =>
        lateUpdataEvent?.Invoke();

    void FixedUpdate() =>
        fixedUpdateEvent?.Invoke();
    #endregion
}
