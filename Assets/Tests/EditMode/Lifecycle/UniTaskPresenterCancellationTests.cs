using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortalGame.Presenter;
using NUnit.Framework;
using UniRx;
using UnityEditor;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class UniTaskPresenterCancellationTests
    {
        private record CloseEvent : IUniTaskPresenter.Event;

        [UnityTest]
        public IEnumerator Cancellation_DiscardsLateCallbacksAndAllowsAnotherRun() => UniTask.ToCoroutine(async () =>
        {
            var presenter = new UniTaskPresenter();
            var handled = 0;
            using var cancellation = new CancellationTokenSource();
            var run = presenter.Run(Disposable.Empty, () => true, cancellation.Token, Handle);
            cancellation.Cancel();
            Assert.That(await run.SuppressCancellationThrow(), Is.True);
            presenter.TryEnqueueNextEvent(new CloseEvent());
            Assert.That(handled, Is.Zero);
            var nextRun = presenter.Run(Disposable.Empty, () => true, CancellationToken.None, Handle);
            presenter.TryEnqueueNextEvent(new CloseEvent());
            EditorApplication.QueuePlayerLoopUpdate();
            await nextRun;
            Assert.That(handled, Is.EqualTo(1));

            UniTask<IUniTaskPresenter.Event> Handle(IUniTaskPresenter.Event evt)
            {
                handled++;
                return UniTask.FromResult<IUniTaskPresenter.Event>(new IUniTaskPresenter.Halt());
            }
        });

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
