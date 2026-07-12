using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public class T016TimingPipelineTests
    {
        private const string SessionKey = "timing-session";

        [Test]
        public void TriggerTiming_UpdatesReactionSessionBeforeEvaluatingBuffConditions()
        {
            var session = new RecordingReactionSessionEntity();
            var buffData = CreatePlayerBuffData(
                BuffTestBuilder.PlayerBuffId,
                GameTiming.BeforeTurnEnd,
                new SessionReceivedTimingCondition(SessionKey, GameTiming.BeforeTurnEnd));
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(buffData)
                .Build();
            built.Ally.BuffManager.AddBuff(CreatePlayerBuff(BuffTestBuilder.PlayerBuffId, session));

            var events = built.Manager.TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance).ToList();

            Assert.That(session.ReceivedTimings, Does.Contain(GameTiming.BeforeTurnEnd));
            Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(6));
        }

        [Test]
        public void AfterTriggerBuffEffect_UpdatesReactionSessionBeforeEvaluatingChainedBuffConditions()
        {
            const string firstBuffId = "first-buff";
            const string chainedBuffId = "chained-buff";
            var chainedSession = new RecordingReactionSessionEntity();
            var firstBuffData = CreatePlayerBuffData(
                firstBuffId,
                GameTiming.BeforeTurnEnd,
                new ConstCondition { Value = true });
            var chainedBuffData = CreatePlayerBuffData(
                chainedBuffId,
                GameTiming.AfterTriggerBuffEffect,
                new SessionReceivedTimingCondition(SessionKey, GameTiming.AfterTriggerBuffEffect),
                new PlayerBuffSourceCondition(firstBuffId));
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(firstBuffData)
                .WithPlayerBuff(chainedBuffData)
                .Build();
            built.Ally.BuffManager.AddBuff(CreatePlayerBuff(firstBuffId));
            built.Ally.BuffManager.AddBuff(CreatePlayerBuff(chainedBuffId, chainedSession));

            var events = built.Manager.TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance).ToList();

            Assert.That(chainedSession.ReceivedTimings, Does.Contain(GameTiming.AfterTriggerBuffEffect));
            Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(10));
        }

        [Test]
        public void WholeTurnSession_ResetsBeforeTurnStartAndClearsAfterTurnEnd()
        {
            var session = new ReactionSessionEntity(
                new SessionBooleanEntity(true, new List<SessionBoolean.TimingRule>()),
                SessionLifeTime.WholeTurn);
            var built = new GameplayManagerTestBuilder().Build();

            Assert.That(session.BooleanValue.ValueOr(false), Is.True);

            session.Update(CreateTimingContext(built.Manager, GameTiming.AfterTurnEnd));
            Assert.That(session.BooleanValue.HasValue, Is.False);

            session.Update(CreateTimingContext(built.Manager, GameTiming.BeforeTurnStart));
            Assert.That(session.BooleanValue.ValueOr(false), Is.True);
        }

        [Test]
        public void PlayCardSession_StartsBeforePlayCardStartAndClearsAfterPlayCardEnd()
        {
            var session = new ReactionSessionEntity(
                new SessionBooleanEntity(true, new List<SessionBoolean.TimingRule>()),
                SessionLifeTime.PlayCard);
            var built = new GameplayManagerTestBuilder().Build();

            Assert.That(session.BooleanValue.HasValue, Is.False);

            session.Update(CreateTimingContext(built.Manager, GameTiming.BeforePlayCardStart));
            Assert.That(session.BooleanValue.ValueOr(false), Is.True);

            session.Update(CreateTimingContext(built.Manager, GameTiming.AfterPlayCardEnd));
            Assert.That(session.BooleanValue.HasValue, Is.False);
        }

        private static PlayerBuffData CreatePlayerBuffData(
            string buffId,
            GameTiming timing,
            params IPlayerBuffCondition[] conditions)
        {
            var conditionalEffect = new ConditionalPlayerBuffEffect
            {
                Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                {
                    Type = EffectAttributeAdditionType.PowerAddition,
                    Value = new ConstInteger { Value = 1 }
                }
            };
            conditionalEffect.Conditions.AddRange(conditions);

            return BuffTestBuilder.CreatePlayerBuffData(
                buffId,
                timing,
                conditionalEffect);
        }

        private static PlayerBuffEntity CreatePlayerBuff(
            string buffId,
            IReactionSessionEntity session = null)
        {
            var sessions = session == null
                ? new Dictionary<string, IReactionSessionEntity>()
                : new Dictionary<string, IReactionSessionEntity> { { SessionKey, session } };
            return BuffTestBuilder.CreatePlayerBuff(
                buffId: buffId,
                reactionSessions: sessions);
        }

        private static TriggerContext CreateTimingContext(
            GameplayManager manager,
            GameTiming timing)
        {
            var player = ((IGameplayModel)manager).GameStatus.Ally;
            return new TriggerContext(
                manager,
                new PlayerTrigger(player),
                new UpdateTimingAction(timing, SystemSource.Instance));
        }
    }

    internal sealed class SessionReceivedTimingCondition : IPlayerBuffCondition
    {
        private readonly string _sessionKey;
        private readonly GameTiming _expectedTiming;

        public SessionReceivedTimingCondition(string sessionKey, GameTiming expectedTiming)
        {
            _sessionKey = sessionKey;
            _expectedTiming = expectedTiming;
        }

        public bool Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered is PlayerBuffTrigger trigger &&
                trigger.Buff.ReactionSessions.TryGetValue(_sessionKey, out var session) &&
                session is RecordingReactionSessionEntity recordingSession &&
                recordingSession.ReceivedTimings.Contains(_expectedTiming);
        }
    }

    internal sealed class PlayerBuffSourceCondition : IPlayerBuffCondition
    {
        private readonly string _expectedBuffId;

        public PlayerBuffSourceCondition(string expectedBuffId)
        {
            _expectedBuffId = expectedBuffId;
        }

        public bool Eval(TriggerContext triggerContext)
        {
            return triggerContext.Action.Source is PlayerBuffSource source &&
                source.Buff.PlayerBuffDataId == _expectedBuffId;
        }
    }

    internal sealed class RecordingReactionSessionEntity : IReactionSessionEntity
    {
        public List<GameTiming> ReceivedTimings { get; } = new();
        public bool IsSessionValueUpdated => ReceivedTimings.Count > 0;
        public Option<bool> BooleanValue => Option.None<bool>();
        public Option<int> IntegerValue => Option.None<int>();

        public bool Update(TriggerContext triggerContext)
        {
            ReceivedTimings.Add(triggerContext.Action.Timing);
            return true;
        }

        public IReactionSessionEntity Clone()
        {
            return new RecordingReactionSessionEntity();
        }
    }
}
