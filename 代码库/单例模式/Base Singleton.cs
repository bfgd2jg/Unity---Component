using UnityEngine;

public class BaseSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    
    static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // 动态挂载
                GameObject obj = new();
                _instance = obj.AddComponent<T>();

                // 设置对象名称
                obj.name = typeof(T).Name;
                
                // 切换场景不销毁
                DontDestroyOnLoad(obj);
            }

            return _instance;
        }
    }


}
