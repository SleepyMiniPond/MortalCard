using System;
using MortalGame.Exceptions;
using NUnit.Framework;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Tests
{

    public class PlayerBuffRuntimeComponentFactoryTests
    {
        private IPlayerBuffPropertyEntityFactory _propertyFactory;
        private IPlayerBuffLifeTimeEntityFactory _lifeTimeFactory;

        [SetUp]
        public void SetUp()
        {
            _propertyFactory = PlayerBuffPropertyEntityFactory.CreateDefault();
            _lifeTimeFactory = PlayerBuffLifeTimeEntityFactory.CreateDefault();
        }

        [TestCase(typeof(AllCardPowerPlayerBuffPropertyData), typeof(AllCardPowerPlayerBuffPropertyEntity))]
        [TestCase(typeof(AllCardCostPlayerBuffPropertyData), typeof(AllCardCostPlayerBuffPropertyEntity))]
        [TestCase(typeof(NormalDamageAdditionPlayerBuffPropertyData), typeof(NormalDamageAdditionPlayerBuffPropertyEntity))]
        [TestCase(typeof(NormalDamageRatioPlayerBuffPropertyData), typeof(NormalDamageRatioPlayerBuffPropertyEntity))]
        [TestCase(typeof(MaxHealthPlayerBuffPropertyData), typeof(MaxHealthPlayerBuffPropertyEntity))]
        [TestCase(typeof(MaxEnergyPlayerBuffPropertyData), typeof(MaxEnergyPlayerBuffPropertyEntity))]
        public void CreateProperty_KnownDataType_ReturnsExpectedEntityType(Type dataType, Type expectedEntityType)
        {
            var data = (IPlayerBuffPropertyData)Activator.CreateInstance(dataType);

            Assert.That(_propertyFactory.Create(data), Is.TypeOf(expectedEntityType));
        }

        [Test]
        public void CreateProperty_UnknownDataType_Throws()
        {
            var error = Assert.Throws<UnregisteredDataTypeException>(() => _propertyFactory.Create(new UnknownPropertyData()));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(PlayerBuffPropertyEntityFactory)));
            Assert.That(error.ActualType, Is.EqualTo(typeof(UnknownPropertyData)));
            Assert.That(error.ParamName, Is.EqualTo("data"));
        }

        [TestCase(typeof(AlwaysLifeTimePlayerBuffData), typeof(AlwaysLifeTimePlayerBuffEntity))]
        public void CreateLifeTime_KnownDataType_ReturnsExpectedEntityType(Type dataType, Type expectedEntityType)
        {
            var data = (IPlayerBuffLifeTimeData)Activator.CreateInstance(dataType);

            var entity = _lifeTimeFactory.Create(data, null);

            Assert.That(entity.TryGetValue(out var value), Is.True);
            Assert.That(value, Is.TypeOf(expectedEntityType));
        }

        [Test]
        public void CreateLifeTime_TurnData_EvaluatesTurnValue()
        {
            var data = new PlayerBuffTurnLifeTimeData { Turn = new ConstInteger { Value = 1 } };

            var entity = _lifeTimeFactory.Create(data, null);

            Assert.That(entity.TryGetValue(out var value), Is.True);
            Assert.That(value.IsExpired(), Is.False);
        }

        [Test]
        public void CreateLifeTime_UnknownDataType_Throws()
        {
            var error = Assert.Throws<UnregisteredDataTypeException>(() => _lifeTimeFactory.Create(new UnknownLifeTimeData(), null));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(PlayerBuffLifeTimeEntityFactory)));
            Assert.That(error.ActualType, Is.EqualTo(typeof(UnknownLifeTimeData)));
            Assert.That(error.ParamName, Is.EqualTo("data"));
        }

        private sealed class UnknownPropertyData : IPlayerBuffPropertyData { }
        private sealed class UnknownLifeTimeData : IPlayerBuffLifeTimeData { }
    }

}
