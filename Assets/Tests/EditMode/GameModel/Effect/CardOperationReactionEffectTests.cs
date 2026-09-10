using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.Editor;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;

namespace MortalGame.Tests
{
    public sealed class CardOperationReactionEffectTests
    {
        public static IEnumerable<TestCaseData> MoveEffectSources()
        {
            yield return new TestCaseData(ReactionSource.PlayerBuff);
            yield return new TestCaseData(ReactionSource.CharacterBuff);
            yield return new TestCaseData(ReactionSource.CardBuff);
        }

        public static IEnumerable<TestCaseData> MoveEffects()
        {
            yield return new TestCaseData(
                typeof(DiscardCardEffect),
                CardCollectionType.Graveyard,
                MoveCardType.Discard);
            yield return new TestCaseData(
                typeof(ConsumeCardEffect),
                CardCollectionType.ExclusionZone,
                MoveCardType.Consume);
            yield return new TestCaseData(
                typeof(DisposeCardEffect),
                CardCollectionType.DisposeZone,
                MoveCardType.Dispose);
        }

        [TestCaseSource(nameof(MoveEffectSources))]
        public void MoveEffects_FromHand_WorkForAllReactionSources(ReactionSource source)
        {
            var setup = _CreateSetup(source);
            var card = setup.Card;
            var effect = new DiscardCardEffect
            {
                TargetCards = new CardsOfPlayer
                {
                    Player = new PlayerByFaction { Faction = Faction.Ally },
                    Zone = CardCollectionType.HandCard
                }
            };

            var result = _RunReactionEffect(setup, effect);

            Assert.That(
                setup.Built.Ally.CardManager.HandCard.Cards.Any(
                    candidate => candidate.Identity == card.Identity),
                Is.False);
            Assert.That(
                setup.Built.Ally.CardManager.Graveyard.Cards,
                Does.Contain(card));
            Assert.That(result.Events.OfType<MoveCardEvent>().Count(), Is.EqualTo(1));
        }

