using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public class GameContextManagerTests
{
    [Test]
    public void NewManager_ContextIsEmpty()
    {
        var manager = GameContextTestBuilder.CreateContextManager();

        Assert.AreEqual(GameContext.EMPTY, manager.Context);
        Assert.AreEqual(Guid.Empty, manager.Context.SelectedPlayer);
        Assert.AreEqual(Guid.Empty, manager.Context.SelectedCharacter);
        Assert.AreEqual(Guid.Empty, manager.Context.SelectedCard);
    }

    [Test]
    public void SelectedPlayerScope_PushesAndRestoresPreviousContext()
    {
        var manager = GameContextTestBuilder.CreateContextManager();
        var player = new AllyEntity(
            Guid.NewGuid(),
            new[] { new CharacterParameter { NameKey = "ally", CurrentHealth = 10, MaxHealth = 10 } },
            currentEnergy: 0,
            maxEnergy: 3,
            handCardMaxCount: 5,
            currentDisposition: 0,
            maxDisposition: 10,
            gameContext: manager);

        using (SetSelectedPlayer(manager, player))
        {
            Assert.AreEqual(player.Identity, manager.Context.SelectedPlayer);
        }

        Assert.AreEqual(GameContext.EMPTY, manager.Context);
    }

    [Test]
    public void NestedSelectedScopes_RestoreOneLevelAtATime()
    {
        var manager = GameContextTestBuilder.CreateContextManager();
        var player = new AllyEntity(
            Guid.NewGuid(),
            new[] { new CharacterParameter { NameKey = "ally", CurrentHealth = 10, MaxHealth = 10 } },
            currentEnergy: 0,
            maxEnergy: 3,
            handCardMaxCount: 5,
            currentDisposition: 0,
            maxDisposition: 10,
            gameContext: manager);
        var character = player.MainCharacter;

        using (SetSelectedPlayer(manager, player))
        {
            using (SetSelectedCharacter(manager, character))
            {
                Assert.AreEqual(player.Identity, manager.Context.SelectedPlayer);
                Assert.AreEqual(character.Identity, manager.Context.SelectedCharacter);
            }

            Assert.AreEqual(player.Identity, manager.Context.SelectedPlayer);
            Assert.AreEqual(Guid.Empty, manager.Context.SelectedCharacter);
        }

        Assert.AreEqual(GameContext.EMPTY, manager.Context);
    }

    [Test]
    public void NoneSelection_PushesCloneAndRestoresPreviousContext()
    {
        var manager = GameContextTestBuilder.CreateContextManager();

        using (SetSelectedCardToNone(manager))
        {
            Assert.AreEqual(GameContext.EMPTY, manager.Context);
        }

        Assert.AreEqual(GameContext.EMPTY, manager.Context);
    }

    private static IDisposable SetSelectedPlayer(GameContextManager manager, IPlayerEntity player)
    {
        return (IDisposable)GetContextMethod(nameof(GameContextManager.SetSelectedPlayer), typeof(IPlayerEntity))
            .Invoke(manager, new[] { OptionTestValue.Some(typeof(IPlayerEntity), player) });
    }

    private static IDisposable SetSelectedCharacter(GameContextManager manager, ICharacterEntity character)
    {
        return (IDisposable)GetContextMethod(nameof(GameContextManager.SetSelectedCharacter), typeof(ICharacterEntity))
            .Invoke(manager, new[] { OptionTestValue.Some(typeof(ICharacterEntity), character) });
    }

    private static IDisposable SetSelectedCardToNone(GameContextManager manager)
    {
        return (IDisposable)GetContextMethod(nameof(GameContextManager.SetSelectedCard), typeof(ICardEntity))
            .Invoke(manager, new[] { OptionTestValue.None(typeof(ICardEntity)) });
    }

    private static MethodInfo GetContextMethod(string name, Type valueType)
    {
        var parameterType = OptionTestValue.OptionOf(valueType);
        return typeof(GameContextManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(method =>
                method.Name == name &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType == parameterType);
    }
}
