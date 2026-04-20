using System.Linq;
using Optional;
using Optional.Collections;
using UnityEngine;

public static class UseCardLogic
{
    public static bool TryGetRecommandSelectCard(
        IGameplayModel model,
        EnemyEntity enemy,
        out ICardEntity cardEntity
    )
    {
        var totalSelectedCost = enemy.SelectedCards.Cards
            .Sum(card => GameFormula.CardCost(
                new TriggerContext(model, new CardTrigger(card), new CardLookIntentAction(card)),
                card));
        var remainCost = enemy.CurrentEnergy - totalSelectedCost;

        var candidateCards = enemy.CardManager.HandCard.Cards
            .Where(card => !enemy.SelectedCards.Cards.Contains(card))
            .Select(card => (
                Card: card,
                Cost: GameFormula.CardCost(
                    new TriggerContext(model, new CardTrigger(card), new CardLookIntentAction(card)), card)
            ))
            .Where(c => c.Cost <= remainCost);

        var highestCard = candidateCards.OrderByDescending(c => c.Cost).FirstOrDefault();
        if (highestCard != default)
        {
            cardEntity = highestCard.Card;
            return true;
        }

        cardEntity = null;
        return false;
    }

    public static bool TryGetNextUseCardAction(
        IGameplayModel model,
        EnemyEntity enemy,
        out UseCardAction useCardAction)
    {
        foreach (var selectedCard in enemy.SelectedCards.Cards)
        {
            var selectResult = SelectTargetLogic.SelectMainTarget(model, selectedCard);
            if (!selectResult.IsValid) continue;

            var subSelectResult = SelectTargetLogic.SelectSubTargets(model, selectedCard);

            var cardRuntimeCost = GameFormula.CardCost(new TriggerContext(model, new CardTrigger(selectedCard), new CardLookIntentAction(selectedCard)), selectedCard);
            if (cardRuntimeCost <= enemy.CurrentEnergy)
            {
                useCardAction = new UseCardAction(
                    selectedCard.Identity,
                    MainSelectionAction.Create(selectResult),
                    subSelectResult.SubSelectionActions);
                return true;
            }
        }

        useCardAction = null;
        return false;
    }
}
