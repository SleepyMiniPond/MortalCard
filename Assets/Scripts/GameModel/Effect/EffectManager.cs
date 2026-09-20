using System.Collections.Generic;
using MortalGame.GameData;
using System.Linq;

namespace MortalGame.GameModel
{

    public static class EffectManager
    {
        public static EffectResult CreateNewDeckCard(
            IGameplayModel model,
            IActionSource source,
            IPlayerEntity player,
            IReadOnlyCollection<CardInstance> cardInstances)
        {
            var items = new List<EffectQueueItem>();

            foreach (var cardInstance in cardInstances)
            {
                var action = new CreateCardIntentTargetAction(source, new PlayerTarget(player));
                var context = new TriggerContext(model, new PlayerTrigger(player), action);
                var newCard = CardEntity.CreateFromInstance(
                    cardInstance,
                    model.ContextManager.CardLibrary,
                    model.ContextManager.CardPropertyEntityFactory);
                var createCardCommand = new EffectCommandSet(
                    new CreateCardEffectCommand(player, newCard, CardCollectionType.Deck).WrapAsEnumerable().ToArray());

                items.AddRange(createCardCommand.Commands.Select(command => new EffectCommandQueueItem(context, command)));
            }

            return model.RunEffectBatch(items);
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
                new DrawCardEffectCommand(player, drawCount, true)
                    .WrapAsEnumerable()
                    .ToArray());

            var drawCardResult = model.RunEffectBatch(drawCommand.Commands
                .Select(command => new EffectCommandQueueItem(context, command)));

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

}
