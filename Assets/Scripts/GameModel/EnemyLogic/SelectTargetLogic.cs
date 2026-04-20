using System;
using System.Collections.Generic;
using System.Linq;
using Optional.Collections;
using UniRx;
using UnityEngine;

public record SelectMainTargetResult(
    bool IsValid,
    TargetType TargetType,
    Guid TargetIdentity) : ISelectionTarget;

public record SelectSubTargetsResult(
    IReadOnlyDictionary<string, ISubSelectionAction> SubSelectionActions);

public static class SelectTargetLogic
{
    public static SelectMainTargetResult SelectMainTarget(
        IGameplayModel gameplayWatcher,
        ICardEntity cardEntity)
    {
        var mainSelect = cardEntity.MainSelect;
        if (mainSelect == null)
        {
            Debug.LogError($"MainSelect is null. cardId={cardEntity.CardDataId}");
            return new SelectMainTargetResult(false, TargetType.None, Guid.Empty);
        }

        var selectable = mainSelect.MainSelectable;
        if (selectable == null)
        {
            Debug.LogError($"MainSelectable is null. cardId={cardEntity.CardDataId}");
            return new SelectMainTargetResult(false, TargetType.None, Guid.Empty);
        }

        return selectable switch
        {
            NoneSelectable => new SelectMainTargetResult(true, TargetType.None, Guid.Empty),
            CharacterSelectable => SelectCharacterWithLogic(gameplayWatcher, mainSelect.LogicTag),
            CharacterAllySelectable => SelectAllyCharacter(gameplayWatcher),
            CharacterEnemySelectable => SelectEnemyCharacter(gameplayWatcher),
            CardSelectable => SelectCardWithLogic(gameplayWatcher, mainSelect.LogicTag),
            CardAllySelectable => SelectAllyCard(gameplayWatcher),
            CardEnemySelectable => SelectEnemyCard(gameplayWatcher),
            _ => new SelectMainTargetResult(false, TargetType.None, Guid.Empty)
        };
    }

    public static SelectSubTargetsResult SelectSubTargets(
        IGameplayModel gameplayWatcher,
        ICardEntity cardEntity)
    {
        var subSelectionActions = new Dictionary<string, ISubSelectionAction>();

        var subSelectionInfoOpt = gameplayWatcher.QueryCardSubSelectionInfos(cardEntity.Identity);
        if (subSelectionInfoOpt.TryGetValue(out var subSelectionInfo))
        {
            foreach (var kvp in subSelectionInfo.SelectionInfos)
            {
                switch (kvp.Value)
                {
                    case ExistCardSelectionInfo existCardGroup:
                        subSelectionActions[kvp.Key] =
                            RandomSelectExistCardSubSelection(existCardGroup);
                        break;
                    case NewCardSelectionInfo:
                        subSelectionActions[kvp.Key] = new NewCardSubSelectionAction();
                        break;
                    case NewPartialCardSelectionInfo:
                        subSelectionActions[kvp.Key] = new NewPartialCardSubSelectionAction();
                        break;
                    case NewEffectSelectionInfo:
                        subSelectionActions[kvp.Key] = new NewEffectSubSelectionAction();
                        break;
                }
            }
        }

        return new SelectSubTargetsResult(subSelectionActions);
    }

    private static SelectMainTargetResult SelectCharacterWithLogic(IGameplayModel gameplayWatcher, TargetLogicTag logicTag)
    {
        return logicTag switch
        {
            TargetLogicTag.ToEnemy => SelectEnemyCharacter(gameplayWatcher),
            TargetLogicTag.ToAlly => SelectAllyCharacter(gameplayWatcher),
            TargetLogicTag.ToRandom => SelectRandomCharacter(gameplayWatcher),
            _ => new SelectMainTargetResult(false, TargetType.None, Guid.Empty)
        };
    }

