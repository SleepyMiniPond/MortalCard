using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using MortalGame.GameModel;
using MortalGame.GameView;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class CharacterAnimationWorkerTests
    {
        [UnityTest]
        public IEnumerator Dispose_CancelsAndCompletesWorker()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var worker = new CharacterAnimationWorker(
                    (_, cancellationToken) => UniTask.WaitUntilCanceled(cancellationToken),
                    minTimeInterval: 0f);
                worker.Enqueue(new TestAnimationEvent());

                await UniTask.NextFrame();
                worker.Dispose();
                worker.Dispose();

                var isCanceled = await worker.Completion.SuppressCancellationThrow();

                Assert.IsTrue(isCanceled);
            });
        }

        [UnityTest]
        public IEnumerator EnqueueAfterDispose_DoesNotStartAnimation()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var playCount = 0;
                var worker = new CharacterAnimationWorker(
                    (_, _) =>
                    {
                        playCount++;
                        return UniTask.CompletedTask;
                    },
                    minTimeInterval: 0f);

                worker.Dispose();
                worker.Enqueue(new TestAnimationEvent());
                await worker.Completion.SuppressCancellationThrow();

                Assert.AreEqual(0, playCount);
            });
        }

        [UnityTest]
        public IEnumerator AnimationFailure_PropagatesThroughCompletion()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var expectedException = new InvalidOperationException("animation failed");
                var worker = new CharacterAnimationWorker(
                    (_, _) => UniTask.FromException(expectedException),
                    minTimeInterval: 0f);
                worker.Enqueue(new TestAnimationEvent());

                Exception actualException = null;
                try
                {
                    await worker.Completion;
                }
                catch (Exception exception)
                {
                    actualException = exception;
                }
                finally
                {
                    worker.Dispose();
                }

                Assert.AreSame(expectedException, actualException);
            });
        }

        private sealed class TestAnimationEvent : IAnimationNumberEvent
        {
        }
    }
}
