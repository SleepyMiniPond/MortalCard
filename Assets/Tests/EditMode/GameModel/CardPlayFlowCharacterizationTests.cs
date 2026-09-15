using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class CardPlayFlowCharacterizationTests
    {
        [Test]
        public void UseCard_EffectRepeatProperty_RepeatsNormalEffectsButEmitsOneUsedEvent()
        {
            const string cardId = "card-play-effect-repeat";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.PropertyDatas.Add(new EffectRepeatPropertyData { Value = 2 });
            cardData.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents();
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(2));
            Assert.That(events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UseCard_EffectRepeatCardBuffProperty_RepeatsNormalEffects()
        {
            const string cardId = "card-play-buff-effect-repeat";
            const string cardBuffId = "card-play-effect-repeat-buff";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            var cardBuffData = new CardBuffData
            {
                ID = cardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData(),
                PropertyDatas = new List<ICardBuffPropertyData>
                {
                    new EffectRepeatCardBuffPropertyData
                    {
                        Value = new ConstInteger { Value = 2 }
                    }
                }
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .Build();
            var setupContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new CardLookIntentAction(CardEntity.DummyCard));
            var card = CardTestBuilder.CreateCardWithBuff(
                setupContext,
                built.ContextManager.CardBuffLibrary,
                built.ContextManager.CardLibrary,
                cardId,
                cardBuffId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents();
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(2));
            Assert.That(events.OfType<UsedCardEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UseCard_NormalEffectRunsWhileCardIsPlayingThenUsedEventPrecedesGraveyardExit()
        {
            const string cardId = "card-play-flow";
            const string cardBuffId = "card-play-flow-buff";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.Effects.Add(new GainEnergyEffect
            {
                Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
                Value = new ConstInteger { Value = 1 }
            });
            cardData.Effects.Add(new AddCardBuffEffect
            {
                TargetCards = new SingleCardCollection
                {
                    TargetCard = new PlayingCardOfPlayer
                    {
                        Player = new CurrentPlayer()
                    }
                },
                AddCardBuffDatas = new List<AddCardBuffData>
                {
                    new()
                    {
                        CardBuffId = cardBuffId,
                        Level = new ConstInteger { Value = 1 }
                    }
                }
            });
            var cardBuffData = new CardBuffData
            {
                ID = cardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithCardBuff(cardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            var events = built.Manager.PopAllEvents().ToArray();
            Assert.That(
                events
                    .Where(gameEvent => gameEvent is
                        LoseEnergyEvent or
                        GainEnergyEvent or
                        AddCardBuffEvent or
                        UsedCardEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(LoseEnergyEvent),
                    typeof(GainEnergyEvent),
                    typeof(AddCardBuffEvent),
                    typeof(UsedCardEvent)
                }));
            Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(1));
            Assert.That(
                card.BuffManager.Buffs.Select(buff => buff.CardBuffDataID),
                Does.Contain(cardBuffId));
            Assert.That(
                built.Ally.CardManager.HandCard.Cards.Any(item => item.Identity == card.Identity),
                Is.False);
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(
                built.Ally.CardManager.Graveyard.Cards.Any(item => item.Identity == card.Identity),
                Is.True);
        }

        [Test]
        public void UseCard_DisposePropertyMovesCardToExclusionZoneAfterUsedEvent()
        {
            const string cardId = "card-play-dispose-flow";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.PropertyDatas.Add(new DisposePropertyData());
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, cardId);
            built.Ally.CardManager.HandCard.AddCard(card);
            _InitializeEventBuffer(built.Manager);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                _InvokeUseCard(built.Manager, built.Ally, card.Identity);
            }

            Assert.That(
                built.Manager.PopAllEvents()
                    .Where(gameEvent => gameEvent is LoseEnergyEvent or UsedCardEvent)
                    .Select(gameEvent => gameEvent.GetType()),
                Is.EqualTo(new[]
                {
                    typeof(LoseEnergyEvent),
                    typeof(UsedCardEvent)
                }));
            Assert.That(built.Ally.CardManager.PlayingCard.HasValue, Is.False);
            Assert.That(
                built.Ally.CardManager.Graveyard.Cards.Any(item => item.Identity == card.Identity),
                Is.False);
            Assert.That(
                built.Ally.CardManager.ExclusionZone.Cards.Any(item => item.Identity == card.Identity),
                Is.True);
        }

        private static void _InitializeEventBuffer(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetField("_gameEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, new List<IGameEvent>());
        }

        private static void _InvokeUseCard(
            GameplayManager manager,
            IPlayerEntity player,
            System.Guid cardIdentity)
        {
            typeof(GameplayManager)
                .GetMethod("_UseCard", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(manager, new object[] { player, cardIdentity });
        }
    }
}
