using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{

// Cards in the exclusion zone are removed from the Deck, but return next Battle.
public interface IExclusionZoneEntity : ICardColletionZone
{
}

public class ExclusionZoneEntity : CardColletionZone, IExclusionZoneEntity
{        
    public ExclusionZoneEntity() : base(CardCollectionType.ExclusionZone)
    {
        
    }
}

}
