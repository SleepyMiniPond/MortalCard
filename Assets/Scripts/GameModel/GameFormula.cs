using System;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;
using UnityEngine;

namespace MortalGame.GameModel
{

    public static class GameFormula
    {
        public static Option<int> NormalDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
        {
            var actionAddition = _GetAttributeAddition(triggerContext, EffectAttributeAdditionType.NormalDamageAddition, PlayerBuffProperty.NormalDamageAddition);

            var actionRatio = _GetAttributeRatio(triggerContext, EffectAttributeRatioType.NormalDamageRatio, PlayerBuffProperty.NormalDamageRatio);

            return actionAddition.FlatMap(addition => GameplayIntegerMath.Add(rawDamagePoint, addition));
        }

        public static Option<int> PenetrateDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
        {
            var actionAddition = _GetAttributeAddition(
                triggerContext, EffectAttributeAdditionType.PenetrateDamageAddition, PlayerBuffProperty.PenetrateDamageAddition);

            var actionRatio = _GetAttributeRatio(
                triggerContext, EffectAttributeRatioType.PenetrateDamageRatio, PlayerBuffProperty.PenetrateDamageRatio);

            return actionAddition.FlatMap(addition => GameplayIntegerMath.Add(rawDamagePoint, addition));
        }

        public static Option<int> AdditionalDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
        {
            var actionAddition = _GetAttributeAddition(
                triggerContext, EffectAttributeAdditionType.AdditionalDamageAddition, PlayerBuffProperty.AdditionalDamageAddition);

            var actionRatio = _GetAttributeRatio(
                triggerContext, EffectAttributeRatioType.AdditionalDamageRatio, PlayerBuffProperty.AdditionalDamageRatio);

            return actionAddition.FlatMap(addition => GameplayIntegerMath.Add(rawDamagePoint, addition));
        }

        public static Option<int> EffectiveDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
        {
            var actionAddition = _GetAttributeAddition(
                triggerContext, EffectAttributeAdditionType.EffectiveDamageAddition, PlayerBuffProperty.EffectiveDamageAddition);

            var actionRatio = _GetAttributeRatio(
                triggerContext, EffectAttributeRatioType.EffectiveDamageRatio, PlayerBuffProperty.EffectiveDamageRatio);

            return actionAddition.FlatMap(addition => GameplayIntegerMath.Add(rawDamagePoint, addition));
        }

        public static Option<int> HealPoint(TriggerContext triggerContext, int rawHealPoint)
        {
            var actionAddition = _GetAttributeAddition(
                triggerContext, EffectAttributeAdditionType.HealAddition, PlayerBuffProperty.HealAddition);

            var actionRatio = _GetAttributeRatio(
                triggerContext, EffectAttributeRatioType.HealRatio, PlayerBuffProperty.HealRatio);

            return actionAddition.FlatMap(addition => GameplayIntegerMath.Add(rawHealPoint, addition));
        }

        public static Option<int> CardPower(TriggerContext triggerContext, ICardEntity card)
        {
            var actionAddition = _GetAttributeAddition(triggerContext, EffectAttributeAdditionType.PowerAddition, PlayerBuffProperty.AllCardPower);

            var cardAddition = card.GetCardProperty(triggerContext, CardProperty.PowerAddition);

            return _CalculateCardValue(card.OriginPower, actionAddition, cardAddition);
        }

        public static Option<int> CardPreviewPower(TriggerContext triggerContext, ICardEntity card)
        {
            var actionAddition = _GetAttributeAddition(triggerContext, EffectAttributeAdditionType.PowerAddition, PlayerBuffProperty.AllCardPower);

            var cardAddition = card.GetCardProperty(triggerContext, CardProperty.PowerAddition);

            return _CalculateCardValue(card.OriginPower, actionAddition, cardAddition);
        }

        public static Option<int> CardCost(TriggerContext triggerContext, ICardEntity card)
        {
            var actionAddition = _GetAttributeAddition(triggerContext, EffectAttributeAdditionType.CostAddition, PlayerBuffProperty.AllCardCost);

            var cardAddition = card.GetCardProperty(triggerContext, CardProperty.CostAddition);
            return _CalculateCardValue(card.OriginCost, actionAddition, cardAddition);
        }

        private static Option<int> _CalculateCardValue(
            int originValue,
            Option<int> actionAddition,
            Option<int> cardAddition)
        {
            return actionAddition
                .Combine(cardAddition)
                .FlatMap(values => GameplayIntegerMath.Add(originValue, values.Item1)
                    .FlatMap(value => GameplayIntegerMath.Add(value, values.Item2)))
                .Map(value => Math.Max(0, value));
        }

        private static Option<int> _GetAttributeAddition(
            TriggerContext triggerContext,
            EffectAttributeAdditionType attribute,
            PlayerBuffProperty playerBuffProperty)
        {
            if (triggerContext.Action is CardLookIntentAction cardLookIntent)
            {
                return cardLookIntent.Card.Owner(triggerContext.Model)
                    .FlatMap(player => player.GetPlayerBuffAdditionProperty(triggerContext, playerBuffProperty));
            }
            if (triggerContext.Action.Source is CardPlaySource cardPlaySource)
            {
                var cardPlayAttribute = cardPlaySource.Attribute.IntValues
                    .GetValueOrDefault(attribute, 0);
                return triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .FlatMap(player => player.GetPlayerBuffAdditionProperty(triggerContext, playerBuffProperty))
                    .FlatMap(playerAttribute => GameplayIntegerMath.Add(cardPlayAttribute, playerAttribute));
            }
            return 0.Some();
        }
        private static float _GetAttributeRatio(
            TriggerContext triggerContext,
            EffectAttributeRatioType attribute,
            PlayerBuffProperty playerBuffProperty)
        {
            if (triggerContext.Action.Source is CardPlaySource cardPlaySource)
            {
                var cardPlayAttribute = cardPlaySource.Attribute.FloatValues
                    .GetValueOrDefault(attribute, 0);
                var playerAttribute = triggerContext.Model.GameStatus.CurrentPlayer.Value
                    .Map(player => player.GetPlayerBuffRatioProperty(triggerContext, playerBuffProperty))
                    .ValueOr(0);

                return cardPlayAttribute + playerAttribute;
            }
            return 0f;
        }
    }

}
