using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface ITargetCardBuffValue
    {
        Option<ICardBuffEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NoneCardBuff : ITargetCardBuffValue
    {
        public Option<ICardBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Option.None<ICardBuffEntity>();
        }
    }

    [Serializable]
    public class TriggeredCardBuff : ITargetCardBuffValue
    {
        public Option<ICardBuffEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                CardBuffTrigger cardBuff => cardBuff.Buff.SomeNotNull(),
                _ => Option.None<ICardBuffEntity>()
            };
        }
    }

    [Serializable]
    public class CardBuffById : ITargetCardBuffValue
    {
        [HorizontalGroup("1")]
        public ITargetCardBuffCollectionValue CardBuffs;

        [HorizontalGroup("2")]
        public string BuffId;

        public Option<ICardBuffEntity> Eval(TriggerContext triggerContext)
        {
            return CardBuffs
                .Eval(triggerContext)
                .FirstOrDefault(buff => buff.CardBuffDataID == BuffId)
                .SomeNotNull();
        }
    }

    public interface ITargetCardBuffCollectionValue
    {
        IReadOnlyCollection<ICardBuffEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NoneCardBuffs : ITargetCardBuffCollectionValue
    {
        public IReadOnlyCollection<ICardBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Array.Empty<ICardBuffEntity>();
        }
    }

    [Serializable]
    public class CardBuffsOfCard : ITargetCardBuffCollectionValue
    {
        [HorizontalGroup("1")]
        public ITargetCardValue Card;

        public IReadOnlyCollection<ICardBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Card
                .Eval(triggerContext)
                .Map(card => card.BuffManager.Buffs)
                .ValueOr(Array.Empty<ICardBuffEntity>());
        }
    }

}
