using System;
using System.Collections.Generic;
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
                        new TriggerContext(
                            model, 
                            new CardTrigger(card), 
                            new CardLookIntentAction(
                                card, 
                                ((IPlayerEntity)enemy).Some())),
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
            IReadOnlyCollection<Guid> attemptedCardIdentities,
            out UseCardAction useCardAction,
            out CardSelectionSession selectionSession)
        {
            foreach (var selectedCard in enemy.SelectedCards.Cards)
            {
                if (attemptedCardIdentities.Contains(selectedCard.Identity))
                    continue;

                if (GameFormula.CardCost(
                            new TriggerContext(
                                model, 
                                new CardTrigger(selectedCard), 
                                new CardLookIntentAction(
                                    selectedCard, 
                                    ((IPlayerEntity)enemy).Some())),
                            selectedCard)
                        .TryGetValue(out var cardRuntimeCost) &&
                    cardRuntimeCost <= enemy.CurrentEnergy &&
                    SelectTargetLogic.SelectTargets(model, selectedCard, enemy).TryGetValue(out var selectResult))
                {
                    useCardAction = new UseCardAction(
                        selectedCard.Identity,
                        selectResult.MainSelectionAction,
                        selectResult.SubSelectionActions);
                    selectionSession = selectResult.Session;
                    return true;
                }
            }

            useCardAction = null;
            selectionSession = null;
            return false;
        }
    }

}
