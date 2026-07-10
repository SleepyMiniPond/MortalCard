using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

public interface IReactionSessionEntityCreator
{
    Type DataType { get; }
    IReactionSessionEntity Create(IReactionSessionData data);
}

public interface IReactionSessionEntityFactory
{
    IReactionSessionEntity Create(IReactionSessionData data);
}

public sealed class ReactionSessionEntityFactory : IReactionSessionEntityFactory
{
    private readonly IReadOnlyDictionary<Type, IReactionSessionEntityCreator> _creators;

    public ReactionSessionEntityFactory(IEnumerable<IReactionSessionEntityCreator> creators)
    {
        _creators = creators.ToDictionary(creator => creator.DataType);
    }

    public static ReactionSessionEntityFactory CreateDefault()
    {
        return new ReactionSessionEntityFactory(new IReactionSessionEntityCreator[]
        {
            new SessionBooleanEntityCreator(),
            new SessionIntegerEntityCreator(),
        });
    }

    public IReactionSessionEntity Create(IReactionSessionData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (_creators.TryGetValue(data.GetType(), out var creator))
        {
            return creator.Create(data);
        }

        throw new ArgumentException(
            $"未註冊的 Reaction Session Data 型別：{data.GetType().FullName}",
            nameof(data));
    }

    private sealed class SessionBooleanEntityCreator : IReactionSessionEntityCreator
    {
        public Type DataType => typeof(SessionBoolean);
        public IReactionSessionEntity Create(IReactionSessionData data)
        {
            var sessionData = (SessionBoolean)data;
            return new ReactionSessionEntity(
                new SessionBooleanEntity(sessionData.InitialValue, sessionData.UpdateRules),
                sessionData.LifeTime);
        }
    }

    private sealed class SessionIntegerEntityCreator : IReactionSessionEntityCreator
    {
        public Type DataType => typeof(SessionInteger);
        public IReactionSessionEntity Create(IReactionSessionData data)
        {
            var sessionData = (SessionInteger)data;
            return new ReactionSessionEntity(
                new SessionIntegerEntity(sessionData.InitialValue, sessionData.UpdateRules),
                sessionData.LifeTime);
        }
    }
}

}
