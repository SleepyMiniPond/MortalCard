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
}
