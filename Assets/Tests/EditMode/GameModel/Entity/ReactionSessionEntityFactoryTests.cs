using System;
using MortalGame.Exceptions;
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
            var error = Assert.Throws<UnregisteredDataTypeException>(() => _factory.Create(new UnknownReactionSessionData()));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(ReactionSessionEntityFactory)));
            Assert.That(error.ActualType, Is.EqualTo(typeof(UnknownReactionSessionData)));
            Assert.That(error.ParamName, Is.EqualTo("data"));
        }

        private sealed class UnknownReactionSessionData : IReactionSessionData
        {
        }
    }

}
