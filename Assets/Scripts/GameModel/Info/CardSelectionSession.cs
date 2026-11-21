using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MortalGame.GameModel
{
    /// <summary>一張來源卡從選取到提交的候選資料；離開作用域或完成提交時統一清理。</summary>
    public sealed class CardSelectionSession : IDisposable
    {
        private sealed record Offer(CardSelectionDraft Draft, SubSelectionStepInfo Pool, SubSelectionStepInfo Shown);
        private readonly Dictionary<Guid, Offer> _offers = new();
        private readonly CardSelectionSessionRegistry _owner;
        private CancellationTokenRegistration _cancellation;
        private bool _disposed;
        public Guid CardIdentity { get; }

        internal CardSelectionSession(CardSelectionSessionRegistry owner, Guid cardIdentity)
        {
            _owner = owner;
            CardIdentity = cardIdentity;
        }

        public static CardSelectionSession Begin(IGameplayModel model, Guid cardIdentity,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var session = model.ContextManager.SelectionSessionRegistry.Open(cardIdentity);
            session._cancellation = cancellationToken.Register(session.Dispose);
            return session;
        }

        internal SubSelectionStepInfo Prepare(CardSelectionDraft draft, SubSelectionStepInfo pool, IGameRandom random)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CardSelectionSession));
            var existing = _offers.Values.FirstOrDefault(offer =>
                offer.Draft.SessionIdentity == draft.SessionIdentity && offer.Pool.Id == pool.Id);
            if (existing != null) return existing.Shown;
            var identity = Guid.NewGuid();
            SubSelectionStepInfo shown = pool switch
            {
                CardTemplateSelectionInfo templates => templates with
                {
                    TemplateCardDataIds = templates.TemplateCardDataIds.Shuffle(random)
                        .Take(templates.OfferCount).ToImmutableArray(), OfferIdentity = identity
                },
                CardEffectSelectionInfo effects => effects with
                {
                    Options = effects.Options.Shuffle(random).Take(effects.OfferCount).ToImmutableArray(),
                    OfferIdentity = identity
                },
                _ => pool
            };
            _offers.Add(identity, new Offer(draft, pool, shown));
            return shown;
        }

        internal bool Accepts(CardSelectionDraft draft, SubSelectionStepInfo pool, ISubSelectionAction selection, Guid identity)
        {
            if (_disposed || !_offers.TryGetValue(identity, out var offer) ||
                offer.Draft.CardIdentity != draft.CardIdentity || offer.Draft.CasterIdentity != draft.CasterIdentity ||
                offer.Draft.MainSelectionAction != draft.MainSelectionAction ||
                offer.Pool.CardDataId != pool.CardDataId || offer.Pool.Id != pool.Id ||
                offer.Pool.StepIndex != pool.StepIndex || offer.Pool.StepCount != pool.StepCount ||
                offer.Draft.SelectedGroups.Count != pool.StepIndex ||
                !offer.Draft.SelectedGroups.All(pair => draft.SelectedGroups.TryGetValue(pair.Key, out var current) &&
                    _SameSelection(pair.Value, current))) return false;
            return (offer.Pool, pool, offer.Shown, selection) switch
            {
                (CardTemplateSelectionInfo old, CardTemplateSelectionInfo current,
                    CardTemplateSelectionInfo shown, CardTemplateSubSelectionAction selected)
                    => old.OfferCount == current.OfferCount && old.TemplateCardDataIds.SequenceEqual(current.TemplateCardDataIds) &&
                       shown.TemplateCardDataIds.Contains(selected.TemplateCardDataId),
                (CardEffectSelectionInfo old, CardEffectSelectionInfo current,
                    CardEffectSelectionInfo shown, CardEffectSubSelectionAction selected)
                    => old.OfferCount == current.OfferCount && old.TemplateSelectionId == current.TemplateSelectionId &&
                       old.TemplateCardDataId == current.TemplateCardDataId &&
                       old.Options.Select(option => (option.Id, option.DescriptionKey))
                           .SequenceEqual(current.Options.Select(option => (option.Id, option.DescriptionKey))) &&
                       shown.Options.Any(option => option.Id == selected.OptionId),
                _ => false
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cancellation.Dispose();
            _offers.Clear();
            _owner.Close(this);
        }

        private static bool _SameSelection(ISubSelectionAction left, ISubSelectionAction right)
            => (left, right) switch
            {
                (ExistCardSubSelectionAction a, ExistCardSubSelectionAction b) => a.CardIdentity.SequenceEqual(b.CardIdentity),
                _ => Equals(left, right)
            };
    }

    /// <summary>查找仍有效的 Session，候選紀錄與清理責任由 Session 持有。</summary>
    public sealed class CardSelectionSessionRegistry
    {
        private readonly Dictionary<Guid, CardSelectionSession> _sessions = new();

        internal CardSelectionSession Open(Guid cardIdentity)
        {
            if (_sessions.TryGetValue(cardIdentity, out var previous)) previous.Dispose();
            var session = new CardSelectionSession(this, cardIdentity);
            _sessions.Add(cardIdentity, session);
            return session;
        }

        internal void Close(CardSelectionSession session)
        {
            if (_sessions.TryGetValue(session.CardIdentity, out var current) && ReferenceEquals(current, session))
                _sessions.Remove(session.CardIdentity);
        }

        internal SubSelectionStepInfo Prepare(CardSelectionDraft draft, SubSelectionStepInfo pool, IGameRandom random)
        {
            if (pool is not (CardTemplateSelectionInfo or CardEffectSelectionInfo)) return pool;
            if (!_sessions.TryGetValue(draft.CardIdentity, out var session))
                throw new InvalidOperationException("準備合成候選前必須建立 CardSelectionSession。");
            return session.Prepare(draft, pool, random);
        }

        internal bool Accepts(CardSelectionDraft draft, SubSelectionStepInfo pool, ISubSelectionAction selection, Guid identity)
            => _sessions.TryGetValue(draft.CardIdentity, out var session) && session.Accepts(draft, pool, selection, identity);

        internal CardSelectionQueryResult Complete(IGameplayModel model, ICardEntity card, IPlayerEntity caster,
            MainSelectionAction main, IReadOnlyDictionary<string, ISubSelectionAction> actions)
        {
            // 先驗證並建立獨立 Context，再結束 Session；後續 Queue 不依賴候選暫存。
            _sessions.TryGetValue(card?.Identity ?? Guid.Empty, out var session);
            using (session)
                return CardSelectionQuery.ValidateComplete(model, card, caster, main, actions);
        }
    }
}