    private static SelectMainTargetResult SelectCardWithLogic(IGameplayModel gameplayWatcher, TargetLogicTag logicTag)
    {
        return logicTag switch
        {
            TargetLogicTag.ToEnemy => SelectEnemyCard(gameplayWatcher),
            TargetLogicTag.ToAlly => SelectAllyCard(gameplayWatcher),
            TargetLogicTag.ToRandom => SelectRandomCard(gameplayWatcher),
            _ => new SelectMainTargetResult(false, TargetType.None, Guid.Empty)
        };
    }

    private static SelectMainTargetResult SelectEnemyCharacter(IGameplayModel gameplayWatcher)
    {
        return gameplayWatcher.GameStatus.OppositePlayer.Value
            .FlatMap(oppositePlayer => LinqEnumerableExtensions.FirstOrNone(oppositePlayer.Characters))
            .Map(oppositeCharacter => new SelectMainTargetResult(true, TargetType.EnemyCharacter, oppositeCharacter.Identity))
            .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
    }

    private static SelectMainTargetResult SelectAllyCharacter(IGameplayModel gameplayWatcher)
    {
        return gameplayWatcher.GameStatus.CurrentPlayer.Value
            .FlatMap(currentPlayer => LinqEnumerableExtensions.FirstOrNone(currentPlayer.Characters))
            .Map(currentCharacter => new SelectMainTargetResult(true, TargetType.AllyCharacter, currentCharacter.Identity))
            .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
    }

    private static SelectMainTargetResult SelectRandomCharacter(IGameplayModel gameplayWatcher)
    {
        return LinqEnumerableExtensions.FirstOrNone(
            gameplayWatcher.GameStatus.Ally.Characters
                .Concat(gameplayWatcher.GameStatus.Enemy.Characters))
            .FlatMap(randomCharacter => randomCharacter.Owner(gameplayWatcher)
                .Map(randomPlayer => (randomPlayer, randomCharacter)))
            .Map(tuple => new SelectMainTargetResult(true,
                tuple.randomPlayer.Faction == Faction.Ally ? TargetType.AllyCharacter : TargetType.EnemyCharacter,
                tuple.randomCharacter.Identity))
            .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
    }

    private static SelectMainTargetResult SelectEnemyCard(IGameplayModel gameplayWatcher)
    {
        return gameplayWatcher.GameStatus.OppositePlayer.Value
            .FlatMap(oppositePlayer => LinqEnumerableExtensions.FirstOrNone(oppositePlayer.CardManager.HandCard.Cards))
            .Map(oppositeCard => new SelectMainTargetResult(true, TargetType.EnemyCard, oppositeCard.Identity))
            .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
    }

    private static SelectMainTargetResult SelectAllyCard(IGameplayModel gameplayWatcher)
    {
        return gameplayWatcher.GameStatus.CurrentPlayer.Value
            .FlatMap(currentPlayer => LinqEnumerableExtensions.FirstOrNone(currentPlayer.CardManager.HandCard.Cards))
            .Map(currentCard => new SelectMainTargetResult(true, TargetType.AllyCard, currentCard.Identity))
            .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
    }

    private static SelectMainTargetResult SelectRandomCard(IGameplayModel gameplayWatcher)
    {
        return LinqEnumerableExtensions.FirstOrNone(
            gameplayWatcher.GameStatus.Ally.CardManager.HandCard.Cards
                .Concat(gameplayWatcher.GameStatus.Enemy.CardManager.HandCard.Cards))
            .FlatMap(randomCard => randomCard.Owner(gameplayWatcher)
                .Map(randomPlayer => (randomPlayer, randomCard)))
            .Map(tuple => new SelectMainTargetResult(true,
                tuple.randomPlayer.Faction == Faction.Ally ? TargetType.AllyCard : TargetType.EnemyCard,
                tuple.randomCard.Identity))
            .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
    }

    private static ExistCardSubSelectionAction RandomSelectExistCardSubSelection(ExistCardSelectionInfo existCardGroup)
    {
        var selectCount = Math.Min(
            existCardGroup.Count,
            existCardGroup.CardInfos.Count);

        var selectedCards = existCardGroup.CardInfos
            .Select(cardInfo => cardInfo.Identity)
            .Shuffle()
            .Take(selectCount)
            .ToList();

        return new ExistCardSubSelectionAction(selectedCards);
    }
}
