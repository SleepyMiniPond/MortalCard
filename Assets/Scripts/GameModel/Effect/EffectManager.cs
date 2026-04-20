using System.Collections.Generic;
using System.Linq;

public static class EffectManager
{
    public static EffectResult CreateNewDeckCard(
        IGameplayModel model,
        IActionSource source,
        IPlayerEntity player,
        IReadOnlyCollection<CardInstance> cardInstances)
    {
        var resultActions = new List<BaseResultAction>();
        var drawCardEvents = new List<IGameEvent>();

        foreach (var cardInstance in cardInstances)
        {
            var action = new CreateCardIntentTargetAction(source, new PlayerTarget(player));
            var context = new TriggerContext(model, new PlayerTrigger(player), action);
            var newCard = CardEntity.CreateFromInstance(
                cardInstance,
                model.ContextManager.CardLibrary);    
            var createCardCommand = new EffectCommandSet(
                new CreateCardEffectCommand(player, newCard, CardCollectionType.Deck).WrapAsEnumerable().ToArray());

            var createCardResult = EffectCommandExecutor.ApplyEffectCommands(context, createCardCommand);
            
            drawCardEvents.AddRange(createCardResult.Events);
            resultActions.AddRange(createCardResult.Actions);
        }

        return new EffectResult(resultActions.ToArray(), drawCardEvents.ToArray());
    }
    public static EffectResult DrawCards(
        IGameplayModel model,
        IActionSource source,
        IPlayerEntity player,
        int drawCount)
    {
        var drawAction = new DrawCardIntentTargetAction(source, new PlayerTarget(player));
        var context = new TriggerContext(model, new PlayerTrigger(player), drawAction);
        var drawCommand = new EffectCommandSet(
            new DrawCardEffectCommand(player, drawCount).WrapAsEnumerable().ToArray());

        var drawCardResult = EffectCommandExecutor.ApplyEffectCommands(context, drawCommand);

        return new EffectResult(drawCardResult.Actions.ToArray(), drawCardResult.Events.ToArray());
    }
    
    public static EffectResult RecycleCardOnPlayEnd(
        IGameplayModel model,
        IPlayerEntity player,
        ICardEntity card)
    {
        var drawCardEvents = new List<IGameEvent>();
        var resultActions = new List<BaseResultAction>();

        var recycleEvents = player.CardManager.RecycleCardOnPlayEnd(model, card);
        drawCardEvents.AddRange(recycleEvents);

        return new EffectResult(resultActions, drawCardEvents);
    }
}
