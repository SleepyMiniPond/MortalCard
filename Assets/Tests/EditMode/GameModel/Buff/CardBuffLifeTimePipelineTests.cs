using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.Presenter;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public class CardBuffLifeTimePipelineTests
    {
        [Test]
        public void AfterTurnEnd_HandCardLifeTimeBuff_RemovesBuffAndRefreshesGraveyardSnapshot()
        {
            var cardBuffData = new CardBuffData
            {
                ID = BuffTestBuilder.CardBuffId,
                LifeTimeData = new HandCardLifeTimeCardBuffData(),
                BuffEffects = new Dictionary<GameTiming, ConditionalCardBuffEffect[]>()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(cardBuffData)
                .Build();
            var context = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
            var card = CardTestBuilder.CreateCardWithBuff(
                context,
                built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);

            var viewModel = new GameViewModel();
            viewModel.UpdateCardInfo(card.ToInfo(built.Manager));
            built.Ally.CardManager.MoveCard(
                card,
                CardCollectionType.HandCard,
                CardCollectionType.Graveyard);
            viewModel.UpdateCardManagerInfo(Faction.Ally, built.Ally.CardManager.ToInfo());

            var events = built.Manager
                .TriggerTiming(GameTiming.AfterTurnEnd, SystemSource.Instance)
                .ToList();
            var updatedCardInfo = events
                .OfType<GeneralUpdateEvent>()
                .SelectMany(gameEvent => gameEvent.CardInfos)
                .Single(info => info.Identity == card.Identity);
            viewModel.UpdateCardInfo(updatedCardInfo);

            Assert.That(card.BuffManager.Buffs, Is.Empty);
            Assert.That(updatedCardInfo.BuffInfos, Is.Empty);
            Assert.That(
                viewModel
                    .ObservableCardCollectionInfo(Faction.Ally, CardCollectionType.Graveyard)
                    .Value
                    .CardInfos
                    .Keys
                    .Single(info => info.Identity == card.Identity)
                    .BuffInfos,
                Is.Empty);
        }
    }
}
