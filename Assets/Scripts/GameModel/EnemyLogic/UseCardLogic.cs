using System.Linq;
using Optional;
using Optional.Collections;
using UnityEngine;

namespace MortalGame.GameModel
{

    public static class UseCardLogic
    {
        public static bool TryGetRecommandSelectCard(
            IGameplayModel model,
            EnemyEntity enemy,
            out ICardEntity cardEntity
        )
        {
            if (!enemy.SelectedCards
                    .EvalTotalCost(model)
                    .TryGetValue(out var totalSelectedCost))
            {
                cardEntity = null;
                return false;
            }

            var remainCost = enemy.CurrentEnergy - totalSelectedCost;

            var candidateCards = enemy.CardManager.HandCard.Cards
                .Where(card => !enemy.SelectedCards.Cards.Contains(card))
                .Select(card => GameFormula.CardCost(
                        new TriggerContext(model, new CardTrigger(card), new CardLookIntentAction(card)),
                        card)
                    .Map(cost => (Card: card, Cost: cost)))
                .Values()
                .Where(candidate => candidate.Cost <= remainCost);

            var highestCard = candidateCards
                .OrderByDescending(candidate => candidate.Cost)
                .FirstOrDefault();
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

                if (!SelectTargetLogic.SelectSubTargets(model, selectedCard)
                        .TryGetValue(out var subSelectResult))
                {
                    continue;
                }

                if (GameFormula.CardCost(
                            new TriggerContext(model, new CardTrigger(selectedCard), new CardLookIntentAction(selectedCard)),
                            selectedCard)
                        .TryGetValue(out var cardRuntimeCost) &&
                    cardRuntimeCost <= enemy.CurrentEnergy)
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

}
