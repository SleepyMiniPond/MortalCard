#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using MortalGame.Presentation.Abstractions;
using MortalGame.Presenter;
using NUnit.Framework;
using Optional;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class SingleCardDetailPanelLifetimeTests
    {
        [UnityTest]
        public IEnumerator DestroyOpenPanel_ReleasesStateAndCompletesEvents()
        {
            var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/SingleCardDetailPopup.prefab"));
            try
            {
                var panel = instance.GetComponentInChildren<SingleCardDetailPopupPanel>(true);
                panel.Init(new GameViewModel(), new LocalizeLibrary(
                    new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                    new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()));
                var state = new SubscriptionProbe();
                using var session = panel.Open(state);
                var ended = false;
                var count = 0;
                using var subscription = session.CloseRequested.Subscribe(_ => count++, () => ended = true);
                Assert.That(state.IsSubscribed, Is.True);
                UnityEngine.Object.Destroy(instance);
                yield return null;
                Assert.That(state.IsSubscribed, Is.False);
                Assert.That(ended, Is.True);
                Assert.That(count, Is.Zero);
                Assert.DoesNotThrow(session.Dispose);
            }
            finally { if (instance != null) UnityEngine.Object.Destroy(instance); }
        }

        private sealed class SubscriptionProbe : IReadOnlyReactiveProperty<SingleCardDetailDisplayState>
        {
            public SingleCardDetailDisplayState Value { get; } = new(new CardInfo(
                Guid.NewGuid(), "detail-test", default, default, Array.Empty<CardTheme>(),
                1, Option.Some(1), 1, Option.Some(1), null,
                Array.Empty<CardBuffInfo>(), Array.Empty<CardProperty>(), Array.Empty<string>()));
            public bool HasValue => true;
            public bool IsSubscribed { get; private set; }
            public IDisposable Subscribe(IObserver<SingleCardDetailDisplayState> observer)
            {
                observer.OnNext(Value);
                IsSubscribed = true;
                return Disposable.Create(() => IsSubscribed = false);
            }
        }
    }
}
#endif
