using UnityEngine;

// 自动回收组件，到时间后自动把对象归还给对象池
public class AutoRelease : MonoBehaviour
{
    // 是否已经开始计时
    [SerializeField] bool isStarted;
    // 生命周期时间
    [SerializeField] float lifeTime;
    [SerializeField] float timer;
            
    void OnDisable()
    {
        isStarted = false;
        // 重置计时器
        timer = 0f; 
    }

    // 初始化生命周期
    public void Init(float time)
    {
        lifeTime = time;
        isStarted = true;
        // 重置计时器
        timer = 0f; 
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime)
            Release();
    }

    void Release()
    {
        if (!isStarted) 
            return;
        // 调用对象池回收
        Pool.Instance.Release(gameObject);
    }
}

