using System;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class InvokeCardEffectsAssetTests
    {
        [TestCase(0, CardCollectionType.HandCard)]
        [TestCase(1, CardCollectionType.HandCard)]
        [TestCase(2, CardCollectionType.Deck)]
        [TestCase(3, CardCollectionType.Graveyard)]
        public void FourSources_SaveUnloadReload_PreservesInvokeAndNestedTargets(int source, CardCollectionType zone)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameData/InvokeCardEffectsRoundTrip.asset");
            var invoke = new InvokeCardEffects { TargetCards = _NestedTargets(zone) };
            var asset = _CreateSource(source, invoke);
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            if (asset is StandardCardDataScriptable card)
            {
                card.Data.SubSelects.Add(new ExistCardSelectionGroup
                {
                    Id = "chosen", CardCandidates = invoke.TargetCards,
                    SelectCount = new ConstInteger { Value = 1 }, IsMustSelect = new TrueValue()
                });
                invoke.TargetCards = new SubSelectedCardCollection { SelectionId = "chosen" };
            }

            try
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssetIfDirty(asset);
                Resources.UnloadAsset(asset);
                asset = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                Assert.That(asset, Is.Not.Null);
                var loadedInvoke = _GetInvoke(asset);
                if (asset is StandardCardDataScriptable loadedCard)
                {
                    Assert.That(((SubSelectedCardCollection)loadedInvoke.TargetCards).SelectionId, Is.EqualTo("chosen"));
                    var group = (ExistCardSelectionGroup)loadedCard.Data.SubSelects[0];
                    Assert.That(group.Id, Is.EqualTo("chosen"));
                    Assert.That(((ConstInteger)group.SelectCount).Value, Is.EqualTo(1));
                    Assert.That(group.IsMustSelect, Is.TypeOf<TrueValue>());
                    _AssertNestedTargets(group.CardCandidates, zone);
                }
                else
                {
                    _AssertNestedTargets(loadedInvoke.TargetCards, zone);
                }
                if (asset is CardBuffScriptable loadedBuff)
                {
                    var timingInvoke = (InvokeCardEffects)loadedBuff.Data.BuffEffects[GameTiming.AfterTurnEnd][0].Effect;
                    _AssertNestedTargets(timingInvoke.TargetCards, zone);
                }

                _SetCatalogSource(catalog, source, asset);
                Assert.That(GameDataValidator.ValidateNestedContent(catalog), Is.Empty);
                Assert.That(GameDataValidator.ValidateEffectResolvers(catalog), Is.Empty);
                Assert.That(GameDataValidator.ValidateEffectCommandHandlers(), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                if (asset != null && !AssetDatabase.Contains(asset))
                    UnityEngine.Object.DestroyImmediate(asset);
                // 僅移除本測試透過 GenerateUniqueAssetPath 建立的暫存資產。
                AssetDatabase.DeleteAsset(path);
            }
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        public void FourSources_ValidatorReportsMissingTargetOrNestedPlayer(int source, bool nested)
        {
            var invoke = new InvokeCardEffects
            {
                TargetCards = nested ? new CardsOfPlayer { Zone = CardCollectionType.HandCard } : null
            };
            var asset = _CreateSource(source, invoke);
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            try
            {
                _SetCatalogSource(catalog, source, asset);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(errors, Has.Some.Contains(nested ? "TargetCards.Player 為空" : "TargetCards 為空"));
                Assert.That(GameDataValidator.ValidateEffectResolvers(catalog), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("missing")]
        public void ValidatorReportsInvalidSubSelectionReference(string selectionId)
        {
            var card = _CreateSource(0, new InvokeCardEffects
            {
                TargetCards = new SubSelectedCardCollection { SelectionId = selectionId }
            });
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            try
            {
                _SetCatalogSource(catalog, 0, card);

                var errors = GameDataValidator.ValidateNestedContent(catalog);

                Assert.That(errors, Has.Some.Contains(string.IsNullOrWhiteSpace(selectionId)
                    ? "SubSelectedCardCollection.SelectionId 為空"
                    : "找不到 ExistCard 群組：missing"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(card);
            }
        }

        private static ScriptableObject _CreateSource(int source, InvokeCardEffects effect)
        {
            switch (source)
            {
                case 0:
                    var card = ScriptableObject.CreateInstance<StandardCardDataScriptable>();
                    card.Data.ID = "invoke-card";
                    card.Data.Effects.Add(effect);
                    return card;
                case 1:
                    var player = ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
                    player.Data.ID = "invoke-player-buff";
                    player.Data.MaxLevel = 1;
                    player.Data.LifeTimeData = new AlwaysLifeTimePlayerBuffData();
                    player.Data.BuffEffects[GameTiming.AfterTurnEnd] = new[] { new ConditionalPlayerBuffEffect { Effect = effect } };
                    return player;
                case 2:
                    var character = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();
                    character.Data.ID = "invoke-character-buff";
                    character.Data.MaxLevel = 1;
                    character.Data.LifeTimeData = new AlwaysLifeTimeCharacterBuffData();
                    character.Data.BuffEffects[GameTiming.AfterTurnEnd] = new[] { new ConditionalCharacterBuffEffect { Effect = effect } };
                    return character;
                case 3:
                    var cardBuff = ScriptableObject.CreateInstance<CardBuffScriptable>();
                    cardBuff.Data.ID = "invoke-card-buff";
                    cardBuff.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                    cardBuff.Data.Effects[CardTriggeredTiming.Drawed] = new[] { new ConditionalCardBuffEffect { Effect = effect } };
                    cardBuff.Data.BuffEffects[GameTiming.AfterTurnEnd] = new[] { new ConditionalCardBuffEffect { Effect = effect } };
                    return cardBuff;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source));
            }
        }

        private static InvokeCardEffects _GetInvoke(ScriptableObject asset) => asset switch
        {
            StandardCardDataScriptable card => (InvokeCardEffects)card.Data.Effects[0],
            PlayerBuffDataScriptable player => (InvokeCardEffects)player.Data.BuffEffects[GameTiming.AfterTurnEnd][0].Effect,
            CharacterBuffDataScriptable character => (InvokeCardEffects)character.Data.BuffEffects[GameTiming.AfterTurnEnd][0].Effect,
            CardBuffScriptable cardBuff => (InvokeCardEffects)cardBuff.Data.Effects[CardTriggeredTiming.Drawed][0].Effect,
            _ => throw new ArgumentException("不支援的測試來源", nameof(asset))
        };

        private static SingleCardCollection _NestedTargets(CardCollectionType zone) => new()
        {
            TargetCard = new IndexOfCardCollection
            {
                CardCollection = new CardsOfPlayer
                {
                    Player = new OppositePlayer { Reference = new ReactionOwnerPlayer() }, Zone = zone
                },
                Index = new ConstInteger { Value = 1 }, Order = OrderType.Descending
            }
        };

        private static void _AssertNestedTargets(ITargetCardCollectionValue targets, CardCollectionType zone)
        {
            var indexed = (IndexOfCardCollection)((SingleCardCollection)targets).TargetCard;
            var cards = (CardsOfPlayer)indexed.CardCollection;
            Assert.That(cards.Zone, Is.EqualTo(zone));
            Assert.That(((OppositePlayer)cards.Player).Reference, Is.TypeOf<ReactionOwnerPlayer>());
            Assert.That(indexed.Order, Is.EqualTo(OrderType.Descending));
            Assert.That(((ConstInteger)indexed.Index).Value, Is.EqualTo(1));
        }

        private static void _SetCatalogSource(GameContentCatalog catalog, int source, UnityEngine.Object asset)
        {
            var propertyName = source switch
            {
                0 => "_cardAssets", 1 => "_playerBuffAssets", 2 => "_characterBuffAssets", 3 => "_cardBuffAssets",
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };
            var serialized = new SerializedObject(catalog);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = asset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
