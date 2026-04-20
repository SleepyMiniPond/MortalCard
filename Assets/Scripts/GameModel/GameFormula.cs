using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameFormula
{
    public static int NormalDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
    {
        var actionAddition = GetAttributeAddition(triggerContext, EffectAttributeAdditionType.NormalDamageAddition, PlayerBuffProperty.NormalDamageAddition);
        
        var actionRatio = GetAttributeRatio(triggerContext, EffectAttributeRatioType.NormalDamageRatio, PlayerBuffProperty.NormalDamageRatio);

        return rawDamagePoint + actionAddition;
    }

    public static int PenetrateDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
    {
        var actionAddition = GetAttributeAddition(
            triggerContext, EffectAttributeAdditionType.PenetrateDamageAddition, PlayerBuffProperty.PenetrateDamageAddition);
        
        var actionRatio = GetAttributeRatio(
            triggerContext, EffectAttributeRatioType.PenetrateDamageRatio, PlayerBuffProperty.PenetrateDamageRatio);

        return rawDamagePoint + actionAddition;
    }

    public static int AdditionalDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
    {
        var actionAddition = GetAttributeAddition(
            triggerContext, EffectAttributeAdditionType.AdditionalDamageAddition, PlayerBuffProperty.AdditionalDamageAddition);
        
        var actionRatio = GetAttributeRatio(
            triggerContext, EffectAttributeRatioType.AdditionalDamageRatio, PlayerBuffProperty.AdditionalDamageRatio);

        return rawDamagePoint + actionAddition;
    }
    
    public static int EffectiveDamagePoint(TriggerContext triggerContext, int rawDamagePoint)
    {
        var actionAddition = GetAttributeAddition(
            triggerContext, EffectAttributeAdditionType.EffectiveDamageAddition, PlayerBuffProperty.EffectiveDamageAddition);
        
        var actionRatio = GetAttributeRatio(
            triggerContext, EffectAttributeRatioType.EffectiveDamageRatio, PlayerBuffProperty.EffectiveDamageRatio);

        return rawDamagePoint + actionAddition;
    }

    public static int HealPoint(TriggerContext triggerContext, int rawHealPoint)
    {
        var actionAddition = GetAttributeAddition(
            triggerContext, EffectAttributeAdditionType.HealAddition, PlayerBuffProperty.HealAddition);

        var actionRatio = GetAttributeRatio(
            triggerContext, EffectAttributeRatioType.HealRatio, PlayerBuffProperty.HealRatio);

        return rawHealPoint + actionAddition;
    }

    public static int CardPower(TriggerContext triggerContext, ICardEntity card)
    {
        var actionAddition = GetAttributeAddition(triggerContext, EffectAttributeAdditionType.PowerAddition, PlayerBuffProperty.AllCardPower);

        var cardAddition = card.GetCardProperty(triggerContext, CardProperty.PowerAddition);

        return Math.Max(0, card.OriginPower + actionAddition + cardAddition);
    }

    public static int CardPreviewPower(TriggerContext triggerContext, ICardEntity card)
    {
        var actionAddition = GetAttributeAddition(triggerContext, EffectAttributeAdditionType.PowerAddition, PlayerBuffProperty.AllCardPower);

        var cardAddition = card.GetCardProperty(triggerContext, CardProperty.PowerAddition);

        return Math.Max(0, card.OriginPower + actionAddition + cardAddition);
    }

    public static int CardCost(TriggerContext triggerContext, ICardEntity card)
    {
        var actionAddition = GetAttributeAddition(triggerContext, EffectAttributeAdditionType.CostAddition, PlayerBuffProperty.AllCardCost);

        var cardAddition = card.GetCardProperty(triggerContext, CardProperty.CostAddition);
        return Math.Max(0, card.OriginCost + actionAddition + cardAddition);
    }

    private static int GetAttributeAddition(
        TriggerContext triggerContext,
        EffectAttributeAdditionType attribute,
        PlayerBuffProperty playerBuffProperty)
    {
        if (triggerContext.Action is CardLookIntentAction cardLookIntent)
        {
            var playerAttribute = cardLookIntent.Card.Owner(triggerContext.Model)
                .Map(player => player.GetPlayerBuffAdditionProperty(triggerContext, playerBuffProperty))
                .ValueOr(0);
            return playerAttribute;
        }
        if (triggerContext.Action.Source is CardPlaySource cardPlaySource)
        {
            var cardPlayAttribute = cardPlaySource.Attribute.IntValues
                .GetValueOrDefault(attribute, 0);
            var playerAttribute = triggerContext.Model.GameStatus.CurrentPlayer.Value
                .Map(player => player.GetPlayerBuffAdditionProperty(triggerContext, playerBuffProperty))
                .ValueOr(0);

            return cardPlayAttribute + playerAttribute;
        }
        return 0;
    }
    private static float GetAttributeRatio(
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
