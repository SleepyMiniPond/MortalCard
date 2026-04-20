using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using UnityEngine;

public record EffectResult(
    IReadOnlyCollection<BaseResultAction> Actions,
    IReadOnlyCollection<IGameEvent> Events);

public static class EffectCommandExecutor
{    
    public record CommandApplyResult(
        IEnumerable<BaseResultAction> Actions,
        IEnumerable<IGameEvent> Events);
        
    public static EffectResult ApplyEffectCommands(
        TriggerContext context,
        EffectCommandSet effectCommandSet)
    {
        var actionList = new List<BaseResultAction>();
        var eventList = new List<IGameEvent>();

        foreach (var command in effectCommandSet.Commands)
        {
            var commandApplyResult = command switch
            {
                DamageEffectCommand damageCommand => 
                    _ApplyDamageEffectCommand(context, damageCommand),
                
                HealEffectCommand healCommand => 
                    _ApplyHealEffectCommand(context, healCommand),

                ShieldEffectCommand shieldCommand => 
                    _ApplyShieldEffectCommand(context, shieldCommand),
                
                GainEnergyEffectCommand gainEnergyCommand => 
                    _ApplyGainEnergyEffectCommand(context, gainEnergyCommand),
                LoseEnergyEffectCommand loseEnergyCommand => 
                    _ApplyLoseEnergyEffectCommand(context, loseEnergyCommand),
                
                IncreaseDispositionEffectCommand increaseDispositionCommand => 
                    _ApplyIncreaseDispositionEffectCommand(context, increaseDispositionCommand),
                DecreaseDispositionEffectCommand decreaseDispositionCommand => 
                    _ApplyDecreaseDispositionEffectCommand(context, decreaseDispositionCommand),
                
                AddPlayerBuffEffectCommand addPlayerBuffCommand =>
                    _ApplyAddPlayerBuffEffectCommand(context, addPlayerBuffCommand),
                RemovePlayerBuffEffectCommand removePlayerBuffCommand =>
                    _ApplyRemovePlayerBuffEffectCommand(context, removePlayerBuffCommand),
                ModifyPlayerBuffLevelEffectCommand modifyPlayerBuffLevelCommand =>
                    _ApplyModifyPlayerBuffLevelEffectCommand(context, modifyPlayerBuffLevelCommand),
                
                DrawCardEffectCommand drawCardCommand =>
                    _ApplyDrawCardEffectCommand(context, drawCardCommand),
                MoveCardEffectCommand moveCardCommand =>
                    _ApplyMoveCardEffectCommand(context, moveCardCommand),
                
                CreateCardEffectCommand createCardCommand =>
                    _ApplyCreateCardEffectCommand(context, createCardCommand),
                CloneCardEffectCommand cloneCardCommand =>
                    _ApplyCloneCardEffectCommand(context, cloneCardCommand),
                
                AddCardBuffEffectCommand addCardBuffCommand =>
                    _ApplyAddCardBuffEffectCommand(context, addCardBuffCommand),
                RemoveCardBuffEffectCommand removeCardBuffCommand =>
                    _ApplyRemoveCardBuffEffectCommand(context, removeCardBuffCommand),
                ModifyCardBuffLevelEffectCommand modifyCardBuffLevelCommand =>
                    _ApplyModifyCardBuffLevelEffectCommand(context, modifyCardBuffLevelCommand),
                
                ModifyCardAttributeEffectCommand modifyCardAttributeCommand =>
                    _ApplyModifyCardAttributeEffectCommand(context, modifyCardAttributeCommand),

                _ => throw new InvalidOperationException($"Unknown effect command: {command.GetType().Name}"),
            };

            actionList.AddRange(commandApplyResult.Actions);
            eventList.AddRange(commandApplyResult.Events);
        }

        return new EffectResult(actionList, eventList);
    }

