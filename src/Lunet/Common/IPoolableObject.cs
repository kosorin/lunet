namespace Lunet.Common;

internal interface IPoolableObject
{
    void Return();
}

internal interface IPoolableObject<TItem> : IPoolableObject
    where TItem : class, IPoolableObject<TItem>
{
    ObjectPool<TItem>? Owner { get; set; }

    void OnRent();

    void OnReturn();
}
