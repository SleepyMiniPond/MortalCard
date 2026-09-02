using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface ITargetPlayerBuffValue
    {
        Option<IPlayerBuffEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NoneBuff : ITargetPlayerBuffValue
    {
        public Option<IPlayerBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Option.None<IPlayerBuffEntity>();
        }
    }
    [Serializable]
    public class TriggeredPlayerBuff : ITargetPlayerBuffValue
    {
        public Option<IPlayerBuffEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                PlayerBuffTrigger playerBuffTrigger => playerBuffTrigger.Buff.Some(),
                _ => Option.None<IPlayerBuffEntity>()
            };
        }
    }

    [Serializable]
    public class PlayerBuffById : ITargetPlayerBuffValue
    {
        [HorizontalGroup("1")]
        public ITargetPlayerBuffCollectionValue PlayerBuffs;

        [HorizontalGroup("2")]
        public string BuffId;

        public Option<IPlayerBuffEntity> Eval(TriggerContext triggerContext)
        {
            return PlayerBuffs
                .Eval(triggerContext)
                .FirstOrDefault(buff => buff.PlayerBuffDataId == BuffId)
                .SomeNotNull();
        }
    }

    public interface ITargetPlayerBuffCollectionValue
    {
        IReadOnlyCollection<IPlayerBuffEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NonePlayerBuffs : ITargetPlayerBuffCollectionValue
    {
        public IReadOnlyCollection<IPlayerBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Array.Empty<IPlayerBuffEntity>();
        }
    }

    [Serializable]
    public class PlayerBuffsOfPlayer : ITargetPlayerBuffCollectionValue
    {
        [HorizontalGroup("1")]
        public ITargetPlayerValue Player;

        public IReadOnlyCollection<IPlayerBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Player
                .Eval(triggerContext)
                .Map(player => player.BuffManager.Buffs)
                .ValueOr(Array.Empty<IPlayerBuffEntity>());
        }
    }

}
