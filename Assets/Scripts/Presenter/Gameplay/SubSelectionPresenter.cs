using MortalGame.Presentation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
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
        public record SelectCardEvent(CardInfo CardInfo, ICardView CardView) : IUniTaskPresenter.Event;
        public record LongTouchCardEvent(CardInfo CardInfo, ICardView CardView) : IUniTaskPresenter.Event;

        UniTask<Option<IReadOnlyList<Guid>>> Run(
            ExistCardSelectionInfo info, CancellationToken cancellationToken);
    }

    public class SubSelectionPresenter : ISubSelectionPresenter
    {
        private readonly ICardSelectionPanel _cardSelectionPanel;
        private readonly ISingleCardDetailPopupPanel _singleCardDetailPopupPanel;
        private readonly LocalizeLibrary _localizeLibrary;

        public SubSelectionPresenter(IGameViewModel gameViewModel, LocalizeLibrary localizeLibrary,
            ISingleCardDetailPopupPanel singleCardDetailPopupPanel, ICardSelectionPanel cardSelectionPanel)
        {
            _localizeLibrary = localizeLibrary;
            _singleCardDetailPopupPanel = singleCardDetailPopupPanel;
            _singleCardDetailPopupPanel.Init(gameViewModel, localizeLibrary);
            _cardSelectionPanel = cardSelectionPanel;
            _cardSelectionPanel.Init(gameViewModel, localizeLibrary);
        }

        public async UniTask<Option<IReadOnlyList<Guid>>> Run(
            ExistCardSelectionInfo info, CancellationToken cancellationToken)
        {
            var selected = new List<Guid>();
            var result = Option.None<IReadOnlyList<Guid>>();
            var visible = true;
            IUniTaskPresenter presenter = new UniTaskPresenter();
            var promptKey = $"CardSelection.{info.CardDataId}.{info.Id}";
            var prompt = _localizeLibrary.TryGet(LocalizeInfoType.UI, promptKey, out var localized)
                ? localized.Info : _Text("CardSelection_Default");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _cardSelectionPanel.Open(new ICardSelectionPanel.Property(
                    info.IsValidCount(0),
                    info.CardInfos.Select(card => new ICardSelectionPanel.SelectionProperty(
                        card.Identity.ToString(), card,
                        (_, view) => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.SelectCardEvent(card, view)),
                        (_, view) => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.LongTouchCardEvent(card, view)))),
                    Description()));
                await presenter.Run(
                    _SetupSubscriptions(presenter), 
                    () => true, 
                    cancellationToken, 
                    EventHandler);
            }
            finally
            {
                _cardSelectionPanel.Close();
            }
            return result;

            async UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
            {
                switch (evt)
                {
                    case ISubSelectionPresenter.CloseEvent:
                        return new IUniTaskPresenter.Halt();
                    case ISubSelectionPresenter.ConfirmEvent:
                        if (info.IsValidCount(selected.Count))
                        {
                            result = Option.Some<IReadOnlyList<Guid>>(selected.ToArray());
                            return new IUniTaskPresenter.Halt();
                        }
                        break;
                    case ISubSelectionPresenter.SelectCardEvent select:
                        if (!selected.Remove(select.CardInfo.Identity) && selected.Count < info.MaxCount)
                            selected.Add(select.CardInfo.Identity);
                        Update();
                        break;
                    case ISubSelectionPresenter.VisibleToggleEvent:
                        visible = !visible;
                        Update();
                        break;
                    case ISubSelectionPresenter.LongTouchCardEvent detail:
                        await _singleCardDetailPopupPanel.Run(CardDetailProperty.Create(detail.CardInfo), cancellationToken);
                        break;
                }
                return new IUniTaskPresenter.None();
            }

            string Description() => string.Join("\n",
                prompt,
                string.Format(_Text("CardSelection_Step"), info.StepIndex + 1, info.StepCount),
                string.Format(_Text(info.CountRule == CardSelectionCountRule.Optional
                    ? "CardSelection_Optional" : "CardSelection_Required"), selected.Count, info.MaxCount));

            void Update() => _cardSelectionPanel.RenderUpdate(
                new ICardSelectionPanel.UpdateProperty(
                    visible, 
                    info.IsValidCount(selected.Count),
                    info.CardInfos.Where(card => selected.Contains(card.Identity)), 
                    Description()));
        }

        private IDisposable _SetupSubscriptions(IUniTaskPresenter presenter)
        {
            var disposables = new CompositeDisposable();
            _cardSelectionPanel.CloseButtons
                .Select(button => button.OnClickAsObservable())
                .Merge()
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.CloseEvent()))
                .AddTo(disposables);
            _cardSelectionPanel.ConfirmButton.OnClickAsObservable()
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.ConfirmEvent()))
                .AddTo(disposables);
            _cardSelectionPanel.VisibleToggleButton.OnClickAsObservable()
                .Subscribe(_ => presenter.TryEnqueueNextEvent(new ISubSelectionPresenter.VisibleToggleEvent()))
                .AddTo(disposables);
            return disposables;
        }

        private string _Text(string key) => _localizeLibrary.Get(LocalizeInfoType.UI, key).Info;
    }
}
