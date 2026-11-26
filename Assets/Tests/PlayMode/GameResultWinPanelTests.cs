#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presenter;
using NUnit.Framework;
using UniRx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class GameResultWinPanelTests
    {
        [UnityTest]
        public IEnumerator GameplayScene_ContinueIsClickableAndCancellationStillPropagates() => UniTask.ToCoroutine(async () =>
        {
            var scene = default(UnityEngine.SceneManagement.Scene);
            GameObject temporaryEventSystem = null;
            using var lifetime = new CancellationTokenSource();
            UniTask<GameplayWinResult> run = default;
            var started = false;
            try
            {
                await EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Gameplay.unity",
                    new LoadSceneParameters(LoadSceneMode.Additive)).ToUniTask();
                scene = SceneManager.GetSceneByPath("Assets/Scenes/Gameplay.unity");
                var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GameResultWinPanel>(true)).Single();
                var serialized = new SerializedObject(panel);
                var popup = (GameObject)serialized.FindProperty("_panel").objectReferenceValue;
                var button = (Button)serialized.FindProperty("_continueButton").objectReferenceValue;
                var presenter = new GameResultWinPresenter(panel);
                run = presenter.Run(lifetime.Token).Preserve();
                started = true;
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                var events = EventSystem.current;
                if (events == null)
                {
                    temporaryEventSystem = new GameObject("勝利面板測試事件系統", typeof(EventSystem));
                    events = temporaryEventSystem.GetComponent<EventSystem>();
                }
                var rect = (RectTransform)button.transform;
                var canvas = button.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                var pointer = new PointerEventData(events)
                {
                    position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                    button = PointerEventData.InputButton.Left
                };
                var hits = new List<RaycastResult>();
                events.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty);
                Assert.That(hits[0].gameObject, Is.SameAs(button.gameObject), "繼續按鈕不能被其他 UI 遮擋");
                ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                Assert.That(await run, Is.TypeOf<GameplayWinResult>());
                Assert.That(popup.activeSelf, Is.False);

                run = presenter.Run(lifetime.Token).Preserve();
                lifetime.Cancel();
                await _Wait(() => run.Status != UniTaskStatus.Pending);
                var canceled = await run.SuppressCancellationThrow();
                Assert.That(canceled.IsCanceled, Is.True);
                Assert.That(canceled.Result, Is.Null);
                Assert.That(popup.activeSelf, Is.False);
            }
            finally
            {
                lifetime.Cancel();
                if (started) await run.SuppressCancellationThrow();
                if (temporaryEventSystem != null) UnityEngine.Object.Destroy(temporaryEventSystem);
                if (scene.IsValid() && scene.isLoaded) await SceneManager.UnloadSceneAsync(scene).ToUniTask();
            }
        });

        private static async UniTask _Wait(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++) await UniTask.NextFrame();
            Assert.That(condition(), Is.True, "勝利操作應在期限內完成");
        }
    }
}
#endif
