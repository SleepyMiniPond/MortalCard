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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MortalGame.Tests
{
    public sealed class GameResultLosePanelTests
    {
        [UnityTest]
        public IEnumerator GameplayScene_AllLoseButtonsAreClickableAndReturnTheirReaction() => UniTask.ToCoroutine(async () =>
        {
            var scene = default(UnityEngine.SceneManagement.Scene);
            GameObject temporaryEventSystem = null;
            using var lifetime = new CancellationTokenSource();
            UniTask<GameplayLoseResult> run = default;
            var started = false;
            try
            {
                await EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Gameplay.unity",
                    new LoadSceneParameters(LoadSceneMode.Additive)).ToUniTask();
                scene = SceneManager.GetSceneByPath("Assets/Scenes/Gameplay.unity");
                var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GameResultLosePanel>(true)).Single();
                var serialized = new SerializedObject(panel);
                var popup = (GameObject)serialized.FindProperty("_panel").objectReferenceValue;
                var presenter = new GameResultLosePresenter(panel);
                var fields = new Dictionary<LoseReactionType, string>
                {
                    [LoseReactionType.Retry] = "_retryButton",
                    [LoseReactionType.Restart] = "_restartButton",
                    [LoseReactionType.Quit] = "_quitButton"
                };
                foreach (var field in fields)
                {
                    run = presenter.Run(lifetime.Token).Preserve();
                    started = true;
                    await UniTask.NextFrame();
                    Canvas.ForceUpdateCanvases();
                    var events = EventSystem.current;
                    if (events == null)
                    {
                        temporaryEventSystem = new GameObject("失敗面板測試事件系統", typeof(EventSystem));
                        events = temporaryEventSystem.GetComponent<EventSystem>();
                    }
                    var button = (Button)serialized.FindProperty(field.Value).objectReferenceValue;
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
                    Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),
                        Is.SameAs(button.gameObject), $"{field.Key} 不得被其他 UI 遮擋");
                    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    await _Wait(() => run.Status != UniTaskStatus.Pending);
                    Assert.That((await run).ReactionType, Is.EqualTo(field.Key));
                    Assert.That(popup.activeSelf, Is.False);
                }
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
            Assert.That(condition(), Is.True, "失敗操作應在期限內完成");
        }
    }
}
#endif
