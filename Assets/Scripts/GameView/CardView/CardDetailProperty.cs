using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.GameView
{
    public record CardDetailProperty(
        ICardView.CardSimpleProperty CardProperty,
        CardPropertyHint.ViewData CardBuffHint,
        CardPropertyHint.ViewData CardKeywordHint)
    {
        public static CardDetailProperty Create(CardInfo cardInfo)
        {
            return new CardDetailProperty(
                CardProperty: new ICardView.CardSimpleProperty(cardInfo),
                CardBuffHint: new CardPropertyHint.ViewData(
                    cardInfo.BuffInfos
                        .Select(buffInfo =>
                            new CardPropertyHint.InfoCellViewData(
                                LocalizeTitleInfoType.CardBuff,
                                buffInfo.CardBuffDataId,
                                buffInfo.GetTemplateValues()))
                        .ToArray()),
                CardKeywordHint: new CardPropertyHint.ViewData(
                    cardInfo.Keywords
                        .Select(keyword =>
                            new CardPropertyHint.InfoCellViewData(
                                LocalizeTitleInfoType.KeyWord,
                                keyword,
                                Utility.Dictionary<string, string>.EMPTY))
                        .ToArray()));
        }
    }
}
