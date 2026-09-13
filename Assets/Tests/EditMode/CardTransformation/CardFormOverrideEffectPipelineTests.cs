using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests.CardTransformation
{
    /// <summary>
    /// 驗證 External Override 從 GameData、Resolver、Command 到 Handler 的完整套用流程。
    /// </summary>
    public class CardFormOverrideEffectPipelineTests
    {
        private const string OverrideCardId = "pipeline-override";

        [Test]
        public void Resolver_CopiesConfiguredPayloadIntoApplyCommand()
        {
            var built = _Build();
            var effect = _CreateEffect(built.Card, "pipeline", OverrideCardId);

            var commandSet = EffectDataResolver.ResolveCardEffect(built.Context, effect);

            var command = commandSet.Commands.Single() as ApplyCardFormOverrideEffectCommand;
            Assert.That(command, Is.Not.Null);
            Assert.That(command.Target, Is.SameAs(built.Card));
            Assert.That(command.OverrideKey, Is.EqualTo("pipeline"));
            Assert.That(command.TargetCardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(command.ReleaseRules, Has.Count.EqualTo(1));
            Assert.That(command.ReactionSessionDatas.ContainsKey("counter"), Is.True);
        }

        [Test]
        public void ExecuteApplyCommand_CreatesRuntimeSessionResultActionAndFormChangedEvent()
        {
            var built = _Build();
            var effect = _CreateEffect(built.Card, "pipeline", OverrideCardId);
            var commandSet = EffectDataResolver.ResolveCardEffect(built.Context, effect);

            var result = _ExecuteCommands(built.Context, commandSet);

            Assert.That(built.Card.CardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(result.Actions.OfType<ApplyCardFormOverrideResultAction>().Count(), Is.EqualTo(1));
            var changedEvent = result.Events.OfType<CardFormChangedEvent>().Single();
            Assert.That(changedEvent.BeforeCardDataId, Is.EqualTo(CardTransformationTestBuilder.BaseCardId));
            Assert.That(changedEvent.AfterCardDataId, Is.EqualTo(OverrideCardId));
            Assert.That(changedEvent.Cause, Is.EqualTo(CardFormChangeCause.OverrideApplied));
            Assert.That(changedEvent.CardInfo.CardDataID, Is.EqualTo(OverrideCardId));
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var state), Is.True);
            Assert.That(state.ReactionSessions["counter"].IntegerValue.ValueOr(-1), Is.EqualTo(3));
        }

        [Test]
        public void ExecuteSameOverrideTwice_SecondExecutionIsNoOpAndPreservesRuntimeState()
        {
            var built = _Build();
            var effect = _CreateEffect(built.Card, "pipeline", OverrideCardId);
            var firstCommands = EffectDataResolver.ResolveCardEffect(built.Context, effect);
            _ExecuteCommands(built.Context, firstCommands);
            built.Card.OverrideFormState.TryGetValue(out var firstState);

            var secondCommands = EffectDataResolver.ResolveCardEffect(built.Context, effect);
            var secondResult = _ExecuteCommands(built.Context, secondCommands);

            Assert.That(secondResult.Actions, Is.Empty);
            Assert.That(secondResult.Events, Is.Empty);
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var secondState), Is.True);
            Assert.That(secondState, Is.SameAs(firstState));
            Assert.That(secondState.ReactionSessions, Is.SameAs(firstState.ReactionSessions));
        }

        [Test]
        public void ExecuteDifferentKeyWithSameCardData_ReplacesStateWithoutFormChangedEvent()
        {
            var built = _Build();
            var firstEffect = _CreateEffect(built.Card, "first", OverrideCardId);
            var firstCommands = EffectDataResolver.ResolveCardEffect(built.Context, firstEffect);
            _ExecuteCommands(built.Context, firstCommands);
            built.Card.OverrideFormState.TryGetValue(out var firstState);
            var secondEffect = _CreateEffect(built.Card, "second", OverrideCardId);
            var secondCommands = EffectDataResolver.ResolveCardEffect(built.Context, secondEffect);

            var secondResult = _ExecuteCommands(built.Context, secondCommands);

            Assert.That(secondResult.Actions.OfType<ApplyCardFormOverrideResultAction>().Count(), Is.EqualTo(1));
            Assert.That(secondResult.Events.OfType<CardFormChangedEvent>(), Is.Empty);
            Assert.That(built.Card.OverrideFormState.TryGetValue(out var secondState), Is.True);
            Assert.That(secondState.Identity, Is.Not.EqualTo(firstState.Identity));
            Assert.That(secondState.BuffLayerHandle, Is.Not.SameAs(firstState.BuffLayerHandle));
        }

        [Test]
        public void ExecuteFormOverride_ToCardWithDrawedEffect_DoesNotDispatchDrawed()
        {
            const string drawedOverrideCardId = "form-change-not-draw";
            var overrideCardData = new OverrideCardData
            {
                ID = drawedOverrideCardId,
                Cost = 4,
                Power = 7
            };
            overrideCardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new GainEnergyEffect
                    {
                        Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                        Value = new ConstInteger { Value = 1 }
                    }
                }
            };
            var built = new CardTransformationTestBuilder()
                .WithCard(overrideCardData)
                .Build();
            using var currentPlayerScope = built.Gameplay.Status.SetCurrentPlayer(built.Gameplay.Ally);
            built.Gameplay.Ally.CardManager.HandCard.AddCard(built.Card);
            var context = new TriggerContext(
                built.Gameplay.Manager,
                new CardTrigger(built.Card),
                new DrawCardIntentTargetAction(
                    SystemSource.Instance,
                    new PlayerTarget(built.Gameplay.Ally)));
            var effect = _CreateEffect(built.Card, "not-draw", drawedOverrideCardId);
            var commandSet = EffectDataResolver.ResolveCardEffect(context, effect);

            var result = _ExecuteCommands(context, commandSet);

            Assert.That(built.Card.CardDataId, Is.EqualTo(drawedOverrideCardId));
            Assert.That(built.Gameplay.Ally.CurrentEnergy, Is.Zero);
            Assert.That(result.Actions.OfType<ApplyCardFormOverrideResultAction>().Count(), Is.EqualTo(1));
            Assert.That(result.Actions.OfType<GainEnergyResultAction>(), Is.Empty);
            Assert.That(result.Events.OfType<CardFormChangedEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<GainEnergyEvent>(), Is.Empty);
        }

        private static BuiltCardTransformationTest _Build()
        {
            return new CardTransformationTestBuilder()
                .WithCard(new OverrideCardData
                {
                    ID = OverrideCardId,
                    Cost = 6,
                    Power = 10
                })
                .Build();
        }

        private static EffectResult _ExecuteCommands(
            TriggerContext context,
            EffectCommandSet commands)
        {
            var runner = new EffectQueueRunner();
            runner.EnqueueCommands(context, commands);
            return runner.RunToCompletion();
        }

        private static ApplyCardFormOverrideEffect _CreateEffect(
            ICardEntity target,
            string overrideKey,
            string targetCardDataId)
        {
            return new ApplyCardFormOverrideEffect
            {
                TargetCards = new FixedCardCollection(target),
                OverrideKey = overrideKey,
                TargetCardDataId = targetCardDataId,
                ReleaseRules =
                {
                    new CardFormOverrideReleaseRule
                    {
                        Timing = GameTiming.AfterTurnEnd,
                        Conditions = { new ConstCondition { Value = true } }
                    }
                },
                ReactionSessions =
                {
                    ["counter"] = new SessionInteger
                    {
                        InitialValue = 3,
                        LifeTime = SessionLifeTime.WholeGame
                    }
                }
            };
        }

        private sealed class FixedCardCollection : ITargetCardCollectionValue
        {
            private readonly ICardEntity _card;

            public FixedCardCollection(ICardEntity card)
            {
                _card = card;
            }

            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext triggerContext)
            {
                return new[] { _card };
            }
        }
    }
}
