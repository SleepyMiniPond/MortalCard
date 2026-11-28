using System;
using MortalGame.Exceptions;
using MortalGame.GameData;
using NUnit.Framework;
using MortalGame.GameModel;

namespace MortalGame.Tests
{

    public class CardPropertyEntityFactoryTests
    {
        private ICardPropertyEntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = CardPropertyEntityFactory.CreateDefault();
        }

        [TestCase(typeof(EffectRepeatPropertyData), typeof(EffectRepeatPropertyEntity))]
        [TestCase(typeof(PreservedPropertyData), typeof(PreservedPropertyEntity))]
        [TestCase(typeof(InitialPriorityPropertyData), typeof(InitialPriorityPropertyEntity))]
        [TestCase(typeof(ConsumablePropertyData), typeof(ConsumablePropertyEntity))]
        [TestCase(typeof(DisposePropertyData), typeof(DisposePropertyEntity))]
        [TestCase(typeof(AutoDisposePropertyData), typeof(AutoDisposePropertyEntity))]
        [TestCase(typeof(SealedPropertyData), typeof(SealedPropertyEntity))]
        [TestCase(typeof(RecyclePropertyData), typeof(RecyclePropertyEntity))]
        public void Create_KnownDataType_ReturnsExpectedEntityType(Type dataType, Type expectedEntityType)
        {
            var data = (ICardPropertyData)Activator.CreateInstance(dataType);

            var entity = _factory.Create(data);

            Assert.That(entity, Is.TypeOf(expectedEntityType));
        }

        [Test]
        public void Create_UnknownDataType_Throws()
        {
            var error = Assert.Throws<UnregisteredDataTypeException>(() => _factory.Create(new UnknownCardPropertyData()));
            Assert.That(error.OwnerType, Is.EqualTo(typeof(CardPropertyEntityFactory)));
            Assert.That(error.ActualType, Is.EqualTo(typeof(UnknownCardPropertyData)));
            Assert.That(error.ParamName, Is.EqualTo("data"));
        }

        [Test]
        public void Create_NullData_PreservesStandardParameterException()
        {
            var error = Assert.Throws<ArgumentNullException>(() => _factory.Create(null));
            Assert.That(error.ParamName, Is.EqualTo("data"));
        }

        private sealed class UnknownCardPropertyData : ICardPropertyData
        {
        }
    }

}
