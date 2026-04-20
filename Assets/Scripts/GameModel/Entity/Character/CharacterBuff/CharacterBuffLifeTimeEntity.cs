using UnityEngine;

public interface ICharacterBuffLifeTimeEntity
{
    bool IsExpired();
    bool Update(TriggerContext triggerContext);
}

public class AlwaysLifeTimeCharacterBuffEntity : ICharacterBuffLifeTimeEntity
{
    public bool IsExpired()
    {
        return false;
    }

    public bool Update(TriggerContext triggerContext)
        => false;
}

public class TurnLifeTimeCharacterBuffEntity : ICharacterBuffLifeTimeEntity
{
    private int _turn;

    public TurnLifeTimeCharacterBuffEntity(int turn)
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
}
