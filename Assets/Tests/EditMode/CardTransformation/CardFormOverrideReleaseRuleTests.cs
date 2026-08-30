using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests.T010
{
    /// <summary>
    /// 驗證 External Override 的 Session 更新、Timing 解除與快照安全性。
    /// </summary>
    public sealed class CardFormOverrideReleaseRuleTests
    {
        private const string OverrideCardId = "t010-release-override";
        private const string SecondOverrideCardId = "t010-release-second-override";

        [Test]
        public void TriggerTiming_UpdatesSessionBeforeEvaluatingReleaseRule()
        {
            var built = _Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            _ApplyOverride(built, OverrideCardId, _CreateCounterReleaseRule());

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.AfterTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(built.Card.OverrideFormState.HasValue, Is.False);
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            var changed = events.OfType<CardFormChangedEvent>().Single();
            Assert.That(changed.Cause, Is.EqualTo(CardFormChangeCause.OverrideRemoved));
            Assert.That(changed.BeforeCardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(changed.AfterCardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        [Test]
        public void ObserveRootAction_WithMatchingTiming_UpdatesSessionWithoutReleasingOverride()
        {
            var built = _Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            _ApplyOverride(built, OverrideCardId, _CreateCounterReleaseRule());

            built.Gameplay.Manager
                .ObserveRootAction(new UpdateTimingAction(
                    GameTiming.AfterTurnEnd,
                    SystemSource.Instance))
                .ToList();

            Assert.That(built.Card.OverrideFormState.TryGetValue(out var state), Is.True);
            Assert.That(state.ReactionSessions["counter"].IntegerValue.ValueOr(-1), Is.EqualTo(1));
            Assert.That(built.Card.CardDataId, Is.EqualTo(OverrideCardId));
        }

        [Test]
        public void AfterPlayCardEnd_OnlyOwnerCardReleasesOverrideAfterRecycle()
        {
            var overrideCard = _CreateOverride(OverrideCardId);
            overrideCard.PropertyDatas.Add(new RecyclePropertyData());
            var built = new CardTransformationTestBuilder()
                .WithCard(overrideCard)
                .WithCard(_CreateOverride(SecondOverrideCardId))
                .Build();
            var otherCard = CardEntity.RuntimeCreateFromId(
                CardTransformationTestBuilder.BaseCardId,
                built.Gameplay.ContextManager.CardLibrary,
                built.Gameplay.ContextManager.CardPropertyEntityFactory);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(otherCard);
            _ApplyOverride(built, OverrideCardId, new CardFormOverrideReleaseRule
            {
                Timing = GameTiming.AfterPlayCardEnd,
                Conditions = { _CreatePlayedCardIsOverrideOwnerCondition() }
            });
            var (success, playingScope) = built.Gameplay.Ally.CardManager.TryPlayCard(
                built.Card,
                out _,
                out _);

            Assert.That(success, Is.True);
            playingScope.Dispose();
            built.Gameplay.Ally.CardManager.RecycleCardOnPlayEnd(
                built.Gameplay.Manager,
                built.Card).ToList();

            var otherCardEvents = built.Gameplay.Manager
                .TriggerTiming(
                    GameTiming.AfterPlayCardEnd,
                    _CreateCardPlayResultSource(otherCard, built.Gameplay.Ally))
                .ToList();

            Assert.That(otherCardEvents.OfType<CardFormChangedEvent>(), Is.Empty);
            Assert.That(built.Card.OverrideFormState.HasValue, Is.True);

            var ownerCardEvents = built.Gameplay.Manager
                .TriggerTiming(
                    GameTiming.AfterPlayCardEnd,
                    _CreateCardPlayResultSource(built.Card, built.Gameplay.Ally))
                .ToList();

            Assert.That(built.Gameplay.Ally.CardManager.HandCard.Cards, Does.Contain(built.Card));
            Assert.That(built.Card.OverrideFormState.HasValue, Is.False);
            Assert.That(ownerCardEvents.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void TriggerTiming_PlayingCardParticipatesInOverrideRelease()
        {
            var built = _Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            _ApplyOverride(built, OverrideCardId, new CardFormOverrideReleaseRule
            {
                Timing = GameTiming.BeforePlayCardEnd,
                Conditions = { new ConstCondition { Value = true } }
            });
            var (success, playingScope) = built.Gameplay.Ally.CardManager.TryPlayCard(
                built.Card,
                out _,
                out _);

            Assert.That(success, Is.True);
            try
            {
                var events = built.Gameplay.Manager
                    .TriggerTiming(GameTiming.BeforePlayCardEnd, SystemSource.Instance)
                    .ToList();

                Assert.That(built.Gameplay.Ally.CardManager.PlayingCard.HasValue, Is.True);
                Assert.That(built.Card.OverrideFormState.HasValue, Is.False);
                Assert.That(events.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            }
            finally
            {
                playingScope.Dispose();
            }
        }

        [Test]
        public void TriggerTiming_DisposeZoneCardDoesNotUpdateOrReleaseOverride()
        {
            var built = _Build();
            built.Gameplay.Ally.CardManager.DisposeZone.AddCard(built.Card);
            _ApplyOverride(built, OverrideCardId, _CreateCounterReleaseRule());

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.AfterTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(events.OfType<CardFormChangedEvent>(), Is.Empty);
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var state), Is.True);
            Assert.That(state.ReactionSessions["counter"].IntegerValue.ValueOr(-1), Is.EqualTo(0));
            Assert.That(built.Card.CardDataId, Is.EqualTo(OverrideCardId));
        }

        [Test]
        public void TriggerTiming_ReleasesOverrideWithoutReplayingSuppressedSelfTransform()
        {
            var baseCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                cost: 2,
                power: 3);
            baseCard.TransformRules.Add(new CardTransformRule
            {
                RuleId = "suppressed-self-transform",
                TransformKey = "stance",
                Timing = GameTiming.BeforeTurnEnd,
                Conditions = { new ConstCondition { Value = true } },
                Operation = new ApplyCardTransformOperationData
                {
                    TargetCardDataId = CardTransformationTestBuilder.AlternateCardId,
                    Persistence = CardFormPersistence.BattleOnly
                }
            });
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .WithCard(_CreateOverride(OverrideCardId))
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            _ApplyOverride(built, OverrideCardId, new CardFormOverrideReleaseRule
            {
                Timing = GameTiming.BeforeTurnEnd,
                Conditions = { new ConstCondition { Value = true } }
            });

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .OfType<CardFormChangedEvent>()
                .ToList();

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Cause, Is.EqualTo(CardFormChangeCause.OverrideRemoved));
            Assert.That(built.Card.SelfFormState.HasValue, Is.False);
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        [Test]
        public void QueuedRemove_WhenOverrideWasReplaced_DoesNotRemoveNewState()
        {
            var built = _Build();
            _ApplyOverride(built, OverrideCardId);
            built.Card.OverrideFormState.TryGetValue(out var firstState);
            var runner = new EffectQueueRunner();
            runner.Enqueue(new RemoveCardFormOverrideQueueItem(
                built.Gameplay.Manager,
                built.Card,
                firstState,
                new UpdateTimingAction(GameTiming.AfterTurnEnd, SystemSource.Instance)));
            _ApplyOverride(built, SecondOverrideCardId);
            built.Card.OverrideFormState.TryGetValue(out var secondState);

            var result = runner.RunToCompletion();

            Assert.That(result.Events, Is.Empty);
            Assert.That(built.Card.OverrideFormState.ValueOr((CardFormOverrideState)null),
                Is.SameAs(secondState));
            Assert.That(built.Card.CardDataId, Is.EqualTo(SecondOverrideCardId));
        }

        [Test]
        public void RemoveOverride_FormChangedEffectUsesRestoredEffectiveForm()
        {
            var baseCard = CardTransformationTestBuilder.CreateCardData(
                CardTransformationTestBuilder.BaseCardId,
                cost: 2,
                power: 3);
            baseCard.TriggeredEffects.Add(new TriggeredCardEffect
            {
                Timing = CardTriggeredTiming.FormChanged,
                Effects = new ICardEffect[]
                {
                    new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection
                        {
                            Target = new CardOwner { Card = new TriggeredCard() }
                        },
                        Value = new ConstInteger { Value = 2 }
                    }
                }
            });
            var built = new CardTransformationTestBuilder()
                .WithCard(baseCard)
                .WithCard(_CreateOverride(OverrideCardId))
                .Build();
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            _ApplyOverride(built, OverrideCardId, new CardFormOverrideReleaseRule
            {
                Timing = GameTiming.BeforeTurnEnd,
                Conditions = { new ConstCondition { Value = true } }
            });

            var events = built.Gameplay.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(events.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            Assert.That(events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(1));
            Assert.That(built.Gameplay.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(built.Card.CardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
        }

        private static BuiltCardTransformationTest _Build()
        {
            return new CardTransformationTestBuilder()
                .WithCard(_CreateOverride(OverrideCardId))
                .WithCard(_CreateOverride(SecondOverrideCardId))
                .Build();
        }

        private static void _ApplyOverride(
            BuiltCardTransformationTest built,
            string cardDataId,
            params CardFormOverrideReleaseRule[] releaseRules)
        {
            var sessionData = new SessionInteger
            {
                InitialValue = 0,
                LifeTime = SessionLifeTime.WholeGame,
                UpdateRules =
                {
                    new SessionInteger.TimingRule
                    {
                        Timing = GameTiming.AfterTurnEnd,
                        Rules = new[]
                        {
                            new ConditionIntegerUpdateRule
                            {
                                Conditions = new ICondition[] { new ConstCondition { Value = true } },
                                Operation = ConditionIntegerUpdateRule.UpdateType.AddOrigin,
                                NewValue = new ConstInteger { Value = 1 }
                            }
                        }
                    }
                }
            };
            var sessions = new Dictionary<string, IReactionSessionEntity>
            {
                ["counter"] = ReactionSessionEntityFactory.CreateDefault().Create(sessionData)
            };

            var result = built.Card.TryApplyOverrideForm(
                cardDataId,
                cardDataId,
                SystemSource.Instance,
                releaseRules,
                sessions);
            Assert.That(result.Status, Is.EqualTo(CardFormOperationStatus.Applied));
        }

        private static CardFormOverrideReleaseRule _CreateCounterReleaseRule()
        {
            return new CardFormOverrideReleaseRule
            {
                Timing = GameTiming.AfterTurnEnd,
                Conditions =
                {
                    new CardFormOverrideSessionCondition
                    {
                        SessionKey = "counter",
                        Conditions =
                        {
                            new ReactionSessionValueIntegerCondition
                            {
                                Conditions =
                                {
                                    new IntegerCompare
                                    {
                                        Arithmetic = ArithmeticConditionType.Equal,
                                        CompareValue = new ConstInteger { Value = 1 }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static ICondition _CreatePlayedCardIsOverrideOwnerCondition()
        {
            return new CardPlayCondition
            {
                Conditions =
                {
                    new CardPlayCardCondition
                    {
                        Conditions =
                        {
                            new CardIdentityCondition
                            {
                                CompareCard = new TriggeredCard()
                            }
                        }
                    }
                }
            };
        }

        private static CardPlayResultSource _CreateCardPlayResultSource(
            ICardEntity card,
            IPlayerEntity player)
        {
            var source = new CardPlaySource(
                card,
                0,
                1,
                new LoseEnergyEffectCommand(player, 0),
                new CardPlayAttributeEntity());
            return source.CreateResultSource(Array.Empty<IEffectResultAction>());
        }

        private static OverrideCardData _CreateOverride(string cardDataId)
        {
            return new OverrideCardData
            {
                ID = cardDataId,
                Cost = 6,
                Power = 10
            };
        }
    }
}