    private static CommandApplyResult _ApplyDamageEffectCommand(
        TriggerContext context,
        DamageEffectCommand command)
    {        
        var damageResult = command.Target.HealthManager
            .TakeDamage(
                command.DamagePoint, 
                context.Model.ContextManager.Context, 
                command.DamageType);

        var damageResultAction = new DamageResultAction(context.Action.Source, new CharacterTarget(command.Target), damageResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(damageResultAction);
        var damageEvent = new DamageEvent(command.Target.Faction(context.Model), command.Target, damageResult);
        return new CommandApplyResult(damageResultAction.WrapAsEnumerable(), reactorEvents.Append(damageEvent));
    }

    private static CommandApplyResult _ApplyHealEffectCommand(
        TriggerContext context,
        HealEffectCommand command)
    {        
        var healResult = command.Target.HealthManager
            .GetHeal(
                command.HealPoint, 
                context.Model.ContextManager.Context);

        var healResultAction = new HealResultAction(context.Action.Source, new CharacterTarget(command.Target), healResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(healResultAction);
        var healEvent = new GetHealEvent(command.Target.Faction(context.Model), command.Target, healResult);
        return new CommandApplyResult(healResultAction.WrapAsEnumerable(), reactorEvents.Append(healEvent));
    }

    private static CommandApplyResult _ApplyShieldEffectCommand(
        TriggerContext context,
        ShieldEffectCommand command)
    {        
        var shieldResult = command.Target.HealthManager
            .GetShield(
                command.ShieldPoint, 
                context.Model.ContextManager.Context);

        var shieldResultAction = new ShieldResultAction(context.Action.Source, new CharacterTarget(command.Target), shieldResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(shieldResultAction);
        var shieldEvent = new GetShieldEvent(command.Target.Faction(context.Model), command.Target, shieldResult);
        return new CommandApplyResult(shieldResultAction.WrapAsEnumerable(), reactorEvents.Append(shieldEvent));
    }

    private static CommandApplyResult _ApplyGainEnergyEffectCommand(
        TriggerContext context,
        GainEnergyEffectCommand command)
    {
        var gainEnergyResult = command.Target.EnergyManager.GainEnergy(command.EnergyPoint);

        var gainEnergyResultAction = new GainEnergyResultAction(context.Action.Source, new PlayerTarget(command.Target), gainEnergyResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(gainEnergyResultAction);
        var gainEnergyEvent = new GainEnergyEvent(command.Target.Faction, command.Target.EnergyManager.ToInfo(), gainEnergyResult);
        return new CommandApplyResult(gainEnergyResultAction.WrapAsEnumerable(), reactorEvents.Append(gainEnergyEvent));
    }

    private static CommandApplyResult _ApplyLoseEnergyEffectCommand(
        TriggerContext context,
        LoseEnergyEffectCommand command)
    {
        var loseEnergyResult = command.Target.EnergyManager.LoseEnergy(command.EnergyPoint);

        var loseEnergyResultAction = new LoseEnergyResultAction(context.Action.Source, new PlayerTarget(command.Target), loseEnergyResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(loseEnergyResultAction);
        var loseEnergyEvent = new LoseEnergyEvent(command.Target.Faction, command.Target.EnergyManager.ToInfo(), loseEnergyResult);
        return new CommandApplyResult(loseEnergyResultAction.WrapAsEnumerable(), reactorEvents.Append(loseEnergyEvent));
    }
    
    private static CommandApplyResult _ApplyIncreaseDispositionEffectCommand(
        TriggerContext context,
        IncreaseDispositionEffectCommand command)
    {
        var increaseDispositionResult = command.Target.DispositionManager.IncreaseDisposition(command.DispositionPoint);

        var increaseDispositionResultAction = new IncreaseDispositionResultAction(context.Action.Source, new PlayerTarget(command.Target), increaseDispositionResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(increaseDispositionResultAction);
        var increaseDispositionEvent = new IncreaseDispositionEvent(command.Target.DispositionManager.ToInfo(), increaseDispositionResult.DeltaDisposition);
        return new CommandApplyResult(increaseDispositionResultAction.WrapAsEnumerable(), reactorEvents.Append(increaseDispositionEvent));
    }

    private static CommandApplyResult _ApplyDecreaseDispositionEffectCommand(
        TriggerContext context,
        DecreaseDispositionEffectCommand command)
    {
        var decreaseDispositionResult = command.Target.DispositionManager.DecreaseDisposition(command.DispositionPoint);

        var decreaseDispositionResultAction = new DecreaseDispositionResultAction(context.Action.Source, new PlayerTarget(command.Target), decreaseDispositionResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(decreaseDispositionResultAction);
        var decreaseDispositionEvent = new DecreaseDispositionEvent(command.Target.DispositionManager.ToInfo(), decreaseDispositionResult.DeltaDisposition);
        return new CommandApplyResult(decreaseDispositionResultAction.WrapAsEnumerable(), reactorEvents.Append(decreaseDispositionEvent));
    }

    private static CommandApplyResult _ApplyAddPlayerBuffEffectCommand(
        TriggerContext context,
        AddPlayerBuffEffectCommand command)
    {        
        var playerTarget = new PlayerTarget(command.Target);
        var addResult = command.Target.BuffManager.AddBuff(command.NewBuff);

        var addPlayerBuffResultAction = new AddPlayerBuffResultAction(context.Action.Source, playerTarget, addResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(addPlayerBuffResultAction);        
        var buffEvent = new AddPlayerBuffEvent(command.Target.Faction, addResult.PlayerBuff.ToInfo(context.Model));
        return new CommandApplyResult(addPlayerBuffResultAction.WrapAsEnumerable(), reactorEvents.Append(buffEvent));
    }
    private static CommandApplyResult _ApplyRemovePlayerBuffEffectCommand(
        TriggerContext context,
        RemovePlayerBuffEffectCommand command)
    {
        var playerTarget = new PlayerTarget(command.Target);
        var removeResult = command.Target.BuffManager.RemoveBuff(command.ExistBuff);

        var removePlayerBuffResultAction = new RemovePlayerBuffResultAction(context.Action.Source, playerTarget, removeResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(removePlayerBuffResultAction);
        var buffEvent = new RemovePlayerBuffEvent(command.Target.Faction, command.ExistBuff.ToInfo(context.Model));
        return new CommandApplyResult(removePlayerBuffResultAction.WrapAsEnumerable(), reactorEvents.Append(buffEvent));
    }
    private static CommandApplyResult _ApplyModifyPlayerBuffLevelEffectCommand(
        TriggerContext context,
        ModifyPlayerBuffLevelEffectCommand command)
    {
        var playerTarget = new PlayerTarget(command.Target);
        var modifyResult = command.Target.BuffManager.ModifyBuffLevel(
            command.BuffId,
            command.Level);

        var modifyPlayerBuffLevelResultAction = new ModifyPlayerBuffLevelResultAction(context.Action.Source, playerTarget, modifyResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(modifyPlayerBuffLevelResultAction);
        var buffEvent = new ModifyPlayerBuffLevelEvent(
            command.Target.Faction, 
            command.Target.BuffManager.Buffs.First(b => b.PlayerBuffDataId == command.BuffId).ToInfo(context.Model));
        return new CommandApplyResult(modifyPlayerBuffLevelResultAction.WrapAsEnumerable(), reactorEvents.Append(buffEvent));
    }

    private static CommandApplyResult _ApplyDrawCardEffectCommand(
        TriggerContext context,
        DrawCardEffectCommand command)
    {
        var resultActions = new List<BaseResultAction>();
        var drawCardEvents = new List<IGameEvent>();
        
        var cardManager = command.Target.CardManager;
        for(var i = 0; i < command.DrawCount; i++)
        {
            if (cardManager.Deck.Cards.Count == 0 &&
                cardManager.Graveyard.Cards.Count > 0)
            {
                var graveyardCards = cardManager.Graveyard.PopAllCards();
                cardManager.Deck.EnqueueCardsThenShuffle(graveyardCards);

                var recycleDeckResultAction = new RecycleDeckResultAction(new PlayerTarget(command.Target));
                var reactorEvents = context.Model.UpdateReactorSessionAction(recycleDeckResultAction);
                var recycleEvent = new RecycleGraveyardToDeckEvent(
                    Faction: command.Target.Faction,
                    CardManagerInfo: cardManager.ToInfo(context.Model)
                );

                resultActions.Add(recycleDeckResultAction);
                drawCardEvents.AddRange(reactorEvents);
                drawCardEvents.Add(recycleEvent);
            }

            if (cardManager.Deck.PopCardOrNone().TryGetValue(out var drawCard))
            {
                cardManager.HandCard.AddCard(drawCard);                

                var drawCardResultAction = new DrawCardResultAction(context.Action.Source, new PlayerTarget(command.Target), drawCard);
                var reactorEvents = context.Model.UpdateReactorSessionAction(drawCardResultAction);
                var drawCardEvent = new DrawCardEvent(
                    command.Target.Faction, 
                    drawCard.ToInfo(context.Model),            
                    command.Target.CardManager.ToInfo(context.Model));

                resultActions.Add(drawCardResultAction);
                drawCardEvents.AddRange(reactorEvents);
                drawCardEvents.Add(drawCardEvent);
            }
        }
        return new CommandApplyResult(resultActions, drawCardEvents);
    }

    private static CommandApplyResult _ApplyMoveCardEffectCommand(
        TriggerContext context,
        MoveCardEffectCommand command)
    {
        var moveCardResult = command.Target.CardManager.MoveCard(
            command.Card,
            command.Start,
            command.Destination);

        var moveCardResultAction = new MoveCardResultAction(
            context.Action.Source, new CardTarget(command.Card), moveCardResult, command.MoveType);
        var reactorEvents = context.Model.UpdateReactorSessionAction(moveCardResultAction);
        var moveCardEvent = new MoveCardEvent(
            command.Target.Faction, 
            moveCardResult.Card.ToInfo(context.Model), 
            command.Start,
            command.Destination,
            command.Target.CardManager.ToInfo(context.Model));
        return new CommandApplyResult(moveCardResultAction.WrapAsEnumerable(), reactorEvents.Append(moveCardEvent));
    }

    private static CommandApplyResult _ApplyCreateCardEffectCommand(
        TriggerContext context,
        CreateCardEffectCommand command)
    {        
        var createResult = command.Target.CardManager.CreateNewCard(
            command.NewCard,
            command.Destination);

        var createCardResultAction = new CreateCardResultAction(
            context.Action.Source, new PlayerTarget(command.Target), createResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(createCardResultAction);
        var createCardEvent = new AddCardEvent(
            command.Target.Faction, 
            createResult.Card.ToInfo(context.Model),
            command.Destination,
            command.Target.CardManager.ToInfo(context.Model));
        return new CommandApplyResult(createCardResultAction.WrapAsEnumerable(), reactorEvents.Append(createCardEvent));
    }
    
    private static CommandApplyResult _ApplyCloneCardEffectCommand(
        TriggerContext context,
        CloneCardEffectCommand command)
    {        
        var cloneResult = command.Target.CardManager.CreateNewCard(
            command.ClonedCard,
            command.Destination);

        var cloneCardResultAction = new CloneCardResultAction(
            context.Action.Source, new PlayerAndCardTarget(command.Target, command.ClonedCard), command.OriginCard, cloneResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(cloneCardResultAction);
        var cloneCardEvent = new AddCardEvent(
            command.Target.Faction, 
            cloneResult.Card.ToInfo(context.Model),
            command.Destination,
            command.Target.CardManager.ToInfo(context.Model));
        return new CommandApplyResult(cloneCardResultAction.WrapAsEnumerable(), reactorEvents.Append(cloneCardEvent));
    }

    private static CommandApplyResult _ApplyAddCardBuffEffectCommand(
        TriggerContext context,
        AddCardBuffEffectCommand command)
    {
        var cardTarget = new CardTarget(command.Target);

        var addBuffResult = command.Target.BuffManager.AddBuff(command.NewBuff);

        var addCardBuffResultAction = new AddCardBuffResultAction(context.Action.Source, cardTarget, addBuffResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(addCardBuffResultAction);
        var cardBuffEvent = new AddCardBuffEvent(command.Target.Faction(context.Model), command.Target.ToInfo(context.Model));
        return new CommandApplyResult(addCardBuffResultAction.WrapAsEnumerable(), reactorEvents.Append(cardBuffEvent));
    }
    private static CommandApplyResult _ApplyRemoveCardBuffEffectCommand(
        TriggerContext context,
        RemoveCardBuffEffectCommand command)
    {
        var cardTarget = new CardTarget(command.Target);
        
        var removeBuffResult = command.Target.BuffManager.RemoveBuff(command.ExistBuff);

        var removeCardBuffResultAction = new RemoveCardBuffResultAction(context.Action.Source, cardTarget, removeBuffResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(removeCardBuffResultAction);
        var cardBuffEvent = new RemoveCardBuffEvent(command.Target.Faction(context.Model), command.Target.ToInfo(context.Model));
        return new CommandApplyResult(removeCardBuffResultAction.WrapAsEnumerable(), reactorEvents.Append(cardBuffEvent));
    }
    private static CommandApplyResult _ApplyModifyCardBuffLevelEffectCommand(
        TriggerContext context,
        ModifyCardBuffLevelEffectCommand command)
    {
        var cardTarget = new CardTarget(command.Target);
        var modifyBuffResult = command.Target.BuffManager.ModifyBuffLevel(
            command.BuffId,
            command.Level);

        var modifyCardBuffLevelResultAction = new ModifyCardBuffLevelResultAction(context.Action.Source, cardTarget, modifyBuffResult);
        var reactorEvents = context.Model.UpdateReactorSessionAction(modifyCardBuffLevelResultAction);
        var cardBuffEvent = new ModifyCardBuffLevelEvent(command.Target.Faction(context.Model), command.Target.ToInfo(context.Model));
        return new CommandApplyResult(modifyCardBuffLevelResultAction.WrapAsEnumerable(), reactorEvents.Append(cardBuffEvent));
    }

    private static CommandApplyResult _ApplyModifyCardAttributeEffectCommand(
        TriggerContext context,
        ModifyCardAttributeEffectCommand command)
    {
        if (context.Action.Source is CardPlaySource cardPlaySource)
        {
            var cardPlayAttribute = cardPlaySource.Attribute;
            cardPlayAttribute.ApplyModify(command.AdditionType, command.AdditionValue);
        }

        return new CommandApplyResult(Array.Empty<BaseResultAction>(), Enumerable.Empty<IGameEvent>());
    }
}
