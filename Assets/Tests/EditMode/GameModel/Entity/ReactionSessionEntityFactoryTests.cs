using System;
using MortalGame.GameData;
using NUnit.Framework;
using Optional;
using MortalGame.GameModel;

namespace MortalGame.Tests
{

    public class ReactionSessionEntityFactoryTests
    {
        private IReactionSessionEntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = ReactionSessionEntityFactory.CreateDefault();
        }

        [Test]
        public void Create_BooleanData_PreservesInitialValue()
        {
            var data = new SessionBoolean
            {
                InitialValue = true,
                LifeTime = SessionLifeTime.WholeGame,
            };

            var entity = _factory.Create(data);

            Assert.That(entity.BooleanValue.ValueOr(false), Is.True);
        }

        [Test]
        public void Create_IntegerData_PreservesInitialValue()
        {
            var data = new SessionInteger
            {
                InitialValue = 7,
                LifeTime = SessionLifeTime.WholeGame,
            };

            var entity = _factory.Create(data);

            Assert.That(entity.IntegerValue.ValueOr(0), Is.EqualTo(7));
        }

        [Test]
        public void Create_UnknownDataType_Throws()
        {
            Assert.Throws<ArgumentException>(() => _factory.Create(new UnknownReactionSessionData()));
        }

        private sealed class UnknownReactionSessionData : IReactionSessionData
        {
        }
    }

}
