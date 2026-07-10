using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

public interface IPlayerBuffLifeTimeEntityCreator
{
    Type DataType { get; }
    IPlayerBuffLifeTimeEntity Create(IPlayerBuffLifeTimeData data, TriggerContext triggerContext);
}

public interface IPlayerBuffLifeTimeEntityFactory
{
    IPlayerBuffLifeTimeEntity Create(IPlayerBuffLifeTimeData data, TriggerContext triggerContext);
}

public sealed class PlayerBuffLifeTimeEntityFactory : IPlayerBuffLifeTimeEntityFactory
{
    private readonly IReadOnlyDictionary<Type, IPlayerBuffLifeTimeEntityCreator> _creators;

    public PlayerBuffLifeTimeEntityFactory(IEnumerable<IPlayerBuffLifeTimeEntityCreator> creators)
    {
        _creators = creators.ToDictionary(creator => creator.DataType);
    }

    public static PlayerBuffLifeTimeEntityFactory CreateDefault()
    {
        return new PlayerBuffLifeTimeEntityFactory(new IPlayerBuffLifeTimeEntityCreator[]
        {
            new AlwaysLifeTimeCreator(), new TurnLifeTimeCreator(),
        });
    }

    public IPlayerBuffLifeTimeEntity Create(IPlayerBuffLifeTimeData data, TriggerContext triggerContext)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (_creators.TryGetValue(data.GetType(), out var creator)) return creator.Create(data, triggerContext);
        throw new ArgumentException($"未註冊的 Player Buff LifeTime Data 型別：{data.GetType().FullName}", nameof(data));
    }

    private sealed class AlwaysLifeTimeCreator : IPlayerBuffLifeTimeEntityCreator { public Type DataType => typeof(AlwaysLifeTimePlayerBuffData); public IPlayerBuffLifeTimeEntity Create(IPlayerBuffLifeTimeData data, TriggerContext triggerContext) => new AlwaysLifeTimePlayerBuffEntity(); }
    private sealed class TurnLifeTimeCreator : IPlayerBuffLifeTimeEntityCreator { public Type DataType => typeof(PlayerBuffTurnLifeTimeData); public IPlayerBuffLifeTimeEntity Create(IPlayerBuffLifeTimeData data, TriggerContext triggerContext) => new TurnLifeTimePlayerBuffEntity(((PlayerBuffTurnLifeTimeData)data).Turn.Eval(triggerContext)); }
}

}
