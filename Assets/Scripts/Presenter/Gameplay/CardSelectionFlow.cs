using System;
using MortalGame.Exceptions;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using Optional;

namespace MortalGame.Presenter
{
    public abstract record CardSelectionPresentationResult
    {
        public sealed record Completed(CardSelectionDraft Draft) : CardSelectionPresentationResult;
        public sealed record Cancelled : CardSelectionPresentationResult;
        public sealed record Rejected(CardSelectionFailure Failure) : CardSelectionPresentationResult;
    }

    public interface ICardSelectionFlow
    {
        // 呼叫端持有 Session，涵蓋面板選取與後續 Action 提交；本流程只負責呈現及收集選擇。
        UniTask<CardSelectionPresentationResult> RunSubSelection(
            IGameplayModel model, CardSelectionDraft draft, CancellationToken cancellationToken);
    }

    public class CardSelectionFlow : ICardSelectionFlow
    {
        private readonly ISubSelectionPresenter _subSelectionPresenter;

        public CardSelectionFlow(ISubSelectionPresenter subSelectionPresenter)
        {
            _subSelectionPresenter = subSelectionPresenter;
        }

        public async UniTask<CardSelectionPresentationResult> RunSubSelection(
            IGameplayModel model, CardSelectionDraft draft, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = CardSelectionQuery.PrepareNext(model, draft);
            if (query is CardSelectionQueryResult.NextStep first)
            {
                var stepCount = first.Info.StepCount;
                for (var stepIndex = first.Info.StepIndex; stepIndex < stepCount; stepIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (query is not CardSelectionQueryResult.NextStep next) break;
                    var selected = await _RunStep(model, next.Info, cancellationToken);
                    if (!selected.TryGetValue(out var selection))
                        return new CardSelectionPresentationResult.Cancelled();
                    draft = draft.WithSelection(next.Info.Id, selection);
                    query = CardSelectionQuery.PrepareNext(model, draft);
                }
            }

            return query switch
            {
                CardSelectionQueryResult.Completed => new CardSelectionPresentationResult.Completed(draft),
                CardSelectionQueryResult.Rejected rejected => new CardSelectionPresentationResult.Rejected(rejected.Failure),
                CardSelectionQueryResult.NextStep remaining => new CardSelectionPresentationResult.Rejected(
                    new CardSelectionFailure(CardSelectionFailureReason.MissingGroup, remaining.Info.Id.Some())),
                _ => throw new UnsupportedSelectionTypeException(query.GetType())
            };
        }

        private async UniTask<Option<ISubSelectionAction>> _RunStep(
            IGameplayModel model, SubSelectionStepInfo info, CancellationToken cancellationToken)
        {
            switch (info)
            {
                case ExistCardSelectionInfo cards:
                    if (cards.MaxCount == 0)
                        return Option.Some<ISubSelectionAction>(new ExistCardSubSelectionAction(Array.Empty<Guid>()));
                    var selected = await _subSelectionPresenter.Run(cards, cancellationToken);
                    return selected.Map<ISubSelectionAction>(identities => new ExistCardSubSelectionAction(identities));
                case CardTemplateSelectionInfo or CardEffectSelectionInfo:
                    return await _subSelectionPresenter.RunComposition(model, info, cancellationToken);
                default:
                    throw new UnsupportedSelectionTypeException(info.GetType());
            }
        }
    }
}
