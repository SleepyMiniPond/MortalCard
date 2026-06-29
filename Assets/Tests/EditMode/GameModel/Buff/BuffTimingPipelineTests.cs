using System;
using System.Linq;
using NUnit.Framework;

public class BuffTimingPipelineTests
{
    [Test]
    public void Constructor_WithInitialStatus_UsesControlledStatus()
    {
        var built = new GameplayManagerTestBuilder().Build();

        Assert.AreSame(built.Status, ((IGameplayModel)built.Manager).GameStatus);
        Assert.AreSame(built.Ally, built.Status.Ally);
        Assert.AreSame(built.Enemy, built.Status.Enemy);
    }

    [Test]
    public void TriggerTiming_PlayerBuffAtMatchingTiming_EmitsTriggerBuffStartUpdates()
    {
        var conditionalEffect = new ConditionalPlayerBuffEffect
        {
            Conditions = { new ConstCondition { Value = true } },
            Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
            {
                Type = EffectAttributeAdditionType.PowerAddition,
                Value = new ConstInteger { Value = 1 }
            }
        };
        var playerBuffData = BuffTestBuilder.CreatePlayerBuffData(
            BuffTestBuilder.PlayerBuffId,
            GameTiming.TurnEnd,
            conditionalEffect);
        var built = new GameplayManagerTestBuilder()
            .WithPlayerBuff(playerBuffData)
            .Build();
        built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff());

        var events = built.Manager.TriggerTiming(GameTiming.TurnEnd, SystemSource.Instance).ToList();

        Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void TriggerTiming_PlayerBuffConditionFalse_DoesNotEmitTriggerBuffStartUpdates()
    {
        var conditionalEffect = new ConditionalPlayerBuffEffect
        {
            Conditions = { new ConstCondition { Value = false } },
            Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
            {
                Type = EffectAttributeAdditionType.PowerAddition,
                Value = new ConstInteger { Value = 1 }
            }
        };
        var playerBuffData = BuffTestBuilder.CreatePlayerBuffData(
            BuffTestBuilder.PlayerBuffId,
            GameTiming.TurnEnd,
            conditionalEffect);
        var built = new GameplayManagerTestBuilder()
            .WithPlayerBuff(playerBuffData)
            .Build();
        built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff());

        var events = built.Manager.TriggerTiming(GameTiming.TurnEnd, SystemSource.Instance).ToList();

