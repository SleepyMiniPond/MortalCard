using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Presentation.Abstractions
{
    public record AllCardDetailDisplayState(
        CardCollectionType CollectionType,
        IReadOnlyList<CardInfo> Cards);
}
