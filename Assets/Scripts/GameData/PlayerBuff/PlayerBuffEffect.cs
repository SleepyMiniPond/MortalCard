using System;
using MortalGame.GameModel;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

    [Serializable]
    public class ConditionalPlayerBuffEffect
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ICondition> Conditions = new();

        [Space(20)]
        [HorizontalGroup("2")]
        public IPlayerBuffEffect Effect;
    }

    [Serializable]
    public class ModifyCardPlayAttributeEffect :
        IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        [HorizontalGroup("1")]
        public EffectAttributeAdditionType Type;

        [HorizontalGroup("2")]
        public IIntegerValue Value;
    }

}

