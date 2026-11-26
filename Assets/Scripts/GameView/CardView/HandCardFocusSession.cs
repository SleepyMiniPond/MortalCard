using System;
using System.Collections.Generic;
using MortalGame.GameModel;
using MortalGame.Presentation.Abstractions;
using UniRx;
using UnityEngine;

namespace MortalGame.GameView
{
    // 一次手牌懸停的擁有者；詳情綁定與視覺位移一起建立、一起還原。
    internal sealed class HandCardFocusSession : IDisposable
    {
        private readonly Guid _identity;
        private readonly ICardView _focusedCard;
        private readonly IReadOnlyList<ICardView> _handCards;
        private readonly float _duration;
        private ReadOnlyReactiveProperty<FocusCardDetailDisplayState> _state;
        private IDisposable _detailSession;
        private bool _isDisposed;

        public HandCardFocusSession(Guid identity, ICardView focusedCard,
            IReadOnlyReactiveProperty<CardInfo> source, FocusCardDetailView detailView,
            IReadOnlyList<ICardView> handCards, IReadOnlyDictionary<Guid, int> indexes,
            float otherOffsetX, float duration)
        {
            _identity = identity;
            _focusedCard = focusedCard;
            _handCards = handCards;
            _duration = duration;
            try
            {
                var focusedIndex = indexes[identity];
                foreach (var card in handCards)
                {
                    if (!indexes.TryGetValue(card.TargetIdentity, out var index) || index == focusedIndex) continue;
                    card.AddLocationOffset(identity,
                        new Vector3(index < focusedIndex ? -otherOffsetX : otherOffsetX, 0, 0), duration);
                }
                focusedCard.ShowHandCardFocusContent();
                _state = source.Select(card => new FocusCardDetailDisplayState(card)).ToReadOnlyReactiveProperty();
                _detailSession = detailView.Open(_state, focusedCard.RectTransform);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            try { _detailSession?.Dispose(); }
            finally
            {
                // 先解除詳情綁定，再解除卡片來源，最後還原手牌顯示。
                try { _state?.Dispose(); }
                finally
                {
                    if (_IsAlive(_focusedCard)) _focusedCard.HideHandCardFocusContent();
                    foreach (var card in _handCards)
                        if (_IsAlive(card))
                            card.RemoveLocationOffset(_identity, card == _focusedCard ? 0f : _duration);
                }
            }
        }

        // 場景銷毀的先後順序不保證手牌元件仍存在。
        private static bool _IsAlive(ICardView card)
            => card is not UnityEngine.Object obj || obj != null;
    }
}
