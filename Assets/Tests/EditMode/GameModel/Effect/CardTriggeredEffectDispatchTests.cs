using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public sealed class CardTriggeredEffectDispatchTests
    {
        [Test]
        public void CreateItems_NoneTiming_ReturnsEmpty()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var context = new TriggerContext(
                built.Manager,
                new CardTrigger(card),
                new CardLookIntentAction(card));

            var items = CardTriggeredEffectDispatch.CreateItems(
                context,
                card,
                CardTriggeredTiming.None);

            Assert.That(items, Is.Empty);
        }

        [Test]
        public void CreateItems_UsesCardTriggeredTimingActionAndPreservesOriginTiming()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions =
                    {
                        new GameTimingCondition
                        {
                            Timing = GameTiming.BeforeTurnEnd
                        }
                    },
                    Effect = new DamageEffect()
                }
            };

            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var context = new TriggerContext(
                built.Manager,
                new CardTrigger(card),
                new UpdateTimingAction(
                    GameTiming.BeforeTurnEnd,
                    SystemSource.Instance));

            var items = CardTriggeredEffectDispatch.CreateItems(
                context,
                card,
                CardTriggeredTiming.Drawed);

            var cardEffectItem = items.OfType<CardTriggeredEffectQueueItem>().Single();
            var action = cardEffectItem.Context.Action as CardTriggeredTimingAction;
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Card, Is.SameAs(card));
            Assert.That(action.TriggeredTiming, Is.EqualTo(CardTriggeredTiming.Drawed));
            Assert.That(action.Source, Is.SameAs(SystemSource.Instance));
            Assert.That(
                cardEffectItem.Context.ReactionOriginTiming.ValueOr(GameTiming.None),
                Is.EqualTo(GameTiming.BeforeTurnEnd));
        }

        [Test]
        public void CreateItems_SnapshotsCardDataBeforeCardBuff()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.TriggeredEffects[CardTriggeredTiming.Drawed] = new[]
            {
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new DamageEffect()
                },
                new ConditionalCardEffect
                {
                    Conditions = { new ConstCondition { Value = false } },
                    Effect = new HealEffect()
                }
            };

            var cardBuffData = BuffTestBuilder.CreateCardBuffData(
                BuffTestBuilder.CardBuffId,
                GameTiming.AfterExecuteEnd,
                new ConditionalCardBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new DamageEffect()
                });
            cardBuffData.Effects = new Dictionary<CardTriggeredTiming, ConditionalCardBuffEffect[]>
            {
                [CardTriggeredTiming.Drawed] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Conditions = { new ConstCondition { Value = true } },
                        Effect = new HealEffect()
                    }
                }
            };

            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var createBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.AfterExecuteEnd, SystemSource.Instance));
            var buff = BuffTestBuilder.CreateCardBuff(
                createBuffContext,
                built.ContextManager.CardBuffLibrary);
            card.BuffManager.AddBuff(buff);

            var context = new TriggerContext(
                built.Manager,
                new CardTrigger(card),
                new CardLookIntentAction(card));

            var items = CardTriggeredEffectDispatch.CreateItems(
                context,
                card,
                CardTriggeredTiming.Drawed);

            Assert.That(items, Has.Length.EqualTo(2));
            Assert.That(items[0], Is.TypeOf<CardTriggeredEffectQueueItem>());
            Assert.That(items[1], Is.TypeOf<CardBuffEffectExecutionQueueItem>());
            Assert.That(
                ((CardBuffEffectExecutionQueueItem)items[1]).Context.Action,
                Is.TypeOf<CardTriggeredTimingAction>());

            card.BuffManager.RemoveBuff(buff);
            Assert.That(items, Has.Length.EqualTo(2));
        }
    }
}
