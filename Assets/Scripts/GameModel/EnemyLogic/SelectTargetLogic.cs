using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using UniRx;
using UnityEngine;
namespace MortalGame.GameModel
{

    public record SelectMainTargetResult(
        bool IsValid,
        TargetType TargetType,
        Guid TargetIdentity) : ISelectionTarget;

    public record SelectSubTargetsResult(
        IReadOnlyDictionary<string, ISubSelectionAction> SubSelectionActions);

    public record SelectCardTargetsResult(
        MainSelectionAction MainSelectionAction,
        IReadOnlyDictionary<string, ISubSelectionAction> SubSelectionActions);

    public static class SelectTargetLogic
    {
        public static SelectMainTargetResult SelectMainTarget(
            IGameplayModel gameplayWatcher,
            ICardEntity cardEntity,
            IPlayerEntity selectionPerspective)
        {
            if (selectionPerspective == null)
            {
                return new SelectMainTargetResult(false, TargetType.None, Guid.Empty);
            }

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
                CharacterSelectable => SelectCharacterWithLogic(
                    gameplayWatcher,
                    selectionPerspective,
                    mainSelect.CandidateScope,
                    mainSelect.AutomaticSelection),
                CharacterAllySelectable => SelectCharacter(
                    gameplayWatcher,
                    gameplayWatcher.GameStatus.Ally.Characters,
                    mainSelect.AutomaticSelection),
                CharacterEnemySelectable => SelectCharacter(
                    gameplayWatcher,
                    gameplayWatcher.GameStatus.Enemy.Characters,
                    mainSelect.AutomaticSelection),
                CardSelectable => SelectCardWithLogic(
                    gameplayWatcher,
                    selectionPerspective,
                    mainSelect.CandidateScope,
                    mainSelect.AutomaticSelection),
                CardAllySelectable => SelectCard(
                    gameplayWatcher,
                    gameplayWatcher.GameStatus.Ally.CardManager.HandCard.Cards,
                    mainSelect.AutomaticSelection),
                CardEnemySelectable => SelectCard(
                    gameplayWatcher,
                    gameplayWatcher.GameStatus.Enemy.CardManager.HandCard.Cards,
                    mainSelect.AutomaticSelection),
                _ => new SelectMainTargetResult(false, TargetType.None, Guid.Empty)
            };
        }

        public static Option<SelectSubTargetsResult> SelectSubTargets(
            IGameplayModel gameplayWatcher,
            ICardEntity cardEntity,
            MainSelectionAction mainSelectionAction)
        {
            var subSelectionActions = new Dictionary<string, ISubSelectionAction>();

            var subSelectionInfoOpt = gameplayWatcher.QueryCardSubSelectionInfos(
                cardEntity.Identity,
                mainSelectionAction);
            if (!subSelectionInfoOpt.TryGetValue(out var subSelectionInfo))
            {
                return Option.None<SelectSubTargetsResult>();
            }

            foreach (var kvp in subSelectionInfo.SelectionInfos)
            {
                if (kvp.Value is not ExistCardSelectionInfo existCardGroup)
                {
                    // TODO（T-011／T-012）：NewCard、NewPartialCard、NewEffect 的自動選取尚未實作，
                    // 暫時回傳 None，待各類型的候選與結果契約完成後接線。
                    return Option.None<SelectSubTargetsResult>();
                }

                subSelectionActions[kvp.Key] =
                    RandomSelectExistCardSubSelection(
                        existCardGroup,
                        gameplayWatcher.ContextManager.GameRandom);
            }

            return new SelectSubTargetsResult(subSelectionActions).Some();
        }

        public static Option<SelectCardTargetsResult> SelectTargets(
            IGameplayModel gameplayWatcher,
            ICardEntity cardEntity,
            IPlayerEntity selectionPerspective)
        {
            var mainTarget = SelectMainTarget(
                gameplayWatcher,
                cardEntity,
                selectionPerspective);
            if (!mainTarget.IsValid)
                return Option.None<SelectCardTargetsResult>();

            var mainSelectionAction = MainSelectionAction.Create(mainTarget);
            var subTargetsOption = SelectSubTargets(
                gameplayWatcher,
                cardEntity,
                mainSelectionAction);

            return subTargetsOption.Map(subTargets =>
                new SelectCardTargetsResult(
                    mainSelectionAction,
                    subTargets.SubSelectionActions));
        }