        [TestCaseSource(nameof(MoveEffects))]
        public void MoveEffects_FromHand_KeepExistingDestinationAndCommand(
            Type effectType,
            CardCollectionType destination,
            MoveCardType moveType)
        {
            var setup = _CreateSetup(ReactionSource.PlayerBuff);
            var effect = _CreateMoveEffect(effectType);

            var commands = EffectDataResolver.ResolvePlayerBuffEffect(
                setup.Context,
                (IPlayerBuffEffect)effect);
            Assert.That(commands.Commands, Has.Count.EqualTo(1));
            Assert.That(commands.Commands.Single(), Is.TypeOf<MoveCardEffectCommand>());
            var command = (MoveCardEffectCommand)commands.Commands.Single();
            Assert.That(command.Destination, Is.EqualTo(destination));
            Assert.That(command.MoveType, Is.EqualTo(moveType));

            var result = _RunReactionEffect(setup, effect);
            Assert.That(setup.Built.Ally.CardManager.HandCard.Cards, Is.Empty);
            Assert.That(
                setup.Built.Ally.CardManager.GetCardCollectionZone(destination).Cards,
                Has.Member(setup.Card));
            Assert.That(result.Events.OfType<MoveCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void MoveEffect_TargetingPlayingCard_IsSafeNoOp()
        {
            var setup = _CreateSetup(ReactionSource.PlayerBuff);
            var (success, _) = setup.Built.Ally.CardManager.TryPlayCard(
                setup.Card,
                out _,
                out _);
            Assert.That(success, Is.True);

            var effect = new DiscardCardEffect
            {
                TargetCards = new SingleCardCollection
                {
                    TargetCard = new PlayingCardOfPlayer
                    {
                        Player = new PlayerByFaction { Faction = Faction.Ally }
                    }
                }
            };
            var result = _RunReactionEffect(setup, effect);

            Assert.That(
                setup.Built.Ally.CardManager.PlayingCard.ValueOr((ICardEntity)null),
                Is.SameAs(setup.Card));
            Assert.That(result.Actions, Is.Empty);
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void TwoQueuedDiscardEffects_WhenFirstMovesCard_SecondIsSafeNoOp()
        {
            var setup = _CreateSetup(ReactionSource.PlayerBuff);
            var effect = new DiscardCardEffect
            {
                TargetCards = new SingleCardCollection { TargetCard = new TriggeredCard() }
            };
            var runner = new EffectQueueRunner();
            var cardContext = setup.Context with { Triggered = new CardTrigger(setup.Card) };
            runner.Enqueue(new CardEffectQueueItem(cardContext, effect));
            runner.Enqueue(new CardEffectQueueItem(cardContext, effect));

            var result = runner.RunToCompletion();

            Assert.That(setup.Built.Ally.CardManager.HandCard.Cards, Is.Empty);
            Assert.That(setup.Built.Ally.CardManager.Graveyard.Cards, Has.Member(setup.Card));
            Assert.That(result.Events.OfType<MoveCardEvent>().Count(), Is.EqualTo(1));
        }

        [TestCase(ReactionSource.PlayerBuff)]
        [TestCase(ReactionSource.CharacterBuff)]
        [TestCase(ReactionSource.CardBuff)]
        public void CreateCardEffect_InheritsReactionCaster(ReactionSource source)
        {
            var setup = _CreateSetup(source, includeCreatedBuff: true);
            var effect = new CreateCardEffect
            {
                Target = new PlayerByFaction { Faction = Faction.Ally },
                CardDataIds = { CardTestBuilder.CardId },
                AddCardBuffDatas =
                {
                    new AddCardBuffData
                    {
                        CardBuffId = setup.CreatedCardBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                },
                CreateDestination = CardCollectionType.HandCard
            };

            _RunReactionEffect(setup, effect);

            var createdCard = setup.Built.Ally.CardManager.HandCard.Cards
                .Single(card => card.Identity != setup.Card.Identity);
            var buff = createdCard.BuffManager.Buffs.Single();
            Assert.That(
                buff.Caster.ValueOr((IPlayerEntity)null),
                Is.SameAs(setup.Built.Enemy));
        }

        [TestCase(ReactionSource.PlayerBuff)]
        [TestCase(ReactionSource.CharacterBuff)]
        [TestCase(ReactionSource.CardBuff)]
        public void CloneCardEffect_InheritsReactionCaster(ReactionSource source)
        {
            var setup = _CreateSetup(source, includeCreatedBuff: true);
            var effect = new CloneCardEffect
            {
                Target = new PlayerByFaction { Faction = Faction.Ally },
                ClonedCards = new CardsOfPlayer
                {
                    Player = new PlayerByFaction { Faction = Faction.Ally },
                    Zone = CardCollectionType.HandCard
                },
                AddCardBuffDatas =
                {
                    new AddCardBuffData
                    {
                        CardBuffId = setup.CreatedCardBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                },
                CloneDestination = CardCollectionType.HandCard
            };

            _RunReactionEffect(setup, effect);

            var clonedCard = setup.Built.Ally.CardManager.HandCard.Cards
                .Single(card => card.Identity != setup.Card.Identity);
            var buff = clonedCard.BuffManager.Buffs.Single();
            Assert.That(
                buff.Caster.ValueOr((IPlayerEntity)null),
                Is.SameAs(setup.Built.Enemy));
        }

        [Test]
        public void CardOperationEffects_RoundTrip_PreserveNestedCardBuffData()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/CardOperationReactionRoundTrip.asset");
            var asset = UnityEngine.ScriptableObject.CreateInstance<CardBuffScriptable>();
            try
            {
                asset.Data.ID = "card-operation-round-trip";
                asset.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                asset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Effect = new CreateCardEffect
                        {
                            Target = new PlayerByFaction { Faction = Faction.Ally },
                            CardDataIds = { CardTestBuilder.CardId },
                            AddCardBuffDatas =
                            {
                                new AddCardBuffData
                                {
                                    CardBuffId = "nested-card-buff",
                                    Level = new ConstInteger { Value = 2 }
                                }
                            },
                            CreateDestination = CardCollectionType.Deck
                        }
                    }
                };
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<CardBuffScriptable>(path);
                var effect = loaded.Data.BuffEffects[GameTiming.AfterExecuteEnd][0].Effect;
                var create = effect as CreateCardEffect;
                Assert.That(create, Is.Not.Null);
                Assert.That(create.CardDataIds, Is.EqualTo(new[] { CardTestBuilder.CardId }));
                Assert.That(create.AddCardBuffDatas.Single().CardBuffId, Is.EqualTo("nested-card-buff"));
                Assert.That(create.AddCardBuffDatas.Single().Level, Is.TypeOf<ConstInteger>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void CardOperationEffects_ValidatorChecksDestinationAndNestedReferences()
        {
            var asset = UnityEngine.ScriptableObject.CreateInstance<CardBuffScriptable>();
            var catalog = UnityEngine.ScriptableObject.CreateInstance<GameContentCatalog>();
            try
            {
                asset.Data.ID = "card-operation-validator";
                asset.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                asset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Effect = new CreateCardEffect
                        {
                            Target = new PlayerByFaction { Faction = Faction.Ally },
                            CardDataIds = { "missing-card" },
                            AddCardBuffDatas =
                            {
                                new AddCardBuffData
                                {
                                    CardBuffId = "missing-card-buff",
                                    Level = new ConstInteger { Value = 1 }
                                }
                            },
                            CreateDestination = CardCollectionType.None
                        }
                    }
                };
                _SetCatalogArray(catalog, "_cardBuffAssets", asset);

                var nestedErrors = GameDataValidator.ValidateNestedContent(catalog);
                var referenceErrors = GameDataValidator.ValidateReferenceIds(catalog);

                Assert.That(
                    nestedErrors,
                    Has.Some.Contains("CreateCardEffect.CreateDestination 必須是有效的一般卡片區域"));
                Assert.That(
                    referenceErrors,
                    Has.Some.Contains("CreateCardEffect.CardDataIds 指向不存在的 ID：missing-card"));
                Assert.That(
                    referenceErrors,
                    Has.Some.Contains("CreateCardEffect.AddCardBuffDatas.CardBuffId 指向不存在的 ID：missing-card-buff"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void AddCardBuffEffect_FromPlayerBuff_ProducesExpectedCommandResultAndEvent()
        {
            var setup = _CreateSetup(ReactionSource.PlayerBuff, includeCreatedBuff: true);
            var targetCards = new CardsOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally },
                Zone = CardCollectionType.HandCard
            };
            var addCardBuffData = new AddCardBuffData
            {
                CardBuffId = setup.CreatedCardBuffId,
                Level = new ConstInteger { Value = 1 }
            };
            var effect = new AddCardBuffEffect
            {
                TargetCards = targetCards,
                AddCardBuffDatas = { addCardBuffData }
            };

            var commands = EffectDataResolver.ResolvePlayerBuffEffect(setup.Context, effect);
            var result = _RunPlayerBuffEffect(setup.Context, effect);

            Assert.That(commands.Commands.Single(), Is.TypeOf<AddCardBuffEffectCommand>());
            Assert.That(result.Actions.Single(), Is.TypeOf<AddCardBuffResultAction>());
            Assert.That(result.Events.OfType<AddCardBuffEvent>().Count(), Is.EqualTo(1));
            Assert.That(
                setup.Card.BuffManager.Buffs.Single().Caster.ValueOr((IPlayerEntity)null),
                Is.SameAs(setup.Built.Enemy));
        }

        [Test]
        public void RemoveCardBuffEffect_FromPlayerBuff_ProducesExpectedCommandResultAndEvent()
        {
            var setup = _CreateSetup(ReactionSource.PlayerBuff, includeCreatedBuff: true);
            var existingBuff = BuffTestBuilder.CreateCardBuff(
                setup.Context,
                setup.Built.ContextManager.CardBuffLibrary,
                setup.CreatedCardBuffId,
                setup.Built.Enemy);
            setup.Card.BuffManager.AddBuff(existingBuff);
            var targetCards = new CardsOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally },
                Zone = CardCollectionType.HandCard
            };
            var effect = new RemoveCardBuffEffect
            {
                TargetCards = targetCards,
                BuffId = setup.CreatedCardBuffId
            };

            var commands = EffectDataResolver.ResolvePlayerBuffEffect(setup.Context, effect);
            var result = _RunPlayerBuffEffect(setup.Context, effect);

            Assert.That(commands.Commands.Single(), Is.TypeOf<RemoveCardBuffEffectCommand>());
            Assert.That(result.Actions.Single(), Is.TypeOf<RemoveCardBuffResultAction>());
            Assert.That(result.Events.OfType<RemoveCardBuffEvent>().Count(), Is.EqualTo(1));
            Assert.That(setup.Card.BuffManager.Buffs, Is.Empty);
        }

        [Test]
        public void SharedPlayerBuffCardBuffEffects_RoundTripWithoutLosingTargets()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/SharedCardBuffPlayerBuffRoundTrip.asset");
            var asset = UnityEngine.ScriptableObject.CreateInstance<PlayerBuffDataScriptable>();
            try
            {
                asset.Data.ID = "shared-card-buff-player-buff-round-trip";
                asset.Data.BuffEffects[GameTiming.AfterExecuteEnd] = new[]
                {
                    new ConditionalPlayerBuffEffect
                    {
                        Effect = new AddCardBuffEffect
                        {
                            TargetCards = new CardsOfPlayer
                            {
                                Player = new PlayerByFaction { Faction = Faction.Ally },
                                Zone = CardCollectionType.HandCard
                            },
                            AddCardBuffDatas =
                            {
                                new AddCardBuffData
                                {
                                    CardBuffId = "shared-card-buff",
                                    Level = new ConstInteger { Value = 1 }
                                }
                            }
                        }
                    },
                    new ConditionalPlayerBuffEffect
                    {
                        Effect = new RemoveCardBuffEffect
                        {
                            TargetCards = new CardsOfPlayer
                            {
                                Player = new PlayerByFaction { Faction = Faction.Ally },
                                Zone = CardCollectionType.HandCard
                            },
                            BuffId = "shared-card-buff"
                        }
                    }
                };
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var loaded = AssetDatabase.LoadAssetAtPath<PlayerBuffDataScriptable>(path);
                var effects = loaded.Data.BuffEffects[GameTiming.AfterExecuteEnd];

                Assert.That(effects[0].Effect, Is.TypeOf<AddCardBuffEffect>());
                Assert.That(
                    ((AddCardBuffEffect)effects[0].Effect).TargetCards,
                    Is.TypeOf<CardsOfPlayer>());
                Assert.That(effects[1].Effect, Is.TypeOf<RemoveCardBuffEffect>());
                Assert.That(
                    ((RemoveCardBuffEffect)effects[1].Effect).TargetCards,
                    Is.TypeOf<CardsOfPlayer>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static ICardEffect _CreateMoveEffect(Type effectType)
        {
            var targetCards = new CardsOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally },
                Zone = CardCollectionType.HandCard
            };
            if (effectType == typeof(DiscardCardEffect))
                return new DiscardCardEffect { TargetCards = targetCards };
            if (effectType == typeof(ConsumeCardEffect))
                return new ConsumeCardEffect { TargetCards = targetCards };
            if (effectType == typeof(DisposeCardEffect))
                return new DisposeCardEffect { TargetCards = targetCards };
            throw new ArgumentOutOfRangeException(nameof(effectType), effectType, null);
        }

        private static EffectResult _RunReactionEffect(
            CardOperationSetup setup,
            ICardEffect effect)
        {
            var runner = new EffectQueueRunner();
            switch (setup.Source)
            {
                case ReactionSource.PlayerBuff:
                    runner.Enqueue(new PlayerBuffEffectQueueItem(
                        setup.Context,
                        (IPlayerBuffEffect)effect));
                    break;
                case ReactionSource.CharacterBuff:
                    runner.Enqueue(new CharacterBuffEffectQueueItem(
                        setup.Context,
                        (ICharacterBuffEffect)effect));
                    break;
                case ReactionSource.CardBuff:
                    runner.Enqueue(new CardBuffEffectQueueItem(
                        setup.Context,
                        (ICardBuffEffect)effect));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return runner.RunToCompletion();
        }

        private static EffectResult _RunPlayerBuffEffect(
            TriggerContext context,
            IPlayerBuffEffect effect)
        {
            var runner = new EffectQueueRunner();
            runner.Enqueue(new PlayerBuffEffectQueueItem(context, effect));
            return runner.RunToCompletion();
        }

        private static CardOperationSetup _CreateSetup(
            ReactionSource source,
            bool includeCreatedBuff = false)
        {
            const string sourceCardBuffId = "operation-source-card-buff";
            const string createdCardBuffId = "operation-created-card-buff";
            var builder = new GameplayManagerTestBuilder()
                .WithCardBuff(new CardBuffData
                {
                    ID = sourceCardBuffId,
                    LifeTimeData = new AlwaysLifeTimeCardBuffData()
                });
            if (includeCreatedBuff)
            {
                builder.WithCardBuff(new CardBuffData
                {
                    ID = createdCardBuffId,
                    LifeTimeData = new AlwaysLifeTimeCardBuffData()
                });
            }

            var built = builder.Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var timing = new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance);
            var context = source switch
            {
                ReactionSource.PlayerBuff => new TriggerContext(
                    built.Manager,
                    new PlayerBuffTrigger(
                        built.Ally,
                        BuffTestBuilder.CreatePlayerBuff("operation-source-player-buff", built.Enemy)),
                    timing),
                ReactionSource.CharacterBuff => new TriggerContext(
                    built.Manager,
                    new CharacterBuffTrigger(
                        built.Ally.MainCharacter,
                        BuffTestBuilder.CreateCharacterBuff(
                            "operation-source-character-buff",
                            built.Enemy)),
                    timing),
                ReactionSource.CardBuff => _CreateCardBuffContext(
                    built,
                    card,
                    sourceCardBuffId,
                    timing),
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };
            var createdId = includeCreatedBuff ? createdCardBuffId : string.Empty;
            return new CardOperationSetup(built, card, context, source, createdId);
        }

        private static void _SetCatalogArray<TAsset>(
            GameContentCatalog catalog,
            string fieldName,
            TAsset asset)
            where TAsset : UnityEngine.Object
        {
            typeof(GameContentCatalog)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(catalog, new[] { asset });
        }

        private static TriggerContext _CreateCardBuffContext(
            BuiltGameplay built,
            ICardEntity card,
            string sourceCardBuffId,
            IActionUnit timing)
        {
            var creationContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                timing);
            var sourceBuff = BuffTestBuilder.CreateCardBuff(
                creationContext,
                built.ContextManager.CardBuffLibrary,
                sourceCardBuffId,
                built.Enemy);
            card.BuffManager.AddBuff(sourceBuff);
            return new TriggerContext(
                built.Manager,
                new CardBuffTrigger(card, sourceBuff),
                timing);
        }

        public enum ReactionSource
        {
            PlayerBuff,
            CharacterBuff,
            CardBuff
        }

        private sealed record CardOperationSetup(
            BuiltGameplay Built,
            ICardEntity Card,
            TriggerContext Context,
            ReactionSource Source,
            string CreatedCardBuffId);
    }
}
