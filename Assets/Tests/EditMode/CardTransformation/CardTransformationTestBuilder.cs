using System;
using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;
using Optional;

namespace MortalGame.Tests.T010
{
    /// <summary>
    /// 提供 T-010 形態系統測試共用的最小戰鬥與卡片組裝。
    /// </summary>
    public sealed class CardTransformationTestBuilder
    {
        public const string BaseCardId = "t010-base-card";
        public const string AlternateCardId = "t010-alternate-card";
        public const string CardBuffId = "t010-card-buff";

        private readonly Dictionary<string, CardData> _cards = new();
        private readonly Dictionary<string, PlayerBuffData> _playerBuffs = new();
        private readonly Dictionary<string, CardBuffData> _cardBuffs = new();

        public CardTransformationTestBuilder()
        {
            var baseCard = CreateCardData(BaseCardId, cost: 2, power: 3);
            baseCard.PropertyDatas.Add(new PreservedPropertyData());
            WithCard(baseCard);

            var alternateCard = CreateCardData(AlternateCardId, cost: 5, power: 8);
            alternateCard.Type = CardType.Defense;
            alternateCard.PropertyDatas.Add(new InitialPriorityPropertyData());
            WithCard(alternateCard);
            WithCardBuff(BuffTestBuilder.CreateCardBuffData(
                CardBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalCardBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new NoOpCardBuffEffect()
                }));
        }

        public CardTransformationTestBuilder WithCard(StandardCardData cardData)
        {
            _cards[cardData.ID] = cardData;
            return this;
        }

        public CardTransformationTestBuilder WithCard(OverrideCardData cardData)
        {
            _cards[cardData.ID] = cardData;
            return this;
        }

        public CardTransformationTestBuilder WithCardBuff(CardBuffData cardBuffData)
        {
            _cardBuffs[cardBuffData.ID] = cardBuffData;
            return this;
        }

        public CardTransformationTestBuilder WithPlayerBuff(PlayerBuffData playerBuffData)
        {
            _playerBuffs[playerBuffData.ID] = playerBuffData;
            return this;
        }

        public BuiltCardTransformationTest Build()
        {
            var gameplay = new GameplayManagerTestBuilder();
            foreach (var cardData in _cards.Values)
            {
                switch (cardData)
                {
                    case StandardCardData standardCardData:
                        gameplay.WithCard(standardCardData);
                        break;
                    case OverrideCardData overrideCardData:
                        gameplay.WithCard(overrideCardData);
                        break;
                }
            }

            foreach (var cardBuff in _cardBuffs.Values)
            {
                gameplay.WithCardBuff(cardBuff);
            }

            foreach (var playerBuff in _playerBuffs.Values)
            {
                gameplay.WithPlayerBuff(playerBuff);
            }

            var builtGameplay = gameplay.Build();
            var card = CardEntity.RuntimeCreateFromId(
                BaseCardId,
                builtGameplay.ContextManager.CardLibrary,
                builtGameplay.ContextManager.CardPropertyEntityFactory);
            var context = new TriggerContext(
                builtGameplay.Manager,
                new CardTrigger(card),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));

            return new BuiltCardTransformationTest(builtGameplay, card, context);
        }

        public BuiltCardTransformationTest BuildFromInstance(params ICardPropertyData[] additionProperties)
        {
            return _BuildFromInstance(
                Option.None<PersistentCardFormState>(),
                additionProperties);
        }

        public BuiltCardTransformationTest BuildFromInstance(
            PersistentCardFormState persistentFormState,
            params ICardPropertyData[] additionProperties)
        {
            return _BuildFromInstance(
                persistentFormState.Some(),
                additionProperties);
        }

        private BuiltCardTransformationTest _BuildFromInstance(
            Option<PersistentCardFormState> persistentFormState,
            IReadOnlyList<ICardPropertyData> additionProperties)
        {
            var built = Build();
            var instance = new CardInstance(
                Guid.NewGuid(),
                BaseCardId,
                additionProperties,
                persistentFormState);
            var card = CardEntity.CreateFromInstance(
                instance,
                built.Gameplay.ContextManager.CardLibrary,
                built.Gameplay.ContextManager.CardPropertyEntityFactory);
            var context = new TriggerContext(
                built.Gameplay.Manager,
                new CardTrigger(card),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));

            return new BuiltCardTransformationTest(built.Gameplay, card, context);
        }

        public static StandardCardData CreateCardData(string cardId, int cost, int power)
        {
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.Cost = cost;
            cardData.Power = power;
            return cardData;
        }
    }

    public sealed record BuiltCardTransformationTest(
        BuiltGameplay Gameplay,
        ICardEntity Card,
        TriggerContext Context);
}
