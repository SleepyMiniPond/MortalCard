using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Tests
{

    public static class CardTestBuilder
    {
        public const string CardId = "test-card";

        public static StandardCardData CreateCardData(string cardId = CardId)
        {
            return new StandardCardData
            {
                ID = cardId,
                Rarity = CardRarity.Common,
                Type = CardType.Attack,
                Cost = 0,
                Power = 0,
                Themes = new CardTheme[0],
                Effects = new List<ICardEffect>(),
                PropertyDatas = new List<ICardPropertyData>()
            };
        }

        public static ICardEntity CreateCard(CardLibrary cardLibrary, string cardId = CardId)
        {
            return CardEntity.RuntimeCreateFromId(
                cardId,
                cardLibrary,
                CardPropertyEntityFactory.CreateDefault());
        }

        public static ICardEntity CreateCardWithBuff(
            TriggerContext context,
            CardBuffLibrary cardBuffLibrary,
            CardLibrary cardLibrary,
            string cardId = CardId,
            string buffId = BuffTestBuilder.CardBuffId)
        {
            var card = CreateCard(cardLibrary, cardId);
            var buff = BuffTestBuilder.CreateCardBuff(context, cardBuffLibrary, buffId);
            card.BuffManager.AddBuff(buff);
            return card;
        }
    }
}
