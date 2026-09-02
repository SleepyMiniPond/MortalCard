using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace MortalGame.GameModel
{

    public interface IPlayerBuffValueCondition
    {
        bool Eval(TriggerContext triggerContext, IPlayerBuffEntity playerBuff);
    }

    [Serializable]
    public class PlayerBuffSessionCondition : IPlayerBuffValueCondition
    {
        [HorizontalGroup("1")]
        public string SessionKey;
        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<IReactionSessionValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext, IPlayerBuffEntity playerBuff)
        {
            return playerBuff.ReactionSessions.TryGetValue(SessionKey, out var session) &&
                Conditions.All(condition => condition.Eval(triggerContext, session));
        }
    }

    [Serializable]
    public class PlayerBuffDataIdCondition : IPlayerBuffValueCondition
    {
        public string BuffId;

        public bool Eval(TriggerContext triggerContext, IPlayerBuffEntity playerBuff)
        {
            return playerBuff.PlayerBuffDataId == BuffId;
        }
    }

}
