using MortalGame.Presentation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameData;
using MortalGame.GameView;
using Optional;
using UniRx;

namespace MortalGame.Presenter
{
    public interface ISubSelectionPresenter
    {
        public record CloseEvent : IUniTaskPresenter.Event;
        public record ConfirmEvent : IUniTaskPresenter.Event;
        public record VisibleToggleEvent : IUniTaskPresenter.Event;
        public record SelectCardEvent(Guid Identity) : IUniTaskPresenter.Event;
        public record LongTouchCardEvent(Guid Identity) : IUniTaskPresenter.Event;

        UniTask<Option<IReadOnlyList<Guid>>> Run(
            ExistCardSelectionInfo info, CancellationToken cancellationToken);
        UniTask<Option<ISubSelectionAction>> RunComposition(
            IGameplayModel model, SubSelectionStepInfo info, CancellationToken cancellationToken);
    }

    public class SubSelectionPresenter : ISubSelectionPresenter
    {
        private readonly ICardSelectionPanel _cardSelectionPanel;
        private readonly ISingleCardDetailPresenter _singleCardDetailPresenter;

        public SubSelectionPresenter(IGameViewModel gameViewModel, LocalizeLibrary localizeLibrary,
            ISingleCardDetailPopupPanel singleCardDetailPopupPanel, ICardSelectionPanel cardSelectionPanel)
        {
            _singleCardDetailPresenter = new SingleCardDetailPresenter(singleCardDetailPopupPanel, gameViewModel, localizeLibrary);
            _cardSelectionPanel = cardSelectionPanel;
            _cardSelectionPanel.Init(gameViewModel, localizeLibrary);
        }

        public async UniTask<Option<IReadOnlyList<Guid>>> Run(
            ExistCardSelectionInfo info, CancellationToken cancellationToken)
        {
            var result = Option.None<IReadOnlyList<Guid>>();
            using var state = new ReactiveProperty<CardSelectionDisplayState>(new(
                Array.AsReadOnly(info.CardInfos.ToArray()),
                true, info.IsValidCount(0), Array.Empty<Guid>(),
                new CardSelectionPromptInfo(info.CardDataId, info.Id, info.StepIndex, info.StepCount,
                    0, info.MaxCount, info.CountRule)));
            IUniTaskPresenter presenter = new UniTaskPresenter();
            cancellationToken.ThrowIfCancellationRequested();
            using var session = _cardSelectionPanel.Open(state);
            await presenter.Run(
                _SetupSubscriptions(session, presenter),
                () => true,
                cancellationToken,
                EventHandler);
            return result;

            async UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
            {
                switch (evt)
                {
                    case ISubSelectionPresenter.CloseEvent:
                        return new IUniTaskPresenter.Halt();
                    case ISubSelectionPresenter.ConfirmEvent:
                        if (info.IsValidCount(state.Value.SelectedCardIdentities.Count))
                        {
                            result = Option.Some<IReadOnlyList<Guid>>(state.Value.SelectedCardIdentities.ToArray());
                            return new IUniTaskPresenter.Halt();
                        }
                        break;
                    case ISubSelectionPresenter.SelectCardEvent select:
                        if (!state.Value.Candidates.Any(card => card.Identity == select.Identity)) break;
                        var selected = state.Value.SelectedCardIdentities.ToList();
                        if (!selected.Remove(select.Identity) && selected.Count < info.MaxCount)
                            selected.Add(select.Identity);
                        // 不修改已發布的集合；一次發布選取、合法性與提示數量的新快照。
                        state.Value = state.Value with
                        {
                            SelectedCardIdentities = Array.AsReadOnly(selected.ToArray()),
                            IsConfirmable = info.IsValidCount(selected.Count),
                            Prompt = state.Value.Prompt with { SelectedCount = selected.Count }
                        };
                        break;
                    case ISubSelectionPresenter.VisibleToggleEvent:
                        state.Value = state.Value with { IsVisible = !state.Value.IsVisible };
                        break;
                    case ISubSelectionPresenter.LongTouchCardEvent detail:
                        var cardInfo = state.Value.Candidates.FirstOrDefault(card => card.Identity == detail.Identity);
                        if (cardInfo != null)
                            await _singleCardDetailPresenter.Run(cardInfo, cancellationToken);
                        break;
                }
                return new IUniTaskPresenter.None();
            }

        }

        public async UniTask<Option<ISubSelectionAction>> RunComposition(
            IGameplayModel model, SubSelectionStepInfo info, CancellationToken cancellationToken)
        {
            // 使用獨立預覽 Identity，避免選項卡片訂閱到戰鬥中其他卡片。
            var manager = model.ContextManager;
            var actions = new Dictionary<Guid, ISubSelectionAction>();
            var previews = new List<CardInfo>();
            if (info is CardTemplateSelectionInfo templates)
                foreach (var id in templates.TemplateCardDataIds)
                {
                    var card = CardEntity.RuntimeCreateFromId(id, manager.CardLibrary, manager.CardPropertyEntityFactory);
                    previews.Add(_CompositionPreview(card, model));
                    actions.Add(card.Identity, new CardTemplateSubSelectionAction(id, info.OfferIdentity));
                }
            else if (info is CardEffectSelectionInfo effects)
                foreach (var option in effects.Options)
                {
                    var card = CardEntity.RuntimeCreateFromId(effects.TemplateCardDataId,
                        manager.CardLibrary, manager.CardPropertyEntityFactory);
                    var preview = _CompositionPreview(card, model) with
                    {
                        DescriptionFragmentsOnly = true,
                        DescriptionFragments = new[] { option.Description }
                    };
                    previews.Add(preview);
                    actions.Add(preview.Identity, new CardEffectSubSelectionAction(option.Id, info.OfferIdentity));
                }
            var selected = await Run(new ExistCardSelectionInfo(previews, 1, CardSelectionCountRule.RequiredExact,
                info.Id, info.StepIndex, info.StepCount, info.CardDataId), cancellationToken);
            return selected.Map(identities => actions[identities.Single()]);
        }

        private static CardInfo _CompositionPreview(ICardEntity card, IGameplayModel model)
            // 預覽卡尚無擁有者，直接呈現模板基本值，不套用戰鬥持有者的公式。
            => card.ToInfo(model) with { Cost = card.OriginCost.Some(), Power = card.OriginPower.Some() };

        private IDisposable _SetupSubscriptions(ICardSelectionSession session, IUniTaskPresenter presenter)
        {
            var disposables = new CompositeDisposable();
            session.CardClicked
                .Subscribe(identity => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.SelectCardEvent(identity)))
                .AddTo(disposables);
            session.CardLongPressed
                .Subscribe(identity => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.LongTouchCardEvent(identity)))
                .AddTo(disposables);
            session.CloseRequested
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.CloseEvent()))
                .AddTo(disposables);
            session.ConfirmRequested
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.ConfirmEvent()))
                .AddTo(disposables);
            session.VisibilityToggleRequested
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.VisibleToggleEvent()))
                .AddTo(disposables);
            return disposables;
        }

    }
}
