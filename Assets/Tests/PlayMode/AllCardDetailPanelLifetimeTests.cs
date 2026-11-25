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
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class AllCardDetailPanelLifetimeTests
    {
        [UnityTest]
        public IEnumerator DestroyOpenPanel_ReleasesStateAndCompletesSessionEvents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/DetailCardInfoPanel.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var panel = instance.GetComponentInChildren<AllCardDetailPanel>(true);
                panel.Init(new GameViewModel(), new LocalizeLibrary(
                    new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                    new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()));
                var source = new SubscriptionProbe(new AllCardDetailDisplayState(
                    CardCollectionType.Deck, Array.Empty<CardInfo>()));
                using var session = panel.Open(source);
                var completed = false;
                var closeCount = 0;
                using var subscription = session.CloseRequested.Subscribe(_ => closeCount++, () => completed = true);
                Assert.That(source.IsSubscribed, Is.True);

                UnityEngine.Object.Destroy(instance);
                yield return null;

                Assert.That(source.IsSubscribed, Is.False);
                Assert.That(completed, Is.True);
                Assert.That(closeCount, Is.Zero);
                Assert.DoesNotThrow(session.Dispose);
            }
            finally
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
            }
        }

        private sealed class SubscriptionProbe : IReadOnlyReactiveProperty<AllCardDetailDisplayState>
        {
            public AllCardDetailDisplayState Value { get; }
            public bool HasValue => true;
            public bool IsSubscribed { get; private set; }
            public SubscriptionProbe(AllCardDetailDisplayState value) => Value = value;
            public IDisposable Subscribe(IObserver<AllCardDetailDisplayState> observer)
            {
                observer.OnNext(Value);
                IsSubscribed = true;
                return Disposable.Create(() => IsSubscribed = false);
            }
        }
    }
}
#endif
