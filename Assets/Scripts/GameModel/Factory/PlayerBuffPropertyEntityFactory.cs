using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.GameModel
{

    public interface IPlayerBuffPropertyEntityCreator
    {
        Type DataType { get; }
        IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data);
    }

    public interface IPlayerBuffPropertyEntityFactory
    {
        IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data);
    }

    public sealed class PlayerBuffPropertyEntityFactory : IPlayerBuffPropertyEntityFactory
    {
        private readonly IReadOnlyDictionary<Type, IPlayerBuffPropertyEntityCreator> _creators;

        public PlayerBuffPropertyEntityFactory(IEnumerable<IPlayerBuffPropertyEntityCreator> creators)
        {
            _creators = creators.ToDictionary(creator => creator.DataType);
        }

        public static PlayerBuffPropertyEntityFactory CreateDefault()
        {
            return new PlayerBuffPropertyEntityFactory(new IPlayerBuffPropertyEntityCreator[]
            {
            new AllCardPowerCreator(), new AllCardCostCreator(), new NormalDamageAdditionCreator(),
            new NormalDamageRatioCreator(), new MaxHealthCreator(), new MaxEnergyCreator(),
            });
        }

        public IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (_creators.TryGetValue(data.GetType(), out var creator)) return creator.Create(data);
            throw new ArgumentException($"未註冊的 Player Buff Property Data 型別：{data.GetType().FullName}", nameof(data));
        }

        private sealed class AllCardPowerCreator : IPlayerBuffPropertyEntityCreator { public Type DataType => typeof(AllCardPowerPlayerBuffPropertyData); public IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data) => new AllCardPowerPlayerBuffPropertyEntity(((AllCardPowerPlayerBuffPropertyData)data).Value); }
        private sealed class AllCardCostCreator : IPlayerBuffPropertyEntityCreator { public Type DataType => typeof(AllCardCostPlayerBuffPropertyData); public IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data) => new AllCardCostPlayerBuffPropertyEntity(((AllCardCostPlayerBuffPropertyData)data).Value); }
        private sealed class NormalDamageAdditionCreator : IPlayerBuffPropertyEntityCreator { public Type DataType => typeof(NormalDamageAdditionPlayerBuffPropertyData); public IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data) => new NormalDamageAdditionPlayerBuffPropertyEntity(((NormalDamageAdditionPlayerBuffPropertyData)data).Value); }
        private sealed class NormalDamageRatioCreator : IPlayerBuffPropertyEntityCreator { public Type DataType => typeof(NormalDamageRatioPlayerBuffPropertyData); public IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data) => new NormalDamageRatioPlayerBuffPropertyEntity(((NormalDamageRatioPlayerBuffPropertyData)data).Value); }
        private sealed class MaxHealthCreator : IPlayerBuffPropertyEntityCreator { public Type DataType => typeof(MaxHealthPlayerBuffPropertyData); public IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data) => new MaxHealthPlayerBuffPropertyEntity(((MaxHealthPlayerBuffPropertyData)data).Value); }
        private sealed class MaxEnergyCreator : IPlayerBuffPropertyEntityCreator { public Type DataType => typeof(MaxEnergyPlayerBuffPropertyData); public IPlayerBuffPropertyEntity Create(IPlayerBuffPropertyData data) => new MaxEnergyPlayerBuffPropertyEntity(((MaxEnergyPlayerBuffPropertyData)data).Value); }
    }

}
