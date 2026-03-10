// 单例模式的基类
// 加上 abstract 防止被 new  破坏单例唯一性
public abstract class BaseSingleton<T> where T : class, new() // 泛型约束 T 必须是一个类且具有无参构造函数 
{
    static T _instance;

    // 通过属性访问单例实例
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // 使用反射创建实例 速度慢但是不需要 new() 约束
                //_instance = System.Activator.CreateInstance<T>(); 

                // 直接使用 new 创建实例 速度快但是需要 new() 约束
                _instance = new T();

            }
            return _instance;
        }
    }

    // 通过函数访问单例实例
    public static T GetInstance()
    {
        if (_instance == null)
        {
            // 使用反射创建实例 速度慢但是不需要 new() 约束
            //_instance = System.Activator.CreateInstance<T>(); 

            // 直接使用 new 创建实例 速度快但是需要 new() 约束
            _instance = new T();
        }
        return _instance;
    }
}
