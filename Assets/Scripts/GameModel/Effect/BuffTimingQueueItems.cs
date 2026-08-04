using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{

    internal sealed record CharacterBuffReactionCandidate(
        ICharacterEntity Character,
        ICharacterBuffEntity Buff);

    internal sealed record CardBuffReactionCandidate(
        ICardEntity Card,
        ICardBuffEntity Buff);

    internal sealed record TimingReactionSnapshot(
        UpdateTimingAction Action,
        IReadOnlyList<IPlayerBuffEntity> PlayerBuffs,
        IReadOnlyList<CharacterBuffReactionCandidate> CharacterBuffs,
        IReadOnlyList<CardBuffReactionCandidate> CardBuffs,
        IReadOnlyList<ICardEntity> Cards);

    internal sealed record TimingDispatchPlan(
        IReadOnlyList<EffectQueueItem> GeneralReactionItems,
        IReadOnlyList<EffectQueueItem> FormTransitionItems)
    {
        public IReadOnlyList<EffectQueueItem> OrderedItems => GeneralReactionItems
            .Concat(FormTransitionItems)
            .ToArray();
    }

    internal sealed record TriggerTimingQueueItem(
        GameplayManager Manager,
        GameTiming Timing,
        IActionSource Source) : EffectQueueItem((TriggerContext)null)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var snapshot = Manager.CreateTimingReactionSnapshot(Timing, Source);
            var events = new List<IGameEvent>(Manager.ObserveAction(snapshot.Action));
            var items = TimingDispatchPlanner.Create(Manager, snapshot).OrderedItems;
            for (var i = items.Count - 1; i >= 0; i--)
            {
                queue.EnqueueImmediate(items[i]);
            }

            return new EffectResult(Array.Empty<BaseResultAction>(), events);
        }
    }

    internal sealed record TriggeredPlayerBuffEffectQueueItem(
        GameplayManager Manager,
        TriggerContext Context,
        IPlayerBuffEffect Effect,
        IActionSource TriggerBuffSource) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            queue.EnqueueImmediate(new EffectQueueItem[]
            {
                new TriggerTimingQueueItem(Manager, GameTiming.BeforeTriggerBuffEffect, TriggerBuffSource),
                new PlayerBuffEffectExecutionQueueItem(Context, Effect),
                new TriggerTimingQueueItem(Manager, GameTiming.AfterTriggerBuffEffect, TriggerBuffSource)
            });
            return EffectResult.Empty;
        }
    }

    internal sealed record TriggeredCharacterBuffEffectQueueItem(
        GameplayManager Manager,
        ICharacterEntity SelectedCharacter,
        TriggerContext Context,
        ICharacterBuffEffect Effect,
        IActionSource TriggerBuffSource) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            queue.EnqueueImmediate(new EffectQueueItem[]
            {
                new CharacterTimingQueueItem(Manager, SelectedCharacter, GameTiming.BeforeTriggerBuffEffect, TriggerBuffSource),
                new CharacterBuffEffectExecutionQueueItem(Manager, SelectedCharacter, Context, Effect),
                new CharacterTimingQueueItem(Manager, SelectedCharacter, GameTiming.AfterTriggerBuffEffect, TriggerBuffSource)
            });
            return EffectResult.Empty;
        }
    }

    internal sealed record TriggeredCardBuffEffectQueueItem(
        GameplayManager Manager,
        ICardEntity SelectedCard,
        TriggerContext Context,
        ICardBuffEffect Effect,
        IActionSource TriggerBuffSource) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            queue.EnqueueImmediate(new EffectQueueItem[]
            {
                new CardTimingQueueItem(Manager, SelectedCard, GameTiming.BeforeTriggerBuffEffect, TriggerBuffSource),
                new CardBuffEffectExecutionQueueItem(Manager, SelectedCard, Context, Effect),
                new CardTimingQueueItem(Manager, SelectedCard, GameTiming.AfterTriggerBuffEffect, TriggerBuffSource)
            });
            return EffectResult.Empty;
        }
    }

    internal sealed record PlayerBuffEffectExecutionQueueItem(
        TriggerContext Context,
        IPlayerBuffEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            var commands = EffectDataResolver.ResolvePlayerBuffEffect(Context, Effect);
            return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
        }
    }

    internal sealed record CharacterBuffEffectExecutionQueueItem(
        GameplayManager Manager,
        ICharacterEntity SelectedCharacter,
        TriggerContext Context,
        ICharacterBuffEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            using var characterContext = Manager.EffectQueueContextManager.SetSelectedCharacter(SelectedCharacter.Some());
            var commands = EffectDataResolver.ResolveCharacterBuffEffect(Context, Effect);
            return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
        }
    }

    internal sealed record CardBuffEffectExecutionQueueItem(
        GameplayManager Manager,
        ICardEntity SelectedCard,
        TriggerContext Context,
        ICardBuffEffect Effect) : EffectQueueItem(Context)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            using var cardContext = Manager.EffectQueueContextManager.SetSelectedCard(SelectedCard.Some());
            var commands = EffectDataResolver.ResolveCardBuffEffect(Context, Effect);
            return EffectCommandExecutor.ApplyEffectCommands(Context, commands);
        }
    }

    internal sealed record CharacterTimingQueueItem(
        GameplayManager Manager,
        ICharacterEntity SelectedCharacter,
        GameTiming Timing,
        IActionSource Source) : EffectQueueItem((TriggerContext)null)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            using var characterContext = Manager.EffectQueueContextManager.SetSelectedCharacter(SelectedCharacter.Some());
            return new TriggerTimingQueueItem(Manager, Timing, Source).Execute(queue);
        }
    }

    internal sealed record CardTimingQueueItem(
        GameplayManager Manager,
        ICardEntity SelectedCard,
        GameTiming Timing,
        IActionSource Source) : EffectQueueItem((TriggerContext)null)
    {
        public override EffectResult Execute(IEffectQueueContext queue)
        {
            using var cardContext = Manager.EffectQueueContextManager.SetSelectedCard(SelectedCard.Some());
            return new TriggerTimingQueueItem(Manager, Timing, Source).Execute(queue);
        }
    }

}
