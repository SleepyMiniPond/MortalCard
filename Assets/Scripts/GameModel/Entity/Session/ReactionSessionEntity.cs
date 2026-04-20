using System;
using System.Collections.Generic;
using System.Diagnostics;
using Optional;


public interface IReactionSessionEntity
{
    bool IsSessionValueUpdated { get; }
    Option<bool> BooleanValue { get; }
    Option<int> IntegerValue { get; }

    bool Update(TriggerContext triggerContext);
    IReactionSessionEntity Clone();
}

public class ReactionSessionEntity : IReactionSessionEntity
{
    private readonly SessionLifeTime _lifeTime;
    private readonly ISessionValueEntity _baseEntity;
    private Option<ISessionValueEntity> _currentValue;

    public bool IsSessionValueUpdated => _currentValue.HasValue;
    public Option<bool> BooleanValue => _currentValue.Match(
        value =>
            value is SessionBooleanEntity booleanEntity ?
                booleanEntity.Value.Some() :
                Option.None<bool>(),
        () => Option.None<bool>());
    public Option<int> IntegerValue => _currentValue.Match(
        value =>
            value is SessionIntegerEntity integerEntity ?
                integerEntity.Value.Some() :
                Option.None<int>(),
        () => Option.None<int>());

    public ReactionSessionEntity(ISessionValueEntity entity, SessionLifeTime lifeTime)
    {
        _baseEntity = entity;
        _currentValue = Option.None<ISessionValueEntity>();
        _lifeTime = lifeTime;

        if (lifeTime != SessionLifeTime.PlayCard)
        {
            _Reset();
        }
    }

    public bool Update(TriggerContext triggerContext)
    {
        bool isUpdated = false;
        if (triggerContext.Action is UpdateTimingAction timingAction)
        {
            switch (_lifeTime)
            {
                case SessionLifeTime.WholeTurn:
                    if (timingAction.Timing == GameTiming.TurnStart)
                    {
                        isUpdated = true;
                        _Reset();
                    }
                    else if (timingAction.Timing == GameTiming.TurnEnd)
                    {
                        isUpdated = true;
                        _Clear();
                    }
                    break;
                case SessionLifeTime.PlayCard:
                    if (timingAction.Timing == GameTiming.PlayCardStart)
                    {
                        isUpdated = true;
                        _Reset();
                    }
                    else if (timingAction.Timing == GameTiming.PlayCardEnd)
                    {
                        isUpdated = true;
                        _Clear();
                    }
                    break;
            }
        }

        isUpdated |= _currentValue.Match(
            value => value.Update(triggerContext),
            () => false);
        return isUpdated;
    }

    private void _Reset()
    {
        _currentValue = _baseEntity.Clone().Some();
    }
    private void _Clear()
    {
        _currentValue = Option.None<ISessionValueEntity>();
    }

    public IReactionSessionEntity Clone()
    {
        return new ReactionSessionEntity(
            entity: _baseEntity.Clone(),
            lifeTime: _lifeTime
        );
    }
}