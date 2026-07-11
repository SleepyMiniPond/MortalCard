using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

    public interface IReactionSessionData
    {
    }


    [Serializable]
    public class SessionBoolean : IReactionSessionData
    {
        [Serializable]
        public class TimingRule
        {
            [ValueDropdown("@DropdownHelper.UpdateTimings")]
            public GameTiming Timing;

            public ConditionBooleanUpdateRule[] Rules;
        }

        public bool InitialValue;
        public SessionLifeTime LifeTime;

        [ShowInInspector]
        [TableList]
        public List<TimingRule> UpdateRules = new();

    }

    [Serializable]
    public class SessionInteger : IReactionSessionData
    {
        [Serializable]
        public class TimingRule
        {
            [ValueDropdown("@DropdownHelper.UpdateTimings")]
            public GameTiming Timing;

            public ConditionIntegerUpdateRule[] Rules;
        }

        public int InitialValue;
        public SessionLifeTime LifeTime;

        [ShowInInspector]
        [TableList]
        public List<TimingRule> UpdateRules = new();

    }

}
