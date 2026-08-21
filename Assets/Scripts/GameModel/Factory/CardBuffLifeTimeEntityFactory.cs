using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{

    public interface ICardBuffLifeTimeEntityCreator
    {
        Type DataType { get; }
        Option<ICardBuffLifeTimeEntity> Create(ICardBuffLifeTimeData data, TriggerContext triggerContext);
    }

    public interface ICardBuffLifeTimeEntityFactory
    {
        Option<ICardBuffLifeTimeEntity> Create(ICardBuffLifeTimeData data, TriggerContext triggerContext);
    }

    public sealed class CardBuffLifeTimeEntityFactory : ICardBuffLifeTimeEntityFactory
    {
        private readonly IReadOnlyDictionary<Type, ICardBuffLifeTimeEntityCreator> _creators;

        public CardBuffLifeTimeEntityFactory(IEnumerable<ICardBuffLifeTimeEntityCreator> creators)
        {
            _creators = creators.ToDictionary(creator => creator.DataType);
        }

        public static ICardBuffLifeTimeEntityFactory CreateDefault()
        {
            return new CardBuffLifeTimeEntityFactory(new ICardBuffLifeTimeEntityCreator[]
            {
                new AlwaysLifeTimeCardBuffEntityCreator(),
                new TurnLifeTimeCardBuffEntityCreator(),
                new HandCardLifeTimeCardBuffEntityCreator(),
            });
        }

        public Option<ICardBuffLifeTimeEntity> Create(ICardBuffLifeTimeData data, TriggerContext triggerContext)
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
            public Option<ICardBuffLifeTimeEntity> Create(ICardBuffLifeTimeData data, TriggerContext triggerContext) =>
                ((ICardBuffLifeTimeEntity)new AlwaysLifeTimeCardBuffEntity()).Some();
        }

        private sealed class TurnLifeTimeCardBuffEntityCreator : ICardBuffLifeTimeEntityCreator
        {
            public Type DataType => typeof(TurnLifeTimeCardBuffData);
            public Option<ICardBuffLifeTimeEntity> Create(ICardBuffLifeTimeData data, TriggerContext triggerContext) =>
                ((TurnLifeTimeCardBuffData)data).Turn
                    .Eval(triggerContext)
                    .Map(turn => (ICardBuffLifeTimeEntity)new TurnLifeTimeCardBuffEntity(turn));
        }

        private sealed class HandCardLifeTimeCardBuffEntityCreator : ICardBuffLifeTimeEntityCreator
        {
            public Type DataType => typeof(HandCardLifeTimeCardBuffData);
            public Option<ICardBuffLifeTimeEntity> Create(ICardBuffLifeTimeData data, TriggerContext triggerContext) =>
                ((ICardBuffLifeTimeEntity)new HandCardLifeTimeCardBuffEntity()).Some();
        }
    }

}
