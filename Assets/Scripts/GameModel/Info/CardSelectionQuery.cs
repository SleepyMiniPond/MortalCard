using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{
    public record CardSelectionDraft(
        Guid CardIdentity,
        Guid CasterIdentity,
        MainSelectionAction MainSelectionAction,
        IImmutableDictionary<string, ISubSelectionAction> SelectedGroups)
    {
        public static CardSelectionDraft Create(
            ICardEntity card, IPlayerEntity caster, MainSelectionAction mainSelection)
            => new(card.Identity, caster.Identity, mainSelection,
                ImmutableDictionary<string, ISubSelectionAction>.Empty);

        public CardSelectionDraft WithSelection(string groupId, ISubSelectionAction selection)
        {
            // 確認結果保存為快照，外部集合之後的變動不影響本次選取。
            var snapshot = selection switch
            {
                ExistCardSubSelectionAction { CardIdentity: not null } cards
                    when cards.CardIdentity is not ImmutableArray<Guid>
                    => cards with { CardIdentity = cards.CardIdentity.ToImmutableArray() },
                _ => selection
            };
            return this with { SelectedGroups = SelectedGroups.Add(groupId, snapshot) };
        }
    }

    public enum CardSelectionFailureReason
    {
        InvalidSource,
        InvalidMainTarget,
        UnsupportedGroup,
        InvalidCount,
        InsufficientCandidates,
        MissingGroup,
        UnexpectedGroup,
        OutOfOrder,
        InvalidSelection
    }

    public record CardSelectionFailure(CardSelectionFailureReason Reason, Option<string> GroupId);

    public abstract record CardSelectionQueryResult
    {
        public sealed record NextStep(SubSelectionStepInfo Info) : CardSelectionQueryResult;
        public sealed record Completed(GameContext Context) : CardSelectionQueryResult;
        public sealed record Rejected(CardSelectionFailure Failure) : CardSelectionQueryResult;
    }

    /// <summary>
    /// 依配置順序查詢與驗證選取。所有查詢皆在同步作用域內完成，不保存共用 Context。
    /// </summary>
    public static class CardSelectionQuery
    {
        public static CardSelectionQueryResult QueryNext(IGameplayModel model, CardSelectionDraft draft)
            => _Evaluate(model, draft, false);

        public static CardSelectionQueryResult ValidateComplete(
            IGameplayModel model,
            ICardEntity card,
            IPlayerEntity caster,
            MainSelectionAction mainSelection,
            IReadOnlyDictionary<string, ISubSelectionAction> actions)
        {
            if (card == null || caster == null || mainSelection == null || actions == null)
                return _Reject(CardSelectionFailureReason.InvalidSource);

            var draft = CardSelectionDraft.Create(card, caster, mainSelection);
            foreach (var pair in actions)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    return _Reject(CardSelectionFailureReason.InvalidSelection, pair.Key);
                draft = draft.WithSelection(pair.Key, pair.Value);
            }
            return _Evaluate(model, draft, true);
        }

        private static CardSelectionQueryResult _Evaluate(
            IGameplayModel model, CardSelectionDraft draft, bool requireComplete)
        {
            if (draft == null || draft.SelectedGroups == null || draft.MainSelectionAction == null ||
                !model.GetCard(draft.CardIdentity).TryGetValue(out var card) ||
                !model.GameStatus.GetPlayer(draft.CasterIdentity).TryGetValue(out var caster))
                return _Reject(CardSelectionFailureReason.InvalidSource);
            if (!SelectionInfoUtility.TryCreateMainSelectionContext(model, card, draft.MainSelectionAction)
                    .TryGetValue(out var context))
                return _Reject(CardSelectionFailureReason.InvalidMainTarget);

            var groups = card.SubSelects.ToArray();
            var groupIds = groups.Select(group => group.Id).ToHashSet();
            foreach (var id in draft.SelectedGroups.Keys)
                if (!groupIds.Contains(id))
                    return _Reject(CardSelectionFailureReason.UnexpectedGroup, id);

            for (var index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                var hasResult = draft.SelectedGroups.TryGetValue(group.Id, out var selected);
                if (!hasResult && groups.Skip(index + 1).Any(next => draft.SelectedGroups.ContainsKey(next.Id)))
                    return _Reject(CardSelectionFailureReason.OutOfOrder, group.Id);

                CardSelectionQueryResult step;
                using (model.ContextManager.SetContext(context))
                    step = _QueryStep(model, card, caster, group, index, groups.Length);
                if (step is not CardSelectionQueryResult.NextStep nextStep)
                    return step;
                if (!hasResult)
                    return requireComplete ? _Reject(CardSelectionFailureReason.MissingGroup, group.Id) : step;

                if (!_ApplySelection(nextStep.Info, selected, context).TryGetValue(out var nextContext))
                    return _Reject(CardSelectionFailureReason.InvalidSelection, group.Id);
                context = nextContext;
            }

            return new CardSelectionQueryResult.Completed(context);
        }

        private static Option<GameContext> _ApplySelection(
            SubSelectionStepInfo info, ISubSelectionAction selection, GameContext context)
            => (info, selection) switch
            {
                (ExistCardSelectionInfo cards, ExistCardSubSelectionAction selected)
                    => _ApplyExistCardSelection(cards, selected, context),
                _ => Option.None<GameContext>()
            };

        private static Option<GameContext> _ApplyExistCardSelection(
            ExistCardSelectionInfo info, ExistCardSubSelectionAction selection, GameContext context)
        {
            var selected = selection.CardIdentity;
            if (selected == null || selected is ImmutableArray<Guid> { IsDefault: true })
                return Option.None<GameContext>();

            var candidates = info.CardInfos.Select(candidate => candidate.Identity).ToHashSet();
            if (!info.IsValidCount(selected.Count) || selected.Distinct().Count() != selected.Count ||
                selected.Any(id => !candidates.Contains(id)))
                return Option.None<GameContext>();

            return (context with
            {
                SelectedCardGroups = context.SelectedCardGroups.Add(info.Id, selected.ToImmutableArray())
            }).Some();
        }

        private static CardSelectionQueryResult _QueryStep(
            IGameplayModel model, ICardEntity card, IPlayerEntity caster,
            ISubSelectionGroup group, int index, int total)
        {
            if (group is not ExistCardSelectionGroup selection)
                return _Reject(CardSelectionFailureReason.UnsupportedGroup, group.Id);
            var trigger = new TriggerContext(model, new CardTrigger(card), new CardLookIntentAction(card, caster.Some()));
            if (!selection.SelectCount.Eval(trigger).TryGetValue(out var count) || count < 0)
                return _Reject(CardSelectionFailureReason.InvalidCount, group.Id);

            var candidates = selection.CardCandidates.Eval(trigger)
                .GroupBy(candidate => candidate.Identity)
                .Select(items => items.First().ToInfo(model)).ToImmutableArray();
            if (selection.CountRule == CardSelectionCountRule.RequiredExact && candidates.Length < count)
                return _Reject(CardSelectionFailureReason.InsufficientCandidates, group.Id);

            return new CardSelectionQueryResult.NextStep(new ExistCardSelectionInfo(
                candidates, count, selection.CountRule, group.Id, index, total, card.CardDataId));
        }

        private static CardSelectionQueryResult.Rejected _Reject(
            CardSelectionFailureReason reason, string groupId = null)
            => new(new CardSelectionFailure(reason, groupId.SomeNotNull()));
    }
}
