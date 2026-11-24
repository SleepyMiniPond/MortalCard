#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class CardSelectionPanelLifetimeTests
    {
        [UnityTest]
        public IEnumerator DestroyOpenPanel_ReleasesStateSubscription()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/UI/OverlayCanvas/CardSelectionPanel.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var panel = instance.GetComponent<CardSelectionPanel>();
                panel.Init(new GameViewModel(), new LocalizeLibrary(
                    new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
                    new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>()));
                var state = new SubscriptionProbe(new CardSelectionDisplayState(
                    Array.Empty<CardInfo>(), true, false, Array.Empty<Guid>(),
                    new CardSelectionPromptInfo("card", "first", 0, 1, 0, 1, CardSelectionCountRule.Optional)));
                panel.Open(state);
                Assert.That(state.IsSubscribed, Is.True);

                UnityEngine.Object.Destroy(instance);
                yield return null;

                Assert.That(state.IsSubscribed, Is.False);
            }
            finally
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
            }
        }

        private sealed class SubscriptionProbe : IReadOnlyReactiveProperty<CardSelectionDisplayState>
        {
            public CardSelectionDisplayState Value { get; }
            public bool HasValue => true;
            public bool IsSubscribed { get; private set; }
            public SubscriptionProbe(CardSelectionDisplayState value) => Value = value;
            public IDisposable Subscribe(IObserver<CardSelectionDisplayState> observer)
            {
                observer.OnNext(Value);
                IsSubscribed = true;
                return Disposable.Create(() => IsSubscribed = false);
            }
        }
    }
}
#endif
