using System.Linq;
using MortalGame.GameData;
namespace MortalGame.GameModel
{

    public class MoveCardEffectCommandHandler : IEffectCommandHandler
    {
        public CommandApplyResult Handle(TriggerContext context, IEffectCommand command)
        {
            var c = (MoveCardEffectCommand)command;
            if (c.Target == null ||
                c.Card == null ||
                !c.Start.IsValidCardZone() ||
                !c.Destination.IsValidCardZone())
            {
                return CommandApplyResult.Empty;
            }

            // Queue 可能在建立命令後先執行另一個移牌；來源區域不再持有該卡時，
            // 此命令必須安靜略過，不得產生假的 Result／Event。
            var actualCard = c.Target.CardManager
                .GetCardCollectionZone(c.Start)
                .GetCardOrNone(card => card.Identity == c.Card.Identity);
            if (!actualCard.TryGetValue(out var cardInStartZone))
                return CommandApplyResult.Empty;

            var moveResult = c.Target.CardManager.MoveCard(
                cardInStartZone,
                c.Start,
                c.Destination);

            var resultAction = new MoveCardResultAction(
                context.Action.Source, new CardTarget(cardInStartZone), moveResult, c.MoveType);
            var reactorEvents = context.Model.ObserveDerivedAction(context, resultAction);
            var moveCardEvent = new MoveCardEvent(
                c.Target.Faction,
                moveResult.Card.Identity,
                c.Start,
                c.Destination,
                c.Target.CardManager.ToInfo());
            return new CommandApplyResult(resultAction.WrapAsEnumerable(), reactorEvents.Append(moveCardEvent));
        }
    }

}
