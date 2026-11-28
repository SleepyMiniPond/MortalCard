using System;
using MortalGame.Exceptions;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

    public interface ICardBuffPropertyEntityCreator
    {
        Type DataType { get; }
        ICardBuffPropertyEntity Create(ICardBuffPropertyData data);
    }

    public interface ICardBuffPropertyEntityFactory
    {
        ICardBuffPropertyEntity Create(ICardBuffPropertyData data);
    }

    public sealed class CardBuffPropertyEntityFactory : ICardBuffPropertyEntityFactory
    {
        private readonly IReadOnlyDictionary<Type, ICardBuffPropertyEntityCreator> _creators;

        public CardBuffPropertyEntityFactory(IEnumerable<ICardBuffPropertyEntityCreator> creators)
        {
            _creators = creators.ToDictionary(creator => creator.DataType);
        }

        public static CardBuffPropertyEntityFactory CreateDefault()
        {
            return new CardBuffPropertyEntityFactory(new ICardBuffPropertyEntityCreator[]
            {
            new EffectRepeatCardBuffPropertyEntityCreator(),
            new SealedCardBuffPropertyEntityCreator(),
            new PowerCardBuffPropertyEntityCreator(),
            });
        }

        public ICardBuffPropertyEntity Create(ICardBuffPropertyData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_creators.TryGetValue(data.GetType(), out var creator))
            {
                return creator.Create(data);
            }

            throw new UnregisteredDataTypeException(typeof(CardBuffPropertyEntityFactory), data.GetType(), nameof(data));
        }

        private sealed class EffectRepeatCardBuffPropertyEntityCreator : ICardBuffPropertyEntityCreator
        {
            public Type DataType => typeof(EffectRepeatCardBuffPropertyData);
            public ICardBuffPropertyEntity Create(ICardBuffPropertyData data) =>
                new EffectRepeatCardBuffPropertyEntity(
                    ((EffectRepeatCardBuffPropertyData)data).Value);
        }

        private sealed class SealedCardBuffPropertyEntityCreator : ICardBuffPropertyEntityCreator
        {
            public Type DataType => typeof(SealedCardBuffPropertyData);
            public ICardBuffPropertyEntity Create(ICardBuffPropertyData data) => new SealedCardBuffPropertyEntity();
        }

        private sealed class PowerCardBuffPropertyEntityCreator : ICardBuffPropertyEntityCreator
        {
            public Type DataType => typeof(PowerCardBuffPropertyData);
            public ICardBuffPropertyEntity Create(ICardBuffPropertyData data) =>
                new PowerCardBuffPropertyEntity(((PowerCardBuffPropertyData)data).Value);
        }
    }

}
