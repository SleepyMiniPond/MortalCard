using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

public record CardInstance(
    // static data
    Guid InstanceGuid,
    string CardDataId,
    // dynamic data
    IReadOnlyList<ICardPropertyData> AdditionPropertyDatas)
{
    public static CardInstance Create(CardData cardData)
    {
        return new CardInstance(
            InstanceGuid: Guid.NewGuid(),
            CardDataId: cardData.ID,
            AdditionPropertyDatas: Array.Empty<ICardPropertyData>()
        );
    }
}
