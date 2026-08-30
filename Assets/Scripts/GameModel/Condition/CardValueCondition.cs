using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface ICardValueCondition
    {
        bool Eval(TriggerContext triggerContext, ICardEntity card);
    }

    [Serializable]
    public class CardIdentityCondition : ICardValueCondition
    {
        [HorizontalGroup("1")]
        public ITargetCardValue CompareCard;

        public bool Eval(TriggerContext triggerContext, ICardEntity card)
        {
            return CompareCard
                .Eval(triggerContext)
                .Match(
                    compareCard => card.Identity == compareCard.Identity,
                    () => false);
        }
    }

    [Serializable]
    public class BaseCardDataIdCondition : ICardValueCondition
    {
        [HorizontalGroup("1")]
        public ITargetCardValue CompareCard;

        public bool Eval(TriggerContext triggerContext, ICardEntity card)
        {
            return CompareCard.Eval(triggerContext).Match(
                compareCard => card.BaseCardDataId == compareCard.BaseCardDataId,
                () => false);
        }
    }

    [Serializable]
    public class CardFormCondition : ICardValueCondition
    {
        [HorizontalGroup("1")]
        public ITargetCardValue CompareCard;

        public bool Eval(TriggerContext triggerContext, ICardEntity card)
        {
            return CompareCard.Eval(triggerContext).Match(
                compareCard => card.CardDataId == compareCard.CardDataId,
                () => false);
        }
    }

    [Serializable]
    public class CardTypesCondition : ICardValueCondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<CardType> CardTypes = new();

        public SetConditionType Condition;

        public bool Eval(TriggerContext triggerContext, ICardEntity card)
        {
            return Condition.Eval(CardTypes, type => type == card.Type);
        }
    }

    [Serializable]
    public class CardThemesCondition : ICardValueCondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<CardTheme> CardThemes = new();
        public SetConditionType Condition;

        public bool Eval(TriggerContext triggerContext, ICardEntity card)
        {
            return Condition.Eval(CardThemes, theme => card.Themes.Contains(theme));
        }
    }

    [Serializable]
    public class CardRaritiesCondition : ICardValueCondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<CardRarity> CardRarities = new();
        public SetConditionType Condition;

        public bool Eval(TriggerContext triggerContext, ICardEntity card)
        {
            return Condition.Eval(CardRarities, rarity => rarity == card.Rarity);
        }
    }

    [Serializable]
    public class CardPropertiesCondition : ICardValueCondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<CardProperty> CardProperties = new();
        public SetConditionType Condition;

        public bool Eval(TriggerContext triggerContext, ICardEntity card)
        {
            return Condition.Eval(CardProperties, card.HasProperty);
        }
    }

}
