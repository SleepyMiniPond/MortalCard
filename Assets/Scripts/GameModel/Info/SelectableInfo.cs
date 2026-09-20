using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MortalGame.GameData;
using System.Linq;
using Optional;
using UnityEngine;

namespace MortalGame.GameModel
{

    public record MainSelectionInfo(
        SelectType SelectType,
        TargetCandidateScope CandidateScope,
        AutomaticTargetSelectionStrategy AutomaticSelection);

    public record SubSelectionInfo(
        IReadOnlyDictionary<string, ISubSelectionGroupInfo> SelectionInfos);

    public interface ISubSelectionGroupInfo { }
    public record ExistCardSelectionInfo(
        IReadOnlyList<CardInfo> CardInfos,
        int Count,
        bool IsMustSelect) : ISubSelectionGroupInfo
    {
        public int EffectiveCount => Math.Min(Count, CardInfos.Count);
    }
    public record NewCardSelectionInfo() : ISubSelectionGroupInfo;
    public record NewPartialCardSelectionInfo() : ISubSelectionGroupInfo;
    public record NewEffectSelectionInfo() : ISubSelectionGroupInfo;

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
            ICardEntity cardEntity)
        {
            var selectionInfos = new Dictionary<string, ISubSelectionGroupInfo>();
            foreach (var group in subSelectionGroups)
            {
                switch (group)
                {
                    case ExistCardSelectionGroup existCardGroup:
                        var cardLookTriggerContext = new TriggerContext(
                            model,
                            new CardTrigger(cardEntity),
                            new CardLookIntentAction(cardEntity));
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

                        selectionInfos[group.Id] =
                            new ExistCardSelectionInfo(
                                cardInfos,
                                selectCount,
                                existCardGroup.IsMustSelect.Eval(cardLookTriggerContext));
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
            UseCardAction useCardAction)
        {
            if (!TryCreateMainSelectionContext(
                        model,
                        cardEntity,
                        useCardAction.MainSelectionAction)
                    .TryGetValue(out var mainSelectionContext))
            {
                return Option.None<GameContext>();
            }

            Option<SubSelectionInfo> subSelectionInfoOption;
            using (model.ContextManager.SetContext(mainSelectionContext))
            {
                subSelectionInfoOption = cardEntity.SubSelects.ToInfo(model, cardEntity);
            }

            if (!subSelectionInfoOption.TryGetValue(out var subSelectionInfo) ||
                subSelectionInfo.SelectionInfos.Count !=
                useCardAction.SubSelectionActions.Count)
            {
                return Option.None<GameContext>();
            }

            var selectedCardGroups =
                ImmutableDictionary.CreateBuilder<string, ImmutableArray<Guid>>();
            foreach (var pair in subSelectionInfo.SelectionInfos)
            {
                // TODO（T-011／T-012）：目前只接線 ExistCard 的結果 Context；其他選取類型完成後，
                // 補上各自的結果驗證與保存方式，目前型別限制屬於暫時的實作邊界。
                if (pair.Value is not ExistCardSelectionInfo selectionInfo ||
                    !useCardAction.SubSelectionActions.TryGetValue(
                        pair.Key,
                        out var selectionAction) ||
                    selectionAction is not ExistCardSubSelectionAction existCardAction)
                {
                    return Option.None<GameContext>();
                }

                var candidateIdentities = selectionInfo.CardInfos
                    .Select(cardInfo => cardInfo.Identity)
                    .ToHashSet();
                var selectedIdentities = existCardAction.CardIdentity
                    .Where(candidateIdentities.Contains)
                    .Distinct()
                    .Take(selectionInfo.EffectiveCount)
                    .ToImmutableArray();
                selectedCardGroups.Add(pair.Key, selectedIdentities);
            }

            return (mainSelectionContext with
            {
                SelectedCardGroups = selectedCardGroups.ToImmutable()
            }).Some();
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
