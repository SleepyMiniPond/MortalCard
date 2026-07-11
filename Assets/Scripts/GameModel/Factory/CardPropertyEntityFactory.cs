using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

    public interface ICardPropertyEntityCreator
    {
        Type DataType { get; }
        ICardPropertyEntity Create(ICardPropertyData data);
    }

    public interface ICardPropertyEntityFactory
    {
        ICardPropertyEntity Create(ICardPropertyData data);
    }

    public sealed class CardPropertyEntityFactory : ICardPropertyEntityFactory
    {
        private readonly IReadOnlyDictionary<Type, ICardPropertyEntityCreator> _creators;

        public CardPropertyEntityFactory(IEnumerable<ICardPropertyEntityCreator> creators)
        {
            _creators = creators.ToDictionary(creator => creator.DataType);
        }

        public static CardPropertyEntityFactory CreateDefault()
        {
            return new CardPropertyEntityFactory(new ICardPropertyEntityCreator[]
            {
            new PreservedPropertyEntityCreator(),
            new InitialPriorityPropertyEntityCreator(),
            new ConsumablePropertyEntityCreator(),
            new DisposePropertyEntityCreator(),
            new AutoDisposePropertyEntityCreator(),
            new SealedPropertyEntityCreator(),
            new RecyclePropertyEntityCreator(),
            });
        }

        public ICardPropertyEntity Create(ICardPropertyData data)
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
                $"未註冊的 Card Property Data 型別：{data.GetType().FullName}",
                nameof(data));
        }

        private sealed class PreservedPropertyEntityCreator : ICardPropertyEntityCreator
        {
            public Type DataType => typeof(PreservedPropertyData);
            public ICardPropertyEntity Create(ICardPropertyData data) => new PreservedPropertyEntity();
        }

        private sealed class InitialPriorityPropertyEntityCreator : ICardPropertyEntityCreator
        {
            public Type DataType => typeof(InitialPriorityPropertyData);
            public ICardPropertyEntity Create(ICardPropertyData data) => new InitialPriorityPropertyEntity();
        }

        private sealed class ConsumablePropertyEntityCreator : ICardPropertyEntityCreator
        {
            public Type DataType => typeof(ConsumablePropertyData);
            public ICardPropertyEntity Create(ICardPropertyData data) => new ConsumablePropertyEntity();
        }

        private sealed class DisposePropertyEntityCreator : ICardPropertyEntityCreator
        {
            public Type DataType => typeof(DisposePropertyData);
            public ICardPropertyEntity Create(ICardPropertyData data) => new DisposePropertyEntity();
        }

        private sealed class AutoDisposePropertyEntityCreator : ICardPropertyEntityCreator
        {
            public Type DataType => typeof(AutoDisposePropertyData);
            public ICardPropertyEntity Create(ICardPropertyData data) => new AutoDisposePropertyEntity();
        }

        private sealed class SealedPropertyEntityCreator : ICardPropertyEntityCreator
        {
            public Type DataType => typeof(SealedPropertyData);
            public ICardPropertyEntity Create(ICardPropertyData data) => new SealedPropertyEntity();
        }

        private sealed class RecyclePropertyEntityCreator : ICardPropertyEntityCreator
        {
            public Type DataType => typeof(RecyclePropertyData);
            public ICardPropertyEntity Create(ICardPropertyData data) => new RecyclePropertyEntity();
        }
    }

}
