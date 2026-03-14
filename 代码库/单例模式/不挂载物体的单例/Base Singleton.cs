using System;
using System.Reflection;
// 单例模式的基类
// 加上 abstract 防止被 new  破坏单例唯一性
public abstract class BaseManager<T> where T : class // 泛型约束 T
{
    static readonly Lazy<T> instance = new(() =>
    {
        Type type = typeof(T);

        ConstructorInfo info = type.GetConstructor
        (
            BindingFlags.Instance |
            BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null
        );

        if(info == null)
            throw new Exception($"{type} 必须要有私有构造函数");

        return info.Invoke(null) as T;
    });

    public static T Instance => instance.Value;
}
