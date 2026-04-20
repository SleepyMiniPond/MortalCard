using System.Linq;
using UnityEngine;

public interface ICardBuffLifeTimeEntity
{
    bool IsExpired();
    bool Update(TriggerContext triggerContext);
    ICardBuffLifeTimeEntity Clone();
}

public class AlwaysLifeTimeCardBuffEntity : ICardBuffLifeTimeEntity
{
    public bool IsExpired()
    {
        return false;
    }

    public bool Update(TriggerContext triggerContext)
        => false;
    
    public ICardBuffLifeTimeEntity Clone() => new AlwaysLifeTimeCardBuffEntity();
}

public class TurnLifeTimeCardBuffEntity : ICardBuffLifeTimeEntity
{
    private int _turn;

    public TurnLifeTimeCardBuffEntity(int turn)
    {
        _turn = turn;
    }

    public bool IsExpired()
    {
        return _turn <= 0;
    }

    public bool Update(TriggerContext triggerContext)
    {
        if (triggerContext.Action is UpdateTimingAction timingAction &&
            timingAction.Timing == GameTiming.TurnEnd)
        {
            _turn--;
            return true;
        }
        return false;
    }

    public ICardBuffLifeTimeEntity Clone() => new TurnLifeTimeCardBuffEntity(_turn);
}

public class HandCardLifeTimeCardBuffEntity : ICardBuffLifeTimeEntity
{
    private bool _expired;
    public HandCardLifeTimeCardBuffEntity()
    { 
        _expired = false;
    }

    public bool IsExpired()
    {
        return _expired;
    }

    public bool Update(TriggerContext triggerContext)
    {
        if (!_CheckInHandCard(triggerContext.Model))
        {
            _expired = true;
            return true;
        }
        return false;
    }

    private bool _CheckInHandCard(IGameplayModel gameModel)
    {
        foreach (var card in gameModel.GameStatus.Ally.CardManager.HandCard.Cards
            .Concat(gameModel.GameStatus.Enemy.CardManager.HandCard.Cards))
        {
            foreach (var buff in card.BuffManager.Buffs)
            {
                if (buff.LifeTime == this)
                    return true;
            }
        }
        return false;
    }

    public ICardBuffLifeTimeEntity Clone() => new HandCardLifeTimeCardBuffEntity();
}