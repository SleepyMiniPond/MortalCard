using System;
using NUnit.Framework;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Tests
{

    public class CardBuffRuntimeComponentFactoryTests
    {
        private ICardBuffPropertyEntityFactory _propertyFactory;
        private ICardBuffLifeTimeEntityFactory _lifeTimeFactory;

        [SetUp]
        public void SetUp()
        {
            _propertyFactory = CardBuffPropertyEntityFactory.CreateDefault();
            _lifeTimeFactory = CardBuffLifeTimeEntityFactory.CreateDefault();
        }

        [TestCase(typeof(SealedCardBuffPropertyData), typeof(SealedCardBuffPropertyEntity))]
        [TestCase(typeof(PowerCardBuffPropertyData), typeof(PowerCardBuffPropertyEntity))]
        public void CreateProperty_KnownDataType_ReturnsExpectedEntityType(Type dataType, Type expectedEntityType)
        {
            var data = (ICardBuffPropertyData)Activator.CreateInstance(dataType);

            var entity = _propertyFactory.Create(data);

            Assert.That(entity, Is.TypeOf(expectedEntityType));
        }

        [Test]
        public void CreateProperty_UnknownDataType_Throws()
        {
            var factory = new CardBuffPropertyEntityFactory(
                Array.Empty<ICardBuffPropertyEntityCreator>());

            Assert.Throws<ArgumentException>(() => factory.Create(new SealedCardBuffPropertyData()));
        }

        [TestCase(typeof(AlwaysLifeTimeCardBuffData), typeof(AlwaysLifeTimeCardBuffEntity))]
        [TestCase(typeof(HandCardLifeTimeCardBuffData), typeof(HandCardLifeTimeCardBuffEntity))]
        public void CreateLifeTime_KnownDataType_ReturnsExpectedEntityType(Type dataType, Type expectedEntityType)
        {
            var data = (ICardBuffLifeTimeData)Activator.CreateInstance(dataType);

            var entity = _lifeTimeFactory.Create(data, null);

            Assert.That(entity, Is.TypeOf(expectedEntityType));
        }

        [Test]
        public void CreateLifeTime_TurnData_EvaluatesTurnValue()
        {
            var data = new TurnLifeTimeCardBuffData
            {
                Turn = new ConstInteger { Value = 1 },
            };

            var entity = _lifeTimeFactory.Create(data, null);

            Assert.That(entity.IsExpired(), Is.False);
        }

        [Test]
        public void CreateLifeTime_UnknownDataType_Throws()
        {
            var factory = new CardBuffLifeTimeEntityFactory(
                Array.Empty<ICardBuffLifeTimeEntityCreator>());

            Assert.Throws<ArgumentException>(() => factory.Create(new AlwaysLifeTimeCardBuffData(), null));
        }
    }

}
