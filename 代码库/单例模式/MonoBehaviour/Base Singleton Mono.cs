using UnityEngine;

public class BaseSingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    static T _instance;

    public static T Instance
    {
        get
        {
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this as T;
        
        DontDestroyOnLoad(gameObject);
    }
}
