using System;
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
            Assert.Throws<ArgumentException>(() => _factory.Create(new UnknownCardPropertyData()));
        }

        private sealed class UnknownCardPropertyData : ICardPropertyData
        {
        }
    }

}
