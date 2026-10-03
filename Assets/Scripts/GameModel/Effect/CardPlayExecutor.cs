using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using Optional;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 執行單張卡的驗證、付費、效果與離場；選取作用域及出牌鏈由 GameplayManager 協調。
    /// </summary>
    internal static class CardPlayExecutor
    {
        internal static bool Execute(
            IGameplayModel model,
            CardPlayChain chain,
            IPlayerEntity player,
            Guid cardIdentity,
            CardPlayReason reason)
        {
            if (!_PrepareCardPlay(model, player, cardIdentity, reason).TryGetValue(out var preparation))
                return false;

            var usedCard = preparation.Card;
            if (!player.TryBeginCardPlay(usedCard).TryGetValue(out var playCardScope))
            {
                return false;
            }

            var cardPlaySource = new CardPlaySource(
                usedCard, playCardScope.HandCardIndex, playCardScope.HandCardsCount,
                reason, preparation.Payment, new CardPlayAttributeEntity());
            var cardPlayTrigger = new CardPlayTrigger(cardPlaySource);
            var cardPlayIntent = new CardPlayIntentAction(cardPlaySource);
            var cardPlayTriggerContext = new TriggerContext(model, cardPlayTrigger, cardPlayIntent);
            CardPlayResultSource cardPlayResultSource;
            var completed = false;
            try
            {
                using (playCardScope)
                {
                    if (preparation.Payment.TryGetValue(out var paid))
                    {
                        var result = player.EnergyManager.ConsumeEnergy(paid.EnergySpent);
                        chain.Record(new LoseEnergyEvent(player.Faction, player.EnergyManager.ToInfo(), result));
                    }
                    model.TriggerTiming(
                        GameTiming.BeforePlayCardStart,
                        cardPlaySource);

                    chain.Record(model.ObserveRootAction(cardPlayIntent));

                    // TODO：檢查並移除過期 Buff，派送對應移除事件。

                    model.TriggerTiming(
                        GameTiming.AfterPlayCardStart,
                        cardPlaySource);

                    var effectActionResults = _RunRepeatedEffects(
                        cardPlayTriggerContext, chain, usedCard, preparation.EffectRepeat);

                    var usedCardEvent = new UsedCardEvent(
                        Faction: player.Faction,
                        UsedCardIdentity: usedCard.Identity,
                        CardManagerInfo: player.CardManager.ToInfo(),
                        Reason: cardPlaySource.Reason);
                    chain.Record(usedCardEvent);

                    var playedResult = model.RunEffectBatch(
                        CardTriggeredEffectDispatch.CreateItems(
                            cardPlayTriggerContext,
                            usedCard,
                            cardPlaySource.Reason == CardPlayReason.Active
                            ? CardTriggeredTiming.Played
                            : CardTriggeredTiming.EffectPlayed));

                    effectActionResults.AddRange(playedResult.Actions);

                    cardPlayResultSource = cardPlaySource.CreateResultSource(effectActionResults);

                    chain.Record(
                        model.ObserveRootAction(new CardPlayResultAction(cardPlayResultSource)));
                    model.TriggerTiming(
                        GameTiming.BeforePlayCardEnd,
                        cardPlayResultSource);
                }

                if (usedCard.HasProperty(CardProperty.Recycle))
                {
                    var recycleResult = EffectManager.RecycleCardOnPlayEnd(model, player, usedCard);
                    chain.Record(recycleResult.Events);
                }

                model.TriggerTiming(
                    GameTiming.AfterPlayCardEnd,
                    cardPlayResultSource);
                completed = true;
            }
            finally
            {
                if (!completed)
                    _RecordInterruptedExit(chain, player, usedCard);
            }

            return true;
        }

        private static Option<CardPlayPreparation> _PrepareCardPlay(
            IGameplayModel model,
            IPlayerEntity player,
            Guid cardIdentity,
            CardPlayReason reason)
        {
            if (model.GameStatus.Ally.CardManager.PlayingCard.HasValue ||
                model.GameStatus.Enemy.CardManager.PlayingCard.HasValue)
                return Option.None<CardPlayPreparation>();

            var card = player.CardManager.HandCard.Cards.FirstOrDefault(c => c.Identity == cardIdentity);
            if (card == null || card.HasProperty(CardProperty.Sealed))
                return Option.None<CardPlayPreparation>();

            var lookContext = new TriggerContext(
                model, new CardTrigger(card), new CardLookIntentAction(card, player.Some()));
            if (!card.GetCardProperty(lookContext, CardProperty.EffectRepeat).TryGetValue(out var effectRepeat))
                return Option.None<CardPlayPreparation>();

            var payment = Option.None<CardPlayPayment>();
            if (reason == CardPlayReason.Active)
            {
                if (!GameFormula.CardCost(lookContext, card).TryGetValue(out var cost) ||
                    cost > player.CurrentEnergy)
                    return Option.None<CardPlayPreparation>();

                payment = new CardPlayPayment(cost).Some();
            }

            return new CardPlayPreparation(card, effectRepeat, payment).Some();
        }

        private static List<BaseResultAction> _RunRepeatedEffects(
            TriggerContext context,
            CardPlayChain chain,
            ICardEntity card,
            int effectRepeat)
        {
            var results = new List<BaseResultAction>();
            var repeatTimes = Math.Max(1, effectRepeat);
            for (int i = 0; i < repeatTimes; i++)
            {
                chain.BeginStep($"CardEffectRepeat:{card.Identity}:{i}");
                // 每次重複都讀取當前效果，保留執行期間形態變更的行為。
                var effectResult = context.Model.RunEffectBatch(
                    card.Effects.Select(effect => new CardEffectQueueItem(context, effect)));
                results.AddRange(effectResult.Actions);
            }

            return results;
        }

        private static void _RecordInterruptedExit(
            CardPlayChain chain,
            IPlayerEntity player,
            ICardEntity card)
        {
            // PlayingCard 的 Dispose 已完成；只同步真實離場，不偽造 UsedCard 或 Result。
            if (player.CardManager.HandCard.Cards.Any(c => c.Identity == card.Identity))
            {
                chain.Record(new PlayerExecuteEndEvent(player.Faction, player.CardManager.ToInfo()));
                return;
            }

            var destination = player.CardManager.ExclusionZone.Cards.Any(c => c.Identity == card.Identity)
                ? CardCollectionType.ExclusionZone : CardCollectionType.Graveyard;
            chain.Record(new MoveCardEvent(player.Faction, card.Identity,
                CardCollectionType.HandCard, destination, player.CardManager.ToInfo()));
        }

        private sealed record CardPlayPreparation(
            ICardEntity Card,
            int EffectRepeat,
            Option<CardPlayPayment> Payment);
    }
}
