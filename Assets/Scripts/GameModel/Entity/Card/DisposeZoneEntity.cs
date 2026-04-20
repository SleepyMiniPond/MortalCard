using System;
using System.Collections.Generic;
using System.Linq;
using Optional;

// Cards in the dispose zone are removed from the Deck forever in this game.
public interface IDisposeZoneEntity : ICardColletionZone
{
}

public class DisposeZoneEntity : CardColletionZone, IDisposeZoneEntity
{        
    public DisposeZoneEntity() : base(CardCollectionType.DisposeZone)
    {
    }
}