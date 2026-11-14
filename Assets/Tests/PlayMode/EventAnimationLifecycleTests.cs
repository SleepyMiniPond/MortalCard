using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameView;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class EventAnimationLifecycleTests
    {
        private GameObject _factoryObject;
        private GameObject _prefabObject;
        private ShieldEventViewFactory _factory;
        private EventAnimationTestAsset _animation;
        private ShieldEventView _view;

        [SetUp]
        public void SetUp()
        {
            _factoryObject = new GameObject("測試特效物件池");
            _factory = _factoryObject.AddComponent<ShieldEventViewFactory>();
            _prefabObject = new GameObject("測試護盾特效");
            var prefab = _prefabObject.AddComponent<ShieldEventView>();
            var director = _prefabObject.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            _animation = ScriptableObject.CreateInstance<EventAnimationTestAsset>();
            director.playableAsset = _animation;
            director.extrapolationMode = DirectorWrapMode.Loop;
            _SetField(typeof(BaseAnimationEventView), prefab, "_playableDirector", director);
            _SetField(typeof(PrefabFactory<ShieldEventView>), _factory, "_prefab", prefab);
            _SetField(typeof(PrefabFactory<ShieldEventView>), _factory, "_recycleRoot", _factoryObject.transform);
            _prefabObject.SetActive(false);
            _view = _factory.CreatePrefab();
        }

        [TearDown]
        public void TearDown()
        {
            if (_view != null) Object.DestroyImmediate(_view.gameObject);
            if (_factoryObject != null) Object.DestroyImmediate(_factoryObject);
            if (_prefabObject != null) Object.DestroyImmediate(_prefabObject);
            if (_animation != null) Object.DestroyImmediate(_animation);
        }

        [UnityTest]
        public IEnumerator CancelLiveAnimation_ReturnsViewToPool()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var cancellation = new CancellationTokenSource();
                var task = _PlayAndRecycle(cancellation.Token);
                Assert.AreEqual(UniTaskStatus.Pending, task.Status);

                cancellation.Cancel();
                Assert.IsTrue(await task.SuppressCancellationThrow());
                Assert.IsFalse(_view.gameObject.activeSelf);
                Assert.AreSame(_view, _factory.CreatePrefab());
            });
        }

        [UnityTest]
        public IEnumerator DestroyAnimatingView_CancelsWithoutRecyclingDestroyedObject()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var task = _PlayAndRecycle(CancellationToken.None);
                Assert.AreEqual(UniTaskStatus.Pending, task.Status);

                Object.Destroy(_view.gameObject);
                await UniTask.NextFrame();
                Assert.IsTrue(await task.SuppressCancellationThrow());

                _view = _factory.CreatePrefab();
                Assert.IsTrue(_view != null);
            });
        }

        [UnityTest]
        public IEnumerator DestroyFactoryBeforeAnimationEnds_DestroysOrphanedView()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var cancellation = new CancellationTokenSource();
                _view.transform.SetParent(null);
                var task = _PlayAndRecycle(cancellation.Token);
                Object.Destroy(_factoryObject);
                await UniTask.NextFrame();

                cancellation.Cancel();
                Assert.IsTrue(await task.SuppressCancellationThrow());
                await UniTask.NextFrame();
                Assert.IsTrue(_view == null);
            });
        }

        [UnityTest]
        public IEnumerator DestroyDirectorBeforeCancellation_DoesNotAccessDestroyedDirector()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var cancellation = new CancellationTokenSource();
                var task = _PlayAndRecycle(cancellation.Token);
                Object.DestroyImmediate(_view.GetComponent<PlayableDirector>());
                cancellation.Cancel();

                Assert.IsTrue(await task.SuppressCancellationThrow());
                Assert.AreSame(_view, _factory.CreatePrefab());
            });
        }

        [Test]
        public void DestroyPooledView_CreatePrefabSkipsDestroyedEntry()
        {
            _factory.RecyclePrefab(_view);
            Object.DestroyImmediate(_view.gameObject);

            _view = _factory.CreatePrefab();
            Assert.IsTrue(_view != null);
        }

        private UniTask _PlayAndRecycle(CancellationToken cancellationToken)
        {
            var method = typeof(CharacterEventAnimationPlayer)
                .GetMethod("_PlayAndRecycle", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(typeof(ShieldEventView));
            return (UniTask)method.Invoke(null, new object[] { _view, _factory, cancellationToken });
        }

        private static void _SetField(System.Type type, object target, string name, object value)
        {
            type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }

    public sealed class EventAnimationTestAsset : PlayableAsset
    {
        public override double duration => 60;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = Playable.Create(graph);
            var output = ScriptPlayableOutput.Create(graph, "測試動畫輸出");
            output.SetSourcePlayable(playable);
            return playable;
        }
    }
}
