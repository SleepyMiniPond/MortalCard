using System;
using MortalGame.Exceptions;
using NUnit.Framework;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Tests
{

    public class CharacterBuffRuntimeComponentFactoryTests
    {
        private ICharacterBuffPropertyEntityFactory _propertyFactory;
        private ICharacterBuffLifeTimeEntityFactory _lifeTimeFactory;

        [SetUp]
        public void SetUp()
        {
            _propertyFactory = CharacterBuffPropertyEntityFactory.CreateDefault();
            _lifeTimeFactory = CharacterBuffLifeTimeEntityFactory.CreateDefault();
        }

        [TestCase(typeof(MaxHealthPropertyCharacterBuffData), typeof(MaxHealthPropertyCharacterBuffEntity))]
        [TestCase(typeof(MaxEnergyPropertyCharacterBuffData), typeof(MaxEnergyPropertyCharacterBuffEntity))]
        public void CreateProperty_KnownDataType_ReturnsExpectedEntityType(Type dataType, Type expectedEntityType)
        {
            var data = (ICharacterBuffPropertyData)Activator.CreateInstance(dataType);

            Assert.That(_propertyFactory.Create(data), Is.TypeOf(expectedEntityType));
        }

        [Test]
        public void CreateProperty_UnknownDataType_Throws()
        {
            var error = Assert.Throws<UnregisteredDataTypeException>(() => _propertyFactory.Create(new UnknownPropertyData()));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(CharacterBuffPropertyEntityFactory)));
            Assert.That(error.ActualType, Is.EqualTo(typeof(UnknownPropertyData)));
            Assert.That(error.ParamName, Is.EqualTo("data"));
        }

        [TestCase(typeof(AlwaysLifeTimeCharacterBuffData), typeof(AlwaysLifeTimeCharacterBuffEntity))]
        [TestCase(typeof(TurnLifeTimeCharacterBuffData), typeof(TurnLifeTimeCharacterBuffEntity))]
        public void CreateLifeTime_KnownDataType_ReturnsExpectedEntityType(Type dataType, Type expectedEntityType)
        {
            var data = (ICharacterBuffLifeTimeData)Activator.CreateInstance(dataType);

            Assert.That(_lifeTimeFactory.Create(data), Is.TypeOf(expectedEntityType));
        }

        [Test]
        public void CreateLifeTime_UnknownDataType_Throws()
        {
            var error = Assert.Throws<UnregisteredDataTypeException>(() => _lifeTimeFactory.Create(new UnknownLifeTimeData()));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(CharacterBuffLifeTimeEntityFactory)));
            Assert.That(error.ActualType, Is.EqualTo(typeof(UnknownLifeTimeData)));
            Assert.That(error.ParamName, Is.EqualTo("data"));
        }

        private sealed class UnknownPropertyData : ICharacterBuffPropertyData { }
        private sealed class UnknownLifeTimeData : ICharacterBuffLifeTimeData { }
    }

}
