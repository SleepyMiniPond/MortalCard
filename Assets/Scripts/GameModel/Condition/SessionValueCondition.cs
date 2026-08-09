using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace MortalGame.GameModel
{

    public interface IReactionSessionValueCondition
    {
        bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity);
    }

    [Serializable]
    public class ReactorSessionUpdatedCondition : IReactionSessionValueCondition
    {
        public bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity)
        {
            return sessionEntity.IsSessionValueUpdated;
        }
    }
    [Serializable]
    public class ReactionSessionValueBooleanCondition : IReactionSessionValueCondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<IBooleanValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity)
        {
            return sessionEntity
                .BooleanValue
                .Match(
                    value => Conditions.All(condition => condition.Eval(triggerContext, value)),
                    () => false);
        }
    }

    [Serializable]
    public class ReactionSessionValueIntegerCondition : IReactionSessionValueCondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<IIntegerValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext, IReactionSessionEntity sessionEntity)
        {
            return sessionEntity
                .IntegerValue
                .Match(
                    value => Conditions.All(condition => condition.Eval(triggerContext, value)),
                    () => false);
        }
    }

    /// <summary>
    /// 讀取目前卡牌 External Override 所持有的 ReactionSession。
    /// </summary>
    [Serializable]
    public sealed class CardFormOverrideSessionCondition : ICondition
    {
        public string SessionKey;

        [ShowInInspector]
        public List<IReactionSessionValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered is CardFormOverrideTrigger trigger &&
                trigger.State.ReactionSessions.TryGetValue(SessionKey, out var session) &&
                Conditions.All(condition => condition.Eval(triggerContext, session));
        }
    }

}
