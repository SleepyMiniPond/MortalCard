using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

    public interface ICardBuffLifeTimeEntityCreator
    {
        Type DataType { get; }
        ICardBuffLifeTimeEntity Create(ICardBuffLifeTimeData data, TriggerContext triggerContext);
    }

    public interface ICardBuffLifeTimeEntityFactory
    {
        ICardBuffLifeTimeEntity Create(ICardBuffLifeTimeData data, TriggerContext triggerContext);
    }

    public sealed class CardBuffLifeTimeEntityFactory : ICardBuffLifeTimeEntityFactory
    {
        private readonly IReadOnlyDictionary<Type, ICardBuffLifeTimeEntityCreator> _creators;

        public CardBuffLifeTimeEntityFactory(IEnumerable<ICardBuffLifeTimeEntityCreator> creators)
        {
            _creators = creators.ToDictionary(creator => creator.DataType);
        }

        public static CardBuffLifeTimeEntityFactory CreateDefault()
        {
            return new CardBuffLifeTimeEntityFactory(new ICardBuffLifeTimeEntityCreator[]
            {
            new AlwaysLifeTimeCardBuffEntityCreator(),
            new TurnLifeTimeCardBuffEntityCreator(),
            new HandCardLifeTimeCardBuffEntityCreator(),
            });
        }

        public ICardBuffLifeTimeEntity Create(ICardBuffLifeTimeData data, TriggerContext triggerContext)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_creators.TryGetValue(data.GetType(), out var creator))
            {
                return creator.Create(data, triggerContext);
            }

            throw new ArgumentException(
                $"未註冊的 Card Buff LifeTime Data 型別：{data.GetType().FullName}",
                nameof(data));
        }

        private sealed class AlwaysLifeTimeCardBuffEntityCreator : ICardBuffLifeTimeEntityCreator
        {
            public Type DataType => typeof(AlwaysLifeTimeCardBuffData);
            public ICardBuffLifeTimeEntity Create(ICardBuffLifeTimeData data, TriggerContext triggerContext) => new AlwaysLifeTimeCardBuffEntity();
        }

        private sealed class TurnLifeTimeCardBuffEntityCreator : ICardBuffLifeTimeEntityCreator
        {
            public Type DataType => typeof(TurnLifeTimeCardBuffData);
            public ICardBuffLifeTimeEntity Create(ICardBuffLifeTimeData data, TriggerContext triggerContext) =>
                new TurnLifeTimeCardBuffEntity(((TurnLifeTimeCardBuffData)data).Turn.Eval(triggerContext));
        }

        private sealed class HandCardLifeTimeCardBuffEntityCreator : ICardBuffLifeTimeEntityCreator
        {
            public Type DataType => typeof(HandCardLifeTimeCardBuffData);
            public ICardBuffLifeTimeEntity Create(ICardBuffLifeTimeData data, TriggerContext triggerContext) => new HandCardLifeTimeCardBuffEntity();
        }
    }

}
