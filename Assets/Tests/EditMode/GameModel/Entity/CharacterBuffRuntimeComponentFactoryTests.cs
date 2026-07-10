using System;
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
        Assert.Throws<ArgumentException>(() => _propertyFactory.Create(new UnknownPropertyData()));
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
        Assert.Throws<ArgumentException>(() => _lifeTimeFactory.Create(new UnknownLifeTimeData()));
    }

    private sealed class UnknownPropertyData : ICharacterBuffPropertyData { }
    private sealed class UnknownLifeTimeData : ICharacterBuffLifeTimeData { }
}

}
