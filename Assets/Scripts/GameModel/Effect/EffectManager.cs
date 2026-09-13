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
            var effectQueueRunner = new EffectQueueRunner();

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

                effectQueueRunner.EnqueueCommands(context, createCardCommand);
            }

            return effectQueueRunner.RunToCompletion();
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

            var effectQueueRunner = new EffectQueueRunner();
            effectQueueRunner.EnqueueCommands(context, drawCommand);
            var drawCardResult = effectQueueRunner.RunToCompletion();

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
