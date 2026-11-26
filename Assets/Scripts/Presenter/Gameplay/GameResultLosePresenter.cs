using System;
using System.Threading;
using MortalGame.Presentation.Abstractions;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameView;
using UniRx;
using Optional;

namespace MortalGame.Presenter
{

    public interface IGameResultLosePresenter
    {
        public record ReactionEvent(LoseReactionType Reaction) : IUniTaskPresenter.Event;
        UniTask<GameplayLoseResult> Run(CancellationToken cancellationToken);
    }

    public class GameResultLosePresenter : IGameResultLosePresenter
    {
        private readonly IGameResultLosePanel _losePanel;

        public GameResultLosePresenter(IGameResultLosePanel losePanel)
        {
            _losePanel = losePanel;
        }

        public async UniTask<GameplayLoseResult> Run(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Option.None<GameplayLoseResult>();
            IUniTaskPresenter presenter = new UniTaskPresenter();
            using var session = _losePanel.Open();
            await presenter.Run(_SetupSubscriptions(session, presenter), () => true, cancellationToken, EventHandler);
            if (!result.TryGetValue(out var completed))
                throw new InvalidOperationException("失敗面板結束時缺少玩家操作結果。");
            return completed;

            UniTask<IUniTaskPresenter.Event> EventHandler(IUniTaskPresenter.Event evt)
            {
                if (evt is IGameResultLosePresenter.ReactionEvent reaction)
                {
                    result = Option.Some(new GameplayLoseResult(reaction.Reaction));
                    return UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.Halt());
                }
                return UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.None());
            }
        }

        private IDisposable _SetupSubscriptions(IGameResultLoseSession session, IUniTaskPresenter presenter)
            => session.ReactionRequested.Subscribe(reaction =>
                presenter.TryEnqueueNextEvent(new IGameResultLosePresenter.ReactionEvent(reaction)));
    }
}
