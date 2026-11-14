using System.Collections.Generic;
using UnityEngine;

namespace MortalGame.GameView
{

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
            while (_pool.Count > 0)
            {
                var view = _pool.Pop();
                if (view != null)
                    return view;
            }

            return Instantiate(_prefab, transform);
        }

        public void RecyclePrefab(T view)
        {
            // 停止 Play 或卸載場景後，動畫的 finally 仍可能回到此處。
            if (view == null)
                return;
            if (this == null || _recycleRoot == null)
            {
                Destroy(view.gameObject);
                return;
            }

            view.Reset();
            view.transform.SetParent(_recycleRoot, false);
            _pool.Push(view);
        }
    }
}
