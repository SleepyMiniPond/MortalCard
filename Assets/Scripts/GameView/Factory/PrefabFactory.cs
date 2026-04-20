using System.Collections.Generic;
using UnityEngine;

public interface IRecyclable
{
    void Reset();
}

public class PrefabFactory<T> : MonoBehaviour where T : MonoBehaviour, IRecyclable
{
    [SerializeField]
    private T _prefab;
    [SerializeField]
    private Transform _recycleRoot;

    private Stack<T> _pool = new Stack<T>();

    public T CreatePrefab()
    {
        if (_pool.Count > 0) 
        {
            return _pool.Pop();
        }

        return Instantiate(_prefab, transform);
    }

    public void RecyclePrefab(T view)
    {
        view.Reset();
        view.transform.SetParent(_recycleRoot, false);
        _pool.Push(view);
    }
}
