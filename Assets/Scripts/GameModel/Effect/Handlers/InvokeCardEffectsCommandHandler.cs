using System.Linq;
using MortalGame.GameData;
using Optional;

namespace MortalGame.GameModel
{
    public sealed class InvokeCardEffectsCommandHandler : IEffectCommandHandler
    {
        public CommandApplyResult Handle(TriggerContext context, IEffectCommand command, IEffectQueueContext queue)
        {
            var request = ((InvokeCardEffectsCommand)command).Request;
            var model = context.Model;
            if (request.OriginalZone is not (CardCollectionType.HandCard or CardCollectionType.Deck or CardCollectionType.Graveyard) ||
                !model.GameStatus.GetPlayer(request.OwnerIdentity).TryGetValue(out var owner) ||
                !model.GameStatus.GetPlayer(request.CasterIdentity).TryGetValue(out var caster) ||
                !owner.CardManager.GetCardCollectionZone(request.OriginalZone)
                    .GetCardOrNone(card => card.Identity == request.CardIdentity).TryGetValue(out var card))
                return CommandApplyResult.Empty;

            // 呼叫各自選取，不讓外層選取殘留到沒有主目標的卡片。
            using (model.ContextManager.SetContext(GameContext.EMPTY))
            {
                if (!SelectTargetLogic.SelectTargets(model, card, caster).TryGetValue(out var selection) ||
                    !SelectionInfoUtility.TryCreateCardEffectContext(
                            model, 
                            card, 
                            selection.MainSelectionAction,
                            selection.SubSelectionActions,
                            caster)
                        .TryGetValue(out var selectedContext))
                    return CommandApplyResult.Empty;

                var invokeContext = context with
                {
                    Triggered = new CardTrigger(card),
                    Action = new InvokeCardEffectsAction(new InvokeCardEffectsSource(card, caster, request.RequestedBy))
                };
                var effects = card.Effects
                    .Select(effect => (EffectQueueItem)new CardEffectQueueItem(invokeContext, effect))
                    .ToArray();
                queue.Enqueue(effects, EffectQueuePosition.Immediate, selectedContext.Some());
            }

            return CommandApplyResult.Empty;
        }
    }
}
