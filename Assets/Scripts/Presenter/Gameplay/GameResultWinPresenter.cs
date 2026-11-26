using System;
using System.Threading;
using MortalGame.Presentation.Abstractions;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameView;
using UniRx;

namespace MortalGame.Presenter
{

    public interface IGameResultWinPresenter
    {
        public record ContinueEvent : IUniTaskPresenter.Event;
        UniTask<GameplayWinResult> Run(CancellationToken cancellationToken);
    }

    public class GameResultWinPresenter : IGameResultWinPresenter
    {
        private readonly IGameResultWinPanel _winPanel;

        public GameResultWinPresenter(IGameResultWinPanel winPanel)
        {
            _winPanel = winPanel;
        }

        public async UniTask<GameplayWinResult> Run(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IUniTaskPresenter presenter = new UniTaskPresenter();
            using var session = _winPanel.Open();
            await presenter.Run(_SetupSubscriptions(session, presenter), () => true, cancellationToken,
                evt => UniTask.FromResult<IUniTaskPresenter.Event>(evt is IGameResultWinPresenter.ContinueEvent
                    ? new IUniTaskPresenter.Halt() : new IUniTaskPresenter.None()));
            return new GameplayWinResult();
        }

        private IDisposable _SetupSubscriptions(IGameResultWinSession session, IUniTaskPresenter presenter)
            => session.ContinueRequested.Subscribe(_ =>
                presenter.TryEnqueueNextEvent(new IGameResultWinPresenter.ContinueEvent()));
    }
}
