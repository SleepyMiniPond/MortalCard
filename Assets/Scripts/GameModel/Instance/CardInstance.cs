using System;
using System.Collections.Generic;
using MortalGame.GameData;
using Optional;

namespace MortalGame.GameModel
{

    public record CardInstance(
        // 靜態資料
        Guid InstanceGuid,
        string CardDataId,
        // 動態資料
        IReadOnlyList<ICardPropertyData> AdditionPropertyDatas,
        Option<PersistentCardFormState> PersistentFormState = default)
    {
        public static CardInstance Create(StandardCardData cardData)
        {
            return new CardInstance(
                InstanceGuid: Guid.NewGuid(),
                CardDataId: cardData.ID,
                AdditionPropertyDatas: Array.Empty<ICardPropertyData>(),
                PersistentFormState: Option.None<PersistentCardFormState>()
            );
        }
    }

}
