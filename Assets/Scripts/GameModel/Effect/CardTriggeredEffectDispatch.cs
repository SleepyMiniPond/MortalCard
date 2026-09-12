using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    internal static class CardTriggeredEffectDispatch
    {
        internal static IReadOnlyList<EffectQueueItem> CreateItems(
            TriggerContext context,
            ICardEntity card,
            CardTriggeredTiming timing)
        {
            if (timing == CardTriggeredTiming.None)
            {
                return Array.Empty<EffectQueueItem>();
            }

            var cardContext = context with
            {
                Triggered = new CardTrigger(card),
                Action = new CardTriggeredTimingAction(
                    card,
                    timing,
                    context.Action.Source)
            };

            var cardDataItems = card.TriggeredEffects
                .TryGetValue(timing, out var cardDataEffects)
                ? cardDataEffects
                    .Where(conditionalEffect => conditionalEffect != null &&
                        (conditionalEffect.Conditions ?? new List<ICondition>()).All(
                            condition => condition != null && condition.Eval(cardContext)))
                    .Where(conditionalEffect => conditionalEffect.Effect != null)
                    .Select(conditionalEffect => new CardTriggeredEffectQueueItem(
                        cardContext,
                        conditionalEffect.Effect) as EffectQueueItem)
                : Enumerable.Empty<EffectQueueItem>();

            var cardBuffItems = card.BuffManager.Buffs
                .SelectMany(buff => _CreateCardBuffItems(
                    cardContext,
                    card,
                    buff,
                    timing));

            return cardDataItems
                .Concat(cardBuffItems)
                .ToArray();
        }

        private static IEnumerable<EffectQueueItem> _CreateCardBuffItems(
            TriggerContext context,
            ICardEntity card,
            ICardBuffEntity buff,
            CardTriggeredTiming timing)
        {
            var buffContext = context with
            {
                Triggered = new CardBuffTrigger(card, buff)
            };

            if (!buff.Effects.TryGetValue(timing, out var conditionalEffects))
            {
                return Enumerable.Empty<EffectQueueItem>();
            }

            return conditionalEffects
                .Where(conditionalEffect => conditionalEffect.Conditions.All(
                    condition => condition.Eval(buffContext)))
                .Select(conditionalEffect =>
                    new CardBuffEffectExecutionQueueItem(
                        buffContext,
                        conditionalEffect.Effect) as EffectQueueItem)
                .ToArray();
        }
    }
}
