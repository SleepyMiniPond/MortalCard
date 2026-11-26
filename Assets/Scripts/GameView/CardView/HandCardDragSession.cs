using System;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.Presentation.Abstractions;
using MortalGame.UI;
using Optional;
using Optional.Collections;
using UniRx;
using UnityEngine;

namespace MortalGame.GameView
{
    internal sealed class HandCardDragSession : IDisposable
    {
        private readonly ICardView _card;
        private readonly IReadOnlyReactiveProperty<CardInfo> _source;
        private readonly IGameplayActionReciever _receiver;
        private readonly CustomLineRenderer _line;
        private readonly Vector2 _offset;
        private readonly Vector3 _startWorldPosition;
        private IDisposable _restoreCard;
        private IDisposable _subscription;
        private ISelectableView _selectedTarget;
        private Vector2 _position;
        private bool _isDisposed;
        public Guid Identity { get; }

        public HandCardDragSession(Guid identity, ICardView card, int siblingIndex, Vector2 position,
            IReadOnlyReactiveProperty<CardInfo> source, IGameplayActionReciever receiver, CustomLineRenderer line)
        {
            Identity = identity;
            _card = card;
            _source = source;
            _receiver = receiver;
            _line = line;
            _position = position;
            _startWorldPosition = card.RectTransform.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                card.ParentRectTransform, position, card.Canvas.worldCamera, out var localPoint);
            _offset = card.RectTransform.anchoredPosition - localPoint;
            try
            {
                _restoreCard = card.BeginDrag(siblingIndex);
                _subscription = source.Subscribe(_Render);
            }
            catch { Dispose(); throw; }
        }

        public void Update(Vector2 position)
        {
            if (_isDisposed) return;
            _position = position;
            _Render(_source.Value);
        }

        // 放開使用當次座標與最新卡片資料；只回傳意圖，命令仍由外層交付。
        public Option<UseCardCommand> Release(Vector2 position)
        {
            if (_isDisposed) return Option.None<UseCardCommand>();
            try { return CreateDropCommand(_source.Value, position, _card, _receiver); }
            finally { Dispose(); }
        }

        private void _Render(CardInfo info)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _card.ParentRectTransform, _position, _card.Canvas.worldCamera, out var localPoint);
            var (status, target) = ResolveTarget(info, _position, _card, _receiver);
            _card.Drag(localPoint + _offset, status);
            if (info.MainSelectable.SelectType == SelectType.None || !target.TryGetValue(out var selected))
            {
                _ClearTarget();
                return;
            }
            if (_selectedTarget != selected)
            {
                if (_selectedTarget != null && _IsAlive(_selectedTarget)) _selectedTarget.OnDeselect();
                selected.OnSelect();
                _selectedTarget = selected;
            }
            _line.gameObject.SetActive(true);
            _line.SetLineProperty(_startWorldPosition, selected.RectTransform);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            try { _subscription?.Dispose(); }
            finally
            {
                try { _ClearTarget(); }
                finally
                {
                    if (_IsAlive(_card)) _restoreCard?.Dispose();
                }
            }
        }

        private void _ClearTarget()
        {
            if (_selectedTarget != null && _IsAlive(_selectedTarget)) _selectedTarget.OnDeselect();
            _selectedTarget = null;
            if (_line != null) _line.gameObject.SetActive(false);
        }

        private static bool _IsAlive(object view) => view is not UnityEngine.Object obj || obj != null;

        internal static Option<UseCardCommand> CreateDropCommand(CardInfo info, Vector2 position,
            ICardView card, IGameplayActionReciever receiver)
        {
            var (status, target) = ResolveTarget(info, position, card, receiver);
            return status switch
            {
                IDragableCardView.DradTargetStatus.ValidTarget => target.Map(view =>
                    new UseCardCommand(info.Identity, ((ISelectionTarget)view).Some())),
                IDragableCardView.DradTargetStatus.WithoutTarget => new UseCardCommand(info.Identity).Some(),
                _ => Option.None<UseCardCommand>()
            };
        }

        internal static (IDragableCardView.DradTargetStatus Status, Option<ISelectableView> Target) ResolveTarget(
            CardInfo info, Vector2 position, ICardView card, IGameplayActionReciever receiver)
        {
            if (info.MainSelectable.SelectType != SelectType.None)
            {
                var target = OptionCollectionExtensions.FirstOrNone(receiver.SelectableViews, view => view != card
                    && info.MainSelectable.SelectType.IsSelectable(view.TargetType)
                    && RectTransformUtility.RectangleContainsScreenPoint(view.RectTransform, position, card.Canvas.worldCamera));
                return (target.HasValue ? IDragableCardView.DradTargetStatus.ValidTarget : IDragableCardView.DradTargetStatus.None, target);
            }
            if (RectTransformUtility.RectangleContainsScreenPoint(receiver.BasicSelectableView.RectTransform, position, card.Canvas.worldCamera))
                return (IDragableCardView.DradTargetStatus.WithoutTarget, receiver.BasicSelectableView.Some());
            return (IDragableCardView.DradTargetStatus.None, Option.None<ISelectableView>());
        }
    }
}
