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
                CreateOverrideReleaseQueueItems(manager, snapshot)
                    .Concat(CreateSelfTransformQueueItems(manager, contextManager, snapshot))
                    .ToArray());
        }

        private static IReadOnlyList<EffectQueueItem> CreateGeneralReactionQueueItems(
            GameplayManager manager,
            IGameContextManager contextManager,
            TimingReactionSnapshot snapshot)
        {
            return snapshot.PlayerBuffs
                .SelectMany(candidate => CreatePlayerBuffQueueItems(
                    manager,
                    contextManager,
                    candidate.Player,
                    candidate.Buff,
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
            var overrideCardIdentities = snapshot.CardFormOverrides
                .Select(candidate => candidate.Card.Identity)
                .ToHashSet();
            return snapshot.Cards
                .Where(card => !overrideCardIdentities.Contains(card.Identity))
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

        private static IReadOnlyList<EffectQueueItem> CreateOverrideReleaseQueueItems(
            GameplayManager manager,
            TimingReactionSnapshot snapshot)
        {
            return snapshot.CardFormOverrides
                .Where(candidate => ShouldReleaseOverride(
                    manager,
                    candidate,
                    snapshot.Action))
                .Select(candidate => new RemoveCardFormOverrideQueueItem(
                    manager,
                    candidate.Card,
                    candidate.State,
                    snapshot.Action) as EffectQueueItem)
                .ToArray();

            static bool ShouldReleaseOverride(
                GameplayManager manager,
                CardFormOverrideReactionCandidate candidate,
                UpdateTimingAction timingAction)
            {
                var triggerContext = new TriggerContext(
                    manager,
                    new CardFormOverrideTrigger(candidate.Card, candidate.State),
                    timingAction);
                return candidate.State.ReleaseRules
                    .Where(rule => rule.Timing == timingAction.Timing)
                    .Any(rule => rule.Conditions.All(condition => condition.Eval(triggerContext)));
            }
        }

        private static IReadOnlyList<EffectQueueItem> CreatePlayerBuffQueueItems(
            GameplayManager manager,
            IGameContextManager contextManager,
            IPlayerEntity player,
            IPlayerBuffEntity buff,
            UpdateTimingAction timingAction)
        {
            var buffTrigger = new PlayerBuffTrigger(player, buff);
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
            var buffTrigger = new CharacterBuffTrigger(selectedCharacter, buff);
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
            var cardBuffTrigger = new CardBuffTrigger(selectedCard, buff);
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
                            buffTriggerContext,
                            conditionalEffect.Effect,
                            new CardBuffSource(buff)) as EffectQueueItem)
                    .ToArray())
                .ValueOr(Array.Empty<EffectQueueItem>());
        }
    }
}
