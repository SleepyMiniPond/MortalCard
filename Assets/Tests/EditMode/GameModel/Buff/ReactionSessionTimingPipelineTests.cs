using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public class ReactionSessionTimingPipelineTests
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
        public void ObserveAction_ResultAction_OnlyUpdatesSessionWithoutDispatchingBuffEffect()
        {
            var session = new RecordingReactionSessionEntity();
            var conditionalEffect = new ConditionalPlayerBuffEffect
            {
                Conditions = { new ConstCondition { Value = true } },
                Effect = new EffectiveDamagePlayerBuffEffect
                {
                    Targets = new SingleCharacterCollection
                    {
                        Target = new MainCharacterOfPlayer
                        {
                            Player = new CurrentPlayer()
                        }
                    },
                    Value = new ConstInteger { Value = 10 }
                }
            };
            var buffData = BuffTestBuilder.CreatePlayerBuffData(
                BuffTestBuilder.PlayerBuffId,
                GameTiming.EffectTargetResult,
                conditionalEffect);
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(buffData)
                .Build();
            built.Ally.BuffManager.AddBuff(
                CreatePlayerBuff(BuffTestBuilder.PlayerBuffId, session));

            built.Manager
                .ObserveAction(new ObservedResultAction())
                .ToList();

            Assert.That(session.ReceivedTimings, Is.EqualTo(new[]
            {
                GameTiming.EffectTargetResult
            }));
            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void TriggerTiming_UpdatesEachReactionSessionExactlyOnce()
        {
            var session = new RecordingReactionSessionEntity();
            var buffData = CreatePlayerBuffData(
                BuffTestBuilder.PlayerBuffId,
                GameTiming.AfterTurnEnd,
                new ConstCondition { Value = true });
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(buffData)
                .Build();
            built.Ally.BuffManager.AddBuff(
                CreatePlayerBuff(BuffTestBuilder.PlayerBuffId, session));

            built.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(session.ReceivedTimings, Is.EqualTo(new[]
            {
                GameTiming.BeforeTurnEnd
            }));
        }

        [Test]
        public void TriggerTiming_BuffAddedDuringSessionObservation_DoesNotJoinCurrentTimingSnapshot()
        {
            const string observerBuffId = "observer-buff";
            const string addedBuffId = "added-buff";
            var observerBuffData = CreatePlayerBuffData(
                observerBuffId,
                GameTiming.AfterTurnEnd,
                new ConstCondition { Value = true });
            var addedBuffData = BuffTestBuilder.CreatePlayerBuffData(
                addedBuffId,
                GameTiming.BeforeTurnEnd,
                new ConditionalPlayerBuffEffect
                {
                    Conditions = { new ConstCondition { Value = true } },
                    Effect = new EffectiveDamagePlayerBuffEffect
                    {
                        Targets = new SingleCharacterCollection
                        {
                            Target = new MainCharacterOfPlayer
                            {
                                Player = new CurrentPlayer()
                            }
                        },
                        Value = new ConstInteger { Value = 10 }
                    }
                });
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(observerBuffData)
                .WithPlayerBuff(addedBuffData)
                .Build();
            var addedBuff = CreatePlayerBuff(addedBuffId);
            var addingSession = new AddPlayerBuffOnObservationSession(
                built.Ally.BuffManager,
                addedBuff);
            built.Ally.BuffManager.AddBuff(
                CreatePlayerBuff(observerBuffId, addingSession));

            built.Manager
                .TriggerTiming(GameTiming.BeforeTurnEnd, SystemSource.Instance)
                .ToList();

            Assert.That(built.Ally.BuffManager.Buffs, Does.Contain(addedBuff));
            Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(100));
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

        [Test]
        public void BooleanSession_FirstConditionFalse_UsesNextRuleInTheSameTimingRule()
        {
            var session = new SessionBooleanEntity(
                initialValue: false,
                updateRules: new List<SessionBoolean.TimingRule>
                {
                    new()
                    {
                        Timing = GameTiming.BeforeTurnEnd,
                        Rules = new[]
                        {
                            new ConditionBooleanUpdateRule
                            {
                                Conditions = new ICondition[]
                                {
                                    new ConstCondition { Value = false }
                                },
                                NewValue = new FalseValue()
                            },
                            new ConditionBooleanUpdateRule
                            {
                                Conditions = new ICondition[]
                                {
                                    new ConstCondition { Value = true }
                                },
                                NewValue = new TrueValue()
                            }
                        }
                    }
                });
            var built = new GameplayManagerTestBuilder().Build();

            var isUpdated = session.Update(
                CreateTimingContext(built.Manager, GameTiming.BeforeTurnEnd));

            Assert.That(isUpdated, Is.True);
            Assert.That(session.Value, Is.True);
        }

        [Test]
        public void BooleanSession_DuplicateTimingRules_UsesOnlyTheFirstTimingRule()
        {
            var session = new SessionBooleanEntity(
                initialValue: false,
                updateRules: new List<SessionBoolean.TimingRule>
                {
                    CreateBooleanTimingRule(new TrueValue()),
                    CreateBooleanTimingRule(new FalseValue())
                });
            var built = new GameplayManagerTestBuilder().Build();

            session.Update(CreateTimingContext(
                built.Manager,
                GameTiming.BeforeTurnEnd));

            Assert.That(session.Value, Is.True);
        }

        [Test]
        public void IntegerSession_DuplicateTimingRules_UsesOnlyTheFirstTimingRule()
        {
            var session = new SessionIntegerEntity(
                initialValue: 0,
                updateRules: new List<SessionInteger.TimingRule>
                {
                    CreateIntegerTimingRule(1),
                    CreateIntegerTimingRule(10)
                });
            var built = new GameplayManagerTestBuilder().Build();

            session.Update(CreateTimingContext(
                built.Manager,
                GameTiming.BeforeTurnEnd));

            Assert.That(session.Value, Is.EqualTo(1));
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

        private static SessionBoolean.TimingRule CreateBooleanTimingRule(
            IBooleanValue newValue)
        {
            return new SessionBoolean.TimingRule
            {
                Timing = GameTiming.BeforeTurnEnd,
                Rules = new[]
                {
                    new ConditionBooleanUpdateRule
                    {
                        Conditions = Array.Empty<ICondition>(),
                        NewValue = newValue
                    }
                }
            };
        }

        private static SessionInteger.TimingRule CreateIntegerTimingRule(
            int addition)
        {
            return new SessionInteger.TimingRule
            {
                Timing = GameTiming.BeforeTurnEnd,
                Rules = new[]
                {
                    new ConditionIntegerUpdateRule
                    {
                        Conditions = Array.Empty<ICondition>(),
                        Operation = ConditionIntegerUpdateRule.UpdateType.AddOrigin,
                        NewValue = new ConstInteger { Value = addition }
                    }
                }
            };
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

    internal sealed record ObservedResultAction : IActionUnit
    {
        public GameTiming Timing => GameTiming.EffectTargetResult;
        public IActionSource Source => SystemSource.Instance;
    }

    internal sealed class AddPlayerBuffOnObservationSession : IReactionSessionEntity
    {
        private readonly IPlayerBuffManager _buffManager;
        private readonly IPlayerBuffEntity _buff;
        private bool _isAdded;

        public bool IsSessionValueUpdated => _isAdded;
        public Option<bool> BooleanValue => Option.None<bool>();
        public Option<int> IntegerValue => Option.None<int>();

        public AddPlayerBuffOnObservationSession(
            IPlayerBuffManager buffManager,
            IPlayerBuffEntity buff)
        {
            _buffManager = buffManager;
            _buff = buff;
        }

        public bool Update(TriggerContext triggerContext)
        {
            if (_isAdded)
                return false;

            _buffManager.AddBuff(_buff);
            _isAdded = true;
            return true;
        }

        public IReactionSessionEntity Clone()
        {
            return new AddPlayerBuffOnObservationSession(_buffManager, _buff);
        }
    }
}
