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

    public record SubSelectionInfo(IReadOnlyDictionary<string, ISubSelectionGroupInfo> SelectionInfos);
    public interface ISubSelectionGroupInfo { }
    public record NewCardSelectionInfo() : ISubSelectionGroupInfo;
    public record NewPartialCardSelectionInfo() : ISubSelectionGroupInfo;
    public record NewEffectSelectionInfo() : ISubSelectionGroupInfo;

    public record ExistCardSelectionInfo(
        IReadOnlyList<CardInfo> CardInfos,
        int Count,
        CardSelectionCountRule CountRule,
        string Id,
        int StepIndex,
        int StepCount,
        string CardDataId) : ISubSelectionGroupInfo
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

        public static Option<SubSelectionInfo> ToInfo(
            this IEnumerable<ISubSelectionGroup> subSelectionGroups,
            IGameplayModel model,
            ICardEntity cardEntity,
            IPlayerEntity caster)
        {
            if (caster == null)
                return Option.None<SubSelectionInfo>();

            var selectionInfos = new Dictionary<string, ISubSelectionGroupInfo>();
            foreach (var group in subSelectionGroups)
            {
                switch (group)
                {
                    case ExistCardSelectionGroup existCardGroup:
                        var cardLookTriggerContext = new TriggerContext(
                            model,
                            new CardTrigger(cardEntity),
                            new CardLookIntentAction(cardEntity, caster.Some()));
                        if (!existCardGroup.SelectCount
                                .Eval(cardLookTriggerContext)
                                .TryGetValue(out var selectCount) ||
                            selectCount < 0)
                        {
                            return Option.None<SubSelectionInfo>();
                        }

                        var candidates = existCardGroup.CardCandidates
                            .Eval(cardLookTriggerContext);
                        var cardInfos = candidates
                            .GroupBy(card => card.Identity)
                            .Select(grouping => grouping.First().ToInfo(model))
                            .ToList();

                        if (existCardGroup.CountRule == CardSelectionCountRule.RequiredExact && cardInfos.Count < selectCount)
                            return Option.None<SubSelectionInfo>();

                        selectionInfos[group.Id] =
                            new ExistCardSelectionInfo(
                                cardInfos,
                                selectCount,
                                existCardGroup.CountRule, group.Id, 0, subSelectionGroups.Count(), cardEntity.CardDataId);
                        break;
                    default:
                        // TODO（T-011／T-012）：NewCard、NewPartialCard、NewEffect 尚未實作資訊與結果流程，
                        // 暫時回傳 None；功能完成後補齊轉換，這不是永久禁止這些選取類型的規則。
                        return Option.None<SubSelectionInfo>();
                }
            }

            return new SubSelectionInfo(selectionInfos).Some();
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
