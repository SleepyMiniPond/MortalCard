using System;
using MortalGame.Exceptions;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

    public interface ICharacterBuffLifeTimeEntityCreator
    {
        Type DataType { get; }
        ICharacterBuffLifeTimeEntity Create(ICharacterBuffLifeTimeData data);
    }

    public interface ICharacterBuffLifeTimeEntityFactory
    {
        ICharacterBuffLifeTimeEntity Create(ICharacterBuffLifeTimeData data);
    }

    public sealed class CharacterBuffLifeTimeEntityFactory : ICharacterBuffLifeTimeEntityFactory
    {
        private readonly IReadOnlyDictionary<Type, ICharacterBuffLifeTimeEntityCreator> _creators;

        public CharacterBuffLifeTimeEntityFactory(IEnumerable<ICharacterBuffLifeTimeEntityCreator> creators)
        {
            _creators = creators.ToDictionary(creator => creator.DataType);
        }

        public static CharacterBuffLifeTimeEntityFactory CreateDefault()
        {
            return new CharacterBuffLifeTimeEntityFactory(new ICharacterBuffLifeTimeEntityCreator[]
            {
            new AlwaysLifeTimeCreator(), new TurnLifeTimeCreator(),
            });
        }

        public ICharacterBuffLifeTimeEntity Create(ICharacterBuffLifeTimeData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (_creators.TryGetValue(data.GetType(), out var creator)) return creator.Create(data);
            throw new UnregisteredDataTypeException(typeof(CharacterBuffLifeTimeEntityFactory), data.GetType(), nameof(data));
        }

        private sealed class AlwaysLifeTimeCreator : ICharacterBuffLifeTimeEntityCreator { public Type DataType => typeof(AlwaysLifeTimeCharacterBuffData); public ICharacterBuffLifeTimeEntity Create(ICharacterBuffLifeTimeData data) => new AlwaysLifeTimeCharacterBuffEntity(); }
        private sealed class TurnLifeTimeCreator : ICharacterBuffLifeTimeEntityCreator { public Type DataType => typeof(TurnLifeTimeCharacterBuffData); public ICharacterBuffLifeTimeEntity Create(ICharacterBuffLifeTimeData data) => new TurnLifeTimeCharacterBuffEntity(((TurnLifeTimeCharacterBuffData)data).Turn); }
    }

}
