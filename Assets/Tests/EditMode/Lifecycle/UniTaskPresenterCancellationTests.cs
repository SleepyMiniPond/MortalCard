using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.Presenter;
using NUnit.Framework;
using UniRx;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class UniTaskPresenterCancellationTests
    {
        [UnityTest]
        public IEnumerator Run_WhenCanceled_CompletesAsCanceled()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using var cancellation = new CancellationTokenSource();
                var presenter = new UniTaskPresenter();
                var runTask = presenter.Run(
                    Disposable.Empty,
                    () => true,
                    cancellation.Token,
                    eventTaskHandler: _ => UniTask.FromResult<IUniTaskPresenter.Event>(
                        new IUniTaskPresenter.None()))
                    .Preserve();

                cancellation.Cancel();
                var isCanceled = await runTask.SuppressCancellationThrow();

                Assert.IsTrue(isCanceled);
            });
        }
    }
}
