using UnityEngine;

public interface IPlayerBuffLifeTimeEntity
{
    bool IsExpired();
    bool Update(TriggerContext triggerContext);
}

public class AlwaysLifeTimePlayerBuffEntity : IPlayerBuffLifeTimeEntity
{
    public bool IsExpired()
    {
        return false;
    }

    public bool Update(TriggerContext triggerContext)
    {
        return false;
    }
}

public class TurnLifeTimePlayerBuffEntity : IPlayerBuffLifeTimeEntity
{
    private int _turn;

    public TurnLifeTimePlayerBuffEntity(int turn)
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