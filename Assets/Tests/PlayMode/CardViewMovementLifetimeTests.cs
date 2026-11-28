using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using MortalGame.GameView;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MortalGame.Tests
{
    public sealed class CardViewMovementLifetimeTests
    {
        [UnityTest]
        public IEnumerator DestroyComponent_StopsOwnedMovement() => _DestroyDuringMovement(false);

        [UnityTest]
        public IEnumerator DestroyGameObject_StopsOwnedMovement() => _DestroyDuringMovement(true);

        private static IEnumerator _DestroyDuringMovement(bool destroyGameObject)
        {
            var root = new GameObject("卡片位移生命週期測試", typeof(RectTransform));
            var card = root.AddComponent<CardView>();
            card.AddLocationOffset(Guid.NewGuid(), Vector3.right * 100, 0.5f);
            var tween = DOTween.TweensByTarget(root.transform).Single();
            try
            {
                UnityEngine.Object.DestroyImmediate(destroyGameObject ? (UnityEngine.Object)root : card);
                Assert.That(tween.IsActive(), Is.False, "CardView 銷毀時必須同步結束其位移 Tween");
                yield return null;
            }
            finally
            {
                tween.Kill();
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
