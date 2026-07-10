using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

public interface ICharacterBuffPropertyEntityCreator
{
    Type DataType { get; }
    ICharacterBuffPropertyEntity Create(ICharacterBuffPropertyData data);
}

public interface ICharacterBuffPropertyEntityFactory
{
    ICharacterBuffPropertyEntity Create(ICharacterBuffPropertyData data);
}

public sealed class CharacterBuffPropertyEntityFactory : ICharacterBuffPropertyEntityFactory
{
    private readonly IReadOnlyDictionary<Type, ICharacterBuffPropertyEntityCreator> _creators;

    public CharacterBuffPropertyEntityFactory(IEnumerable<ICharacterBuffPropertyEntityCreator> creators)
    {
        _creators = creators.ToDictionary(creator => creator.DataType);
    }

    public static CharacterBuffPropertyEntityFactory CreateDefault()
    {
        return new CharacterBuffPropertyEntityFactory(new ICharacterBuffPropertyEntityCreator[]
        {
            new MaxHealthCreator(), new MaxEnergyCreator(),
        });
    }

    public ICharacterBuffPropertyEntity Create(ICharacterBuffPropertyData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (_creators.TryGetValue(data.GetType(), out var creator)) return creator.Create(data);
        throw new ArgumentException($"未註冊的 Character Buff Property Data 型別：{data.GetType().FullName}", nameof(data));
    }

    private sealed class MaxHealthCreator : ICharacterBuffPropertyEntityCreator { public Type DataType => typeof(MaxHealthPropertyCharacterBuffData); public ICharacterBuffPropertyEntity Create(ICharacterBuffPropertyData data) => new MaxHealthPropertyCharacterBuffEntity(); }
    private sealed class MaxEnergyCreator : ICharacterBuffPropertyEntityCreator { public Type DataType => typeof(MaxEnergyPropertyCharacterBuffData); public ICharacterBuffPropertyEntity Create(ICharacterBuffPropertyData data) => new MaxEnergyPropertyCharacterBuffEntity(); }
}

}