        private static SelectMainTargetResult SelectCharacterWithLogic(
            IGameplayModel gameplayWatcher,
            IPlayerEntity selectionPerspective,
            TargetCandidateScope candidateScope,
            AutomaticTargetSelectionStrategy selectionStrategy)
        {
            return candidateScope switch
            {
                TargetCandidateScope.ToEnemy => SelectCharacter(
                    gameplayWatcher,
                    OppositePlayer(gameplayWatcher, selectionPerspective).Characters,
                    selectionStrategy),
                TargetCandidateScope.ToAlly => SelectCharacter(
                    gameplayWatcher,
                    selectionPerspective.Characters,
                    selectionStrategy),
                TargetCandidateScope.Any => SelectCharacter(
                    gameplayWatcher,
                    gameplayWatcher.GameStatus.Ally.Characters
                        .Concat(gameplayWatcher.GameStatus.Enemy.Characters),
                    selectionStrategy),
                _ => new SelectMainTargetResult(false, TargetType.None, Guid.Empty)
            };
        }

        private static SelectMainTargetResult SelectCardWithLogic(
            IGameplayModel gameplayWatcher,
            IPlayerEntity selectionPerspective,
            TargetCandidateScope candidateScope,
            AutomaticTargetSelectionStrategy selectionStrategy)
        {
            return candidateScope switch
            {
                TargetCandidateScope.ToEnemy => SelectCard(
                    gameplayWatcher,
                    OppositePlayer(gameplayWatcher, selectionPerspective)
                        .CardManager.HandCard.Cards,
                    selectionStrategy),
                TargetCandidateScope.ToAlly => SelectCard(
                    gameplayWatcher,
                    selectionPerspective.CardManager.HandCard.Cards,
                    selectionStrategy),
                TargetCandidateScope.Any => SelectCard(
                    gameplayWatcher,
                    gameplayWatcher.GameStatus.Ally.CardManager.HandCard.Cards
                        .Concat(gameplayWatcher.GameStatus.Enemy.CardManager.HandCard.Cards),
                    selectionStrategy),
                _ => new SelectMainTargetResult(false, TargetType.None, Guid.Empty)
            };
        }

        private static SelectMainTargetResult SelectCharacter(
            IGameplayModel gameplayWatcher,
            IEnumerable<ICharacterEntity> candidates,
            AutomaticTargetSelectionStrategy selectionStrategy)
        {
            var character = SelectCandidate(
                gameplayWatcher,
                candidates,
                selectionStrategy);
            if (character == null)
            {
                return new SelectMainTargetResult(false, TargetType.None, Guid.Empty);
            }

            return character.Owner(gameplayWatcher)
                .Map(owner => new SelectMainTargetResult(
                    true,
                    CharacterTargetType(owner),
                    character.Identity))
                .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
        }

        private static SelectMainTargetResult SelectCard(
            IGameplayModel gameplayWatcher,
            IEnumerable<ICardEntity> candidates,
            AutomaticTargetSelectionStrategy selectionStrategy)
        {
            var card = SelectCandidate(
                gameplayWatcher,
                candidates,
                selectionStrategy);
            if (card == null)
            {
                return new SelectMainTargetResult(false, TargetType.None, Guid.Empty);
            }

            return card.Owner(gameplayWatcher)
                .Map(owner => new SelectMainTargetResult(
                    true,
                    CardTargetType(owner),
                    card.Identity))
                .ValueOr(new SelectMainTargetResult(false, TargetType.None, Guid.Empty));
        }

        private static T SelectCandidate<T>(
            IGameplayModel gameplayWatcher,
            IEnumerable<T> candidates,
            AutomaticTargetSelectionStrategy selectionStrategy)
            where T : class
        {
            var candidateArray = candidates.ToArray();
            if (candidateArray.Length == 0)
            {
                return null;
            }

            return selectionStrategy switch
            {
                AutomaticTargetSelectionStrategy.First => candidateArray[0],
                AutomaticTargetSelectionStrategy.Random => candidateArray[
                    gameplayWatcher.ContextManager.GameRandom.Range(
                        0,
                        candidateArray.Length)],
                _ => null
            };
        }

        private static IPlayerEntity OppositePlayer(
            IGameplayModel gameplayWatcher,
            IPlayerEntity selectionPerspective)
            => selectionPerspective.Faction == Faction.Ally
                ? gameplayWatcher.GameStatus.Enemy
                : gameplayWatcher.GameStatus.Ally;

        private static TargetType CharacterTargetType(IPlayerEntity owner)
            => owner.Faction == Faction.Ally
                ? TargetType.AllyCharacter
                : TargetType.EnemyCharacter;

        private static TargetType CardTargetType(IPlayerEntity owner)
            => owner.Faction == Faction.Ally
                ? TargetType.AllyCard
                : TargetType.EnemyCard;

        private static ExistCardSubSelectionAction RandomSelectExistCardSubSelection(
            ExistCardSelectionInfo existCardGroup,
            IGameRandom gameRandom)
        {
            var selectedCards = existCardGroup.CardInfos
                .Select(cardInfo => cardInfo.Identity)
                .Shuffle(gameRandom)
                .Take(existCardGroup.EffectiveCount)
                .ToList();

            return new ExistCardSubSelectionAction(selectedCards);
        }
    }

}
