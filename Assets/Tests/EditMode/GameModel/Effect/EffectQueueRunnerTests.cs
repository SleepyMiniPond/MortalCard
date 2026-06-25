using System.Linq;
using NUnit.Framework;

public class EffectQueueRunnerTests
{
    [Test]
    public void RunToCompletion_WithCardEffects_AppliesCommandsAndCollectsResultsInOrder()
    {
        var built = new GameplayManagerTestBuilder().Build();
        using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
        var context = new TriggerContext(
            built.Manager,
            new PlayerTrigger(built.Ally),
            new GainEnergyIntentAction(SystemSource.Instance));
        var runner = new EffectQueueRunner();
        var firstEffect = new GainEnergyEffect
        {
            Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
            Value = new ConstInteger { Value = 1 }
        };
        var secondEffect = new GainEnergyEffect
        {
            Targets = new SinglePlayerCollection { Target = new CurrentPlayer() },
            Value = new ConstInteger { Value = 2 }
        };

        runner.EnqueueCardEffect(context, firstEffect);
        runner.EnqueueCardEffect(context, secondEffect);
        var result = runner.RunToCompletion();

        Assert.That(built.Ally.CurrentEnergy, Is.EqualTo(3));
        Assert.That(result.Actions.Select(action => action.GetType()).ToArray(), Is.EqualTo(new[]
        {
            typeof(GainEnergyResultAction),
            typeof(GainEnergyResultAction)
        }));
        Assert.That(result.Events.OfType<GainEnergyEvent>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void RunToCompletion_WithPlayerBuffEffect_AppliesCommands()
    {
        var built = new GameplayManagerTestBuilder().Build();
        using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Ally);
        var context = new TriggerContext(
            built.Manager,
            new PlayerBuffTrigger(BuffTestBuilder.CreatePlayerBuff()),
            new UpdateTimingAction(GameTiming.TurnEnd, SystemSource.Instance));
        var runner = new EffectQueueRunner();
        var effect = new EffectiveDamagePlayerBuffEffect
        {
            Targets = new SingleCharacterCollection
            {
                Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
            },
            Value = new ConstInteger { Value = 5 }
        };

        runner.EnqueuePlayerBuffEffect(context, effect);
        var result = runner.RunToCompletion();

        Assert.That(built.Ally.MainCharacter.CurrentHealth, Is.EqualTo(95));
        Assert.That(result.Actions.Single(), Is.TypeOf<DamageResultAction>());
        Assert.That(result.Events.OfType<DamageEvent>().Single().Character, Is.SameAs(built.Ally.MainCharacter));
    }

    [Test]
    public void RunToCompletion_WithCharacterBuffEffect_AppliesCommands()
    {
        var built = new GameplayManagerTestBuilder().Build();
        using var currentPlayerScope = built.Status.SetCurrentPlayer(built.Enemy);
        var context = new TriggerContext(
            built.Manager,
            new CharacterBuffTrigger(BuffTestBuilder.CreateCharacterBuff()),
            new UpdateTimingAction(GameTiming.TurnEnd, SystemSource.Instance));
        var runner = new EffectQueueRunner();
        var effect = new EffectiveDamageCharacterBuffEffect
        {
            Targets = new SingleCharacterCollection
            {
                Target = new MainCharacterOfPlayer { Player = new CurrentPlayer() }
            },
            Value = new ConstInteger { Value = 7 }
        };

        runner.EnqueueCharacterBuffEffect(context, effect);
        var result = runner.RunToCompletion();

        Assert.That(built.Enemy.MainCharacter.CurrentHealth, Is.EqualTo(93));
        Assert.That(result.Actions.Single(), Is.TypeOf<DamageResultAction>());
        Assert.That(result.Events.OfType<DamageEvent>().Single().Character, Is.SameAs(built.Enemy.MainCharacter));
    }

    [Test]
    public void RunToCompletion_WithUnknownCardBuffEffect_ReturnsEmptyResult()
    {
        var cardBuffData = BuffTestBuilder.CreateCardBuffData(
            BuffTestBuilder.CardBuffId,
            GameTiming.TurnEnd,
            new ConditionalCardBuffEffect
            {
                Conditions = { new ConstCondition { Value = true } },
                Effect = new NoOpCardBuffEffect()
            });
        var built = new GameplayManagerTestBuilder()
            .WithCardBuff(cardBuffData)
            .Build();
        var createBuffContext = new TriggerContext(
            built.Manager,
            new PlayerTrigger(built.Ally),
            new UpdateTimingAction(GameTiming.TurnEnd, SystemSource.Instance));
        var buff = BuffTestBuilder.CreateCardBuff(createBuffContext, built.ContextManager.CardBuffLibrary);
        var context = new TriggerContext(
            built.Manager,
            new CardBuffTrigger(buff),
            new UpdateTimingAction(GameTiming.TurnEnd, SystemSource.Instance));
        var runner = new EffectQueueRunner();

        runner.EnqueueCardBuffEffect(context, new NoOpCardBuffEffect());
        var result = runner.RunToCompletion();

        Assert.That(result.Actions, Is.Empty);
        Assert.That(result.Events, Is.Empty);
    }
}
