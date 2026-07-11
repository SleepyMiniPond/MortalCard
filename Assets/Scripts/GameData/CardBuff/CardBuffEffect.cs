using System;
using MortalGame.GameModel;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{

    [Serializable]
    public class ConditionalCardBuffEffect
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ICardBuffCondition> Conditions = new();

        [Space(20)]
        [HorizontalGroup("2")]
        public ICardBuffEffect Effect;
    }

}