        Assert.That(events, Is.Empty);
    }

    [Test]
    public void TriggerTiming_CharacterBuffAtMatchingTiming_EmitsTriggerBuffStartUpdates()
    {
        var conditionalEffect = new ConditionalCharacterBuffEffect
        {
            Conditions = new ICharacterBuffCondition[] { new ConstCondition { Value = true } },
            Effect = new EffectiveDamageCharacterBuffEffect
            {
                Targets = new NoneCharacters(),
                Value = new ConstInteger { Value = 1 }
            }
        };
        var characterBuffData = BuffTestBuilder.CreateCharacterBuffData(
            BuffTestBuilder.CharacterBuffId,
            GameTiming.TurnEnd,
            conditionalEffect);
        var built = new GameplayManagerTestBuilder()
            .WithCharacterBuff(characterBuffData)
            .Build();
        built.Ally.MainCharacter.BuffManager.AddBuff(BuffTestBuilder.CreateCharacterBuff());

        var events = built.Manager.TriggerTiming(GameTiming.TurnEnd, SystemSource.Instance).ToList();

        Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void TriggerTiming_CardBuffAtMatchingTiming_EvaluatesConditionWithSelectedCardContext()
    {
        ICardEntity observedCard = null;
        var conditionalEffect = new ConditionalCardBuffEffect
        {
            Conditions = { new SelectedCardIsExpectedCondition(() => observedCard.Identity) },
            Effect = new NoOpCardBuffEffect()
        };
        var cardBuffData = BuffTestBuilder.CreateCardBuffData(
            BuffTestBuilder.CardBuffId,
            GameTiming.TurnEnd,
            conditionalEffect);
        var built = new GameplayManagerTestBuilder()
            .WithCard(CardTestBuilder.CreateCardData())
            .WithCardBuff(cardBuffData)
            .Build();
        var context = new TriggerContext(
            built.Manager,
            new PlayerTrigger(built.Ally),
            new UpdateTimingAction(GameTiming.TurnEnd, SystemSource.Instance));
        observedCard = CardTestBuilder.CreateCardWithBuff(
            context,
            built.ContextManager.CardBuffLibrary,
            built.ContextManager.CardLibrary);
        built.Ally.CardManager.HandCard.AddCard(observedCard);

        var events = built.Manager.TriggerTiming(GameTiming.TurnEnd, SystemSource.Instance).ToList();

        Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(2));
        Assert.AreEqual(GameContext.EMPTY, built.ContextManager.Context);
    }

    [Test]
    public void CreateTriggerTimingQueueItems_WhenAllBuffTypesMatchTiming_ReturnsQueueItemsForEachBuffType()
    {
        var playerBuffData = BuffTestBuilder.CreatePlayerBuffData(
            BuffTestBuilder.PlayerBuffId,
            GameTiming.TurnEnd,
            new ConditionalPlayerBuffEffect
            {
                Conditions = { new ConstCondition { Value = true } },
                Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                {
                    Type = EffectAttributeAdditionType.PowerAddition,
                    Value = new ConstInteger { Value = 1 }
                }
            });
        var characterBuffData = BuffTestBuilder.CreateCharacterBuffData(
            BuffTestBuilder.CharacterBuffId,
            GameTiming.TurnEnd,
            new ConditionalCharacterBuffEffect
            {
                Conditions = new ICharacterBuffCondition[] { new ConstCondition { Value = true } },
                Effect = new EffectiveDamageCharacterBuffEffect
                {
                    Targets = new NoneCharacters(),
                    Value = new ConstInteger { Value = 1 }
                }
            });
        var cardBuffData = BuffTestBuilder.CreateCardBuffData(
            BuffTestBuilder.CardBuffId,
            GameTiming.TurnEnd,
            new ConditionalCardBuffEffect
            {
                Conditions = { new ConstCondition { Value = true } },
                Effect = new NoOpCardBuffEffect()
            });
        var built = new GameplayManagerTestBuilder()
            .WithCard(CardTestBuilder.CreateCardData())
            .WithPlayerBuff(playerBuffData)
            .WithCharacterBuff(characterBuffData)
            .WithCardBuff(cardBuffData)
            .Build();
        built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff());
        built.Ally.MainCharacter.BuffManager.AddBuff(BuffTestBuilder.CreateCharacterBuff());
        var context = new TriggerContext(
            built.Manager,
            new PlayerTrigger(built.Ally),
            new UpdateTimingAction(GameTiming.TurnEnd, SystemSource.Instance));
        var card = CardTestBuilder.CreateCardWithBuff(
            context,
            built.ContextManager.CardBuffLibrary,
            built.ContextManager.CardLibrary);
        built.Ally.CardManager.HandCard.AddCard(card);

        var items = built.Manager.CreateTriggerTimingQueueItems(GameTiming.TurnEnd, SystemSource.Instance);

        Assert.That(items.Count, Is.EqualTo(3));
        Assert.That(items, Has.Exactly(1).TypeOf<TriggeredPlayerBuffEffectQueueItem>());
        Assert.That(items, Has.Exactly(1).TypeOf<TriggeredCharacterBuffEffectQueueItem>());
        Assert.That(items, Has.Exactly(1).TypeOf<TriggeredCardBuffEffectQueueItem>());
        Assert.AreEqual(GameContext.EMPTY, built.ContextManager.Context);
    }

    [Test]
    public void TriggerTiming_WhenBuffTriggers_ThenTriggerBuffEndTimingIsProcessed()
    {
        var firstBuffData = BuffTestBuilder.CreatePlayerBuffData(
            "first-buff",
            GameTiming.TurnEnd,
            new ConditionalPlayerBuffEffect
            {
                Conditions = { new ConstCondition { Value = true } },
                Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                {
                    Type = EffectAttributeAdditionType.PowerAddition,
                    Value = new ConstInteger { Value = 1 }
                }
            });
        var secondBuffData = BuffTestBuilder.CreatePlayerBuffData(
            "second-buff",
            GameTiming.TriggerBuffEnd,
            new ConditionalPlayerBuffEffect
            {
                Conditions = { new PlayerBuffSourceIdCondition("first-buff") },
                Effect = new CardPlayEffectAttributeAdditionPlayerBuffEffect
                {
                    Type = EffectAttributeAdditionType.PowerAddition,
                    Value = new ConstInteger { Value = 1 }
                }
            });
        var built = new GameplayManagerTestBuilder()
            .WithPlayerBuff(firstBuffData)
            .WithPlayerBuff(secondBuffData)
            .Build();
        built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff("first-buff"));
        built.Ally.BuffManager.AddBuff(BuffTestBuilder.CreatePlayerBuff("second-buff"));

        var events = built.Manager.TriggerTiming(GameTiming.TurnEnd, SystemSource.Instance).ToList();

        Assert.That(events.OfType<GeneralUpdateEvent>().Count(), Is.EqualTo(4));
    }
}

public sealed class SelectedCardIsExpectedCondition : ICardBuffCondition
{
    private readonly Func<Guid> _expectedCardId;

    public SelectedCardIsExpectedCondition(Func<Guid> expectedCardId)
    {
        _expectedCardId = expectedCardId;
    }

    public bool Eval(TriggerContext triggerContext)
    {
        return triggerContext.Model.ContextManager.Context.SelectedCard == _expectedCardId();
    }
}

public sealed class NoOpCardBuffEffect : ICardBuffEffect
{
}

public sealed class PlayerBuffSourceIdCondition : IPlayerBuffCondition
{
    private readonly string _buffId;

    public PlayerBuffSourceIdCondition(string buffId)
    {
        _buffId = buffId;
    }

    public bool Eval(TriggerContext triggerContext)
    {
        return triggerContext.Action.Source is PlayerBuffSource source &&
            source.Buff.PlayerBuffDataId == _buffId;
    }
}
