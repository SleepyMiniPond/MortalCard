using System.Collections.Generic;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public class TurnCountIntegerTests
    {
        private const string ObserverBuffId = "turn-count-observer";

        [Test]
        public void Eval_ReturnsCurrentTurnCount()
        {
            var built = new GameplayManagerTestBuilder().Build();
            built.Status.SetNewTurn();
            built.Status.SetNewTurn();
            var context = _CreateTimingContext(built, GameTiming.AfterTurnStart);

            var result = new TurnCountInteger().Eval(context);

            Assert.That(result.TryGetValue(out var value), Is.True);
            Assert.That(value, Is.EqualTo(2));
        }

        [Test]
        public void TurnStart_BeforeAndAfterTimingsReadSameNewTurnCount()
        {
            var observer = new TurnCountRecordingSession();
            var buffData = new PlayerBuffData
            {
                ID = ObserverBuffId,
                MaxLevel = 1,
                LifeTimeData = new AlwaysLifeTimePlayerBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithPlayerBuff(buffData)
                .Build();
            built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff(
                buffId: ObserverBuffId,
                reactionSessions: new Dictionary<string, IReactionSessionEntity>
                {
                    { "observer", observer }
                }));
            _SetGameEvents(built.Manager);

            _InvokeTurnStart(built.Manager);

            Assert.That(built.Status.TurnCount, Is.EqualTo(1));
            Assert.That(observer.ObservedTurnCounts[GameTiming.BeforeTurnStart], Is.EqualTo(1));
            Assert.That(observer.ObservedTurnCounts[GameTiming.AfterTurnStart], Is.EqualTo(1));
        }

        private static TriggerContext _CreateTimingContext(
            BuiltGameplay built,
            GameTiming timing)
        {
            return new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(timing, SystemSource.Instance));
        }

        private static void _SetGameEvents(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetField("_gameEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, new List<IGameEvent>());
        }

        private static void _InvokeTurnStart(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetMethod("_TurnStart", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(manager, null);
        }

        private sealed class TurnCountRecordingSession : IReactionSessionEntity
        {
            public Dictionary<GameTiming, int> ObservedTurnCounts { get; } = new();
            public bool IsSessionValueUpdated => false;
            public Option<bool> BooleanValue => Option.None<bool>();
            public Option<int> IntegerValue => Option.None<int>();

            public bool Update(TriggerContext triggerContext)
            {
                if (triggerContext.Action.Timing is
                    GameTiming.BeforeTurnStart or GameTiming.AfterTurnStart)
                {
                    var turnCount = new TurnCountInteger()
                        .Eval(triggerContext)
                        .ValueOr(-1);
                    ObservedTurnCounts[triggerContext.Action.Timing] = turnCount;
                }

                return false;
            }

            public IReactionSessionEntity Clone()
            {
                return new TurnCountRecordingSession();
            }
        }
    }
}
