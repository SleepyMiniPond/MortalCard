using System;
using System.Collections.Generic;
using MortalGame.GameData;
using Optional;
using System.Linq;

namespace MortalGame.GameModel
{

    public record MainSelectionInfo(
        SelectType SelectType,
        TargetCandidateScope CandidateScope,
        AutomaticTargetSelectionStrategy AutomaticSelection);

    public abstract record SubSelectionStepInfo(
        string Id,
        int StepIndex,
        int StepCount,
        string CardDataId);

    public record ExistCardSelectionInfo(
        IReadOnlyList<CardInfo> CardInfos,
        int Count,
        CardSelectionCountRule CountRule,
        string Id,
        int StepIndex,
        int StepCount,
        string CardDataId) : SubSelectionStepInfo(Id, StepIndex, StepCount, CardDataId)
    {
        public int MaxCount => Math.Min(Count, CardInfos.Count);
        public int MinCount => CountRule == CardSelectionCountRule.Optional ? 0 : MaxCount;
        public bool IsValidCount(int count) => count >= MinCount && count <= MaxCount;
    }

    public static class SelectionInfoUtility
    {
        public static MainSelectionInfo ToInfo(this MainTargetSelectLogic mainTargetLogic)
        {
            return new MainSelectionInfo(
                mainTargetLogic.MainSelectable.SelectType,
                mainTargetLogic.CandidateScope,
                mainTargetLogic.AutomaticSelection);
        }

        internal static Option<GameContext> TryCreateMainSelectionContext(
            IGameplayModel model,
            ICardEntity cardEntity,
            MainSelectionAction mainSelectionAction)
        {
            var selectType = cardEntity.MainSelect.MainSelectable.SelectType;
            if (selectType == SelectType.None)
            {
                return mainSelectionAction.TargetType == TargetType.None
                    ? GameContext.EMPTY.Some()
                    : Option.None<GameContext>();
            }

            if (!selectType.IsSelectable(mainSelectionAction.TargetType) ||
                !mainSelectionAction.SelectedTarget.TryGetValue(out var targetIdentity) ||
                targetIdentity == Guid.Empty)
            {
                return Option.None<GameContext>();
            }

            switch (mainSelectionAction.TargetType)
            {
                case TargetType.AllyCharacter:
                case TargetType.EnemyCharacter:
                    return model.GetCharacter(targetIdentity).HasValue
                        ? (GameContext.EMPTY with
                        {
                            SelectedCharacter = targetIdentity
                        }).Some()
                        : Option.None<GameContext>();
                case TargetType.AllyCard:
                case TargetType.EnemyCard:
                    return model.GetCard(targetIdentity).HasValue
                        ? (GameContext.EMPTY with
                        {
                            SelectedCard = targetIdentity
                        }).Some()
                        : Option.None<GameContext>();
                default:
                    return Option.None<GameContext>();
            }
        }

        internal static Option<GameContext> TryCreateUseCardContext(
            IGameplayModel model,
            ICardEntity cardEntity,
            UseCardAction useCardAction,
            IPlayerEntity caster)
            => TryCreateCardEffectContext(
                model, 
                cardEntity,
                useCardAction.MainSelectionAction,
                useCardAction.SubSelectionActions, 
                caster);

        internal static Option<GameContext> TryCreateCardEffectContext(
            IGameplayModel model,
            ICardEntity cardEntity,
            MainSelectionAction mainSelectionAction,
            IReadOnlyDictionary<string, ISubSelectionAction> subSelectionActions,
            IPlayerEntity caster)
        {
            return CardSelectionQuery.ValidateComplete(model, cardEntity, caster, mainSelectionAction, subSelectionActions)
                is CardSelectionQueryResult.Completed completed
                ? completed.Context.Some()
                : Option.None<GameContext>();
        }
        public static bool IsSelectable(this SelectType selectType, TargetType targetType)
        {
            switch (selectType)
            {
                case SelectType.Character:
                    return targetType == TargetType.AllyCharacter ||
                           targetType == TargetType.EnemyCharacter;
                case SelectType.AllyCharacter:
                    return targetType == TargetType.AllyCharacter;
                case SelectType.EnemyCharacter:
                    return targetType == TargetType.EnemyCharacter;
                case SelectType.Card:
                    return targetType == TargetType.AllyCard ||
                           targetType == TargetType.EnemyCard;
                case SelectType.AllyCard:
                    return targetType == TargetType.AllyCard;
                case SelectType.EnemyCard:
                    return targetType == TargetType.EnemyCard;
                case SelectType.None:
                default:
                    return false;
            }
        }
    }
}
