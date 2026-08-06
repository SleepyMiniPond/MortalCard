using System;
using System.Collections.Generic;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{
    internal static class TimingDispatchPlanner
    {
        internal static TimingDispatchPlan Create(
            GameplayManager manager,
            TimingReactionSnapshot snapshot)
        {
            var contextManager = manager.EffectQueueContextManager;
            return new TimingDispatchPlan(
                CreateGeneralReactionQueueItems(manager, contextManager, snapshot),
                CreateSelfTransformQueueItems(manager, contextManager, snapshot));
        }

        private static IReadOnlyList<EffectQueueItem> CreateGeneralReactionQueueItems(
            GameplayManager manager,
            IGameContextManager contextManager,
            TimingReactionSnapshot snapshot)
        {
            return snapshot.PlayerBuffs
                .SelectMany(buff => CreatePlayerBuffQueueItems(
                    manager,
                    contextManager,
                    buff,
                    snapshot.Action))
                .Concat(snapshot.CharacterBuffs.SelectMany(candidate =>
                    CreateCharacterBuffQueueItems(
                        manager,
                        contextManager,
                        candidate.Character,
                        candidate.Buff,
                        snapshot.Action)))
                .Concat(snapshot.CardBuffs.SelectMany(candidate =>
                    CreateCardBuffQueueItems(
                        manager,
                        contextManager,
                        candidate.Card,
                        candidate.Buff,
                        snapshot.Action)))
                .ToArray();
        }

        private static IReadOnlyList<EffectQueueItem> CreateSelfTransformQueueItems(
            GameplayManager manager,
            IGameContextManager contextManager,
            TimingReactionSnapshot snapshot)
        {
            return snapshot.Cards
                .Where(card => contextManager.CardLibrary
                    .GetStandardCardData(card.BaseCardDataId)
                    .TransformRules
                    .Any(rule => rule.Timing == snapshot.Action.Timing))
                .Select(card => new SelfTransformQueueItem(
                    manager,
                    card,
                    snapshot.Action) as EffectQueueItem)
                .ToArray();
        }

        private static IReadOnlyList<EffectQueueItem> CreatePlayerBuffQueueItems(
            GameplayManager manager,
            IGameContextManager contextManager,
            IPlayerBuffEntity buff,
            UpdateTimingAction timingAction)
        {
            var buffTrigger = new PlayerBuffTrigger(buff);
            var buffTriggerContext = new TriggerContext(manager, buffTrigger, timingAction);
            var conditionalEffectsOption = contextManager.PlayerBuffLibrary
                .GetBuffEffects(buff.PlayerBuffDataId, timingAction.Timing);
            return conditionalEffectsOption
                .Map(conditionalEffects => conditionalEffects
                    .Where(conditionalEffect => conditionalEffect.Conditions
                        .All(condition => condition.Eval(buffTriggerContext)))
                    .Select(conditionalEffect =>
                        new TriggeredPlayerBuffEffectQueueItem(
                            manager,
                            buffTriggerContext,
                            conditionalEffect.Effect,
                            new PlayerBuffSource(buff)) as EffectQueueItem)
                    .ToArray())
                .ValueOr(Array.Empty<EffectQueueItem>());
        }

        private static IReadOnlyList<EffectQueueItem> CreateCharacterBuffQueueItems(
            GameplayManager manager,
            IGameContextManager contextManager,
            ICharacterEntity selectedCharacter,
            ICharacterBuffEntity buff,
            UpdateTimingAction timingAction)
        {
            using var characterContext = contextManager
                .SetSelectedCharacter(selectedCharacter.Some());
            var buffTrigger = new CharacterBuffTrigger(buff);
            var buffTriggerContext = new TriggerContext(manager, buffTrigger, timingAction);
            var conditionalEffectsOption = contextManager.CharacterBuffLibrary
                .GetBuffEffects(buff.CharacterBuffDataId, timingAction.Timing);
            return conditionalEffectsOption
                .Map(conditionalEffects => conditionalEffects
                    .Where(conditionalEffect => conditionalEffect.Conditions
                        .All(condition => condition.Eval(buffTriggerContext)))
                    .Select(conditionalEffect =>
                        new TriggeredCharacterBuffEffectQueueItem(
                            manager,
                            selectedCharacter,
                            buffTriggerContext,
                            conditionalEffect.Effect,
                            new CharacterBuffSource(buff)) as EffectQueueItem)
                    .ToArray())
                .ValueOr(Array.Empty<EffectQueueItem>());
        }

        private static IReadOnlyList<EffectQueueItem> CreateCardBuffQueueItems(
            GameplayManager manager,
            IGameContextManager contextManager,
            ICardEntity selectedCard,
            ICardBuffEntity buff,
            UpdateTimingAction timingAction)
        {
            using var cardContext = contextManager.SetSelectedCard(selectedCard.Some());
            var cardBuffTrigger = new CardBuffTrigger(buff);
            var buffTriggerContext = new TriggerContext(manager, cardBuffTrigger, timingAction);
            var conditionalEffectsOption = contextManager.CardBuffLibrary
                .GetBuffEffects(buff.CardBuffDataID, timingAction.Timing);
            return conditionalEffectsOption
                .Map(conditionalEffects => conditionalEffects
                    .Where(conditionalEffect => conditionalEffect.Conditions
                        .All(condition => condition.Eval(buffTriggerContext)))
                    .Select(conditionalEffect =>
                        new TriggeredCardBuffEffectQueueItem(
                            manager,
                            selectedCard,
                            buffTriggerContext,
                            conditionalEffect.Effect,
                            new CardBuffSource(buff)) as EffectQueueItem)
                    .ToArray())
                .ValueOr(Array.Empty<EffectQueueItem>());
        }
    }
}
