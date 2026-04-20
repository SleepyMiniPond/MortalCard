using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Optional.Collections;
using Sirenix.Utilities;

public record EffectCommandSet(
    IReadOnlyCollection<IEffectCommand> Commands);

public static class EffectDataResolver
{
    #region CardEffect
    public static EffectCommandSet ResolveCardEffect(
        TriggerContext context,
        ICardEffect cardEffect)
    {
        return cardEffect switch
        {
            // Damage Effects
            DamageEffect damageEffect =>
                _ResolveDamageEffect(context, damageEffect.Targets, damageEffect.Value, DamageType.Normal,
                    GameFormula.NormalDamagePoint),

            PenetrateDamageEffect penetrateDamageEffect =>
                _ResolveDamageEffect(context, penetrateDamageEffect.Targets, penetrateDamageEffect.Value, DamageType.Penetrate,
                    GameFormula.PenetrateDamagePoint),

            AdditionalAttackEffect additionalAttackEffect =>
                _ResolveDamageEffect(context, additionalAttackEffect.Targets, additionalAttackEffect.Value, DamageType.Additional,
                    GameFormula.AdditionalDamagePoint),

            EffectiveAttackEffect effectiveAttackEffect =>
                _ResolveDamageEffect(context, effectiveAttackEffect.Targets, effectiveAttackEffect.Value, DamageType.Effective,
                    GameFormula.EffectiveDamagePoint),

            // Character Health Effects
            HealEffect healEffect =>
                _ResolveHealEffect(context, healEffect),

            ShieldEffect shieldEffect =>
                _ResolveShieldEffect(context, shieldEffect),

            // Energy Effects
            GainEnergyEffect gainEnergyEffect =>
                _ResolveGainEnergyEffect(context, gainEnergyEffect),

            LoseEnegyEffect loseEnergyEffect =>
                _ResolveLoseEnergyEffect(context, loseEnergyEffect),

            // Disposition Effects
            IncreaseDispositionEffect increaseDispositionEffect =>
                _ResolveIncreaseDispositionEffect(context, increaseDispositionEffect),

            DecreaseDispositionEffect decreaseDispositionEffect =>
                _ResolveDecreaseDispositionEffect(context, decreaseDispositionEffect),

            // Player Buff Effects
            AddPlayerBuffEffect addBuffEffect =>
                _ResolveAddPlayerBuffEffect(context, addBuffEffect),

            RemovePlayerBuffEffect removeBuffEffect =>
                _ResolveRemovePlayerBuffEffect(context, removeBuffEffect),

            // Card Effects
            DrawCardEffect drawCardEffect =>
                _ResolveDrawCardEffect(context, drawCardEffect),

            DiscardCardEffect discardCardEffect =>
                _ResolveDiscardCardEffect(context, discardCardEffect),

            ConsumeCardEffect consumeCardEffect =>
                _ResolveConsumeCardEffect(context, consumeCardEffect),

            DisposeCardEffect disposeCardEffect =>
                _ResolveDisposeCardEffect(context, disposeCardEffect),

            CreateCardEffect createCardEffect =>
                _ResolveCreateCardEffect(context, createCardEffect),

            CloneCardEffect cloneCardEffect =>
                _ResolveCloneCardEffect(context, cloneCardEffect),

            AddCardBuffEffect addCardBuffEffect =>
                _ResolveAddCardBuffEffect(context, addCardBuffEffect),

            RemoveCardBuffEffect removeCardBuffEffect =>
                _ResolveRemoveCardBuffEffect(context, removeCardBuffEffect),

            _ => new EffectCommandSet(Array.Empty<IEffectCommand>())
        };
    }

    #region Damage Effect Handlers
    private static EffectCommandSet _ResolveDamageEffect(
        TriggerContext context,
        ITargetCharacterCollectionValue targets,
        IIntegerValue value,
        DamageType damageType,
        Func<TriggerContext, int, int> formulaFunc)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new DamageIntentAction(context.Action.Source, damageType);
        var triggerContext = context with { Action = intent };
        var targetEntities = targets.Eval(triggerContext);

        foreach (var target in targetEntities)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new DamageIntentTargetAction(context.Action.Source, characterTarget, damageType);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var damagePoint = value.Eval(targetTriggerContext);
            var damageFormulaPoint = formulaFunc(targetTriggerContext, damagePoint);

            effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, damageType));
        }
        return new EffectCommandSet(effectCommands);
    }
    #endregion

    #region Character Health Effect Handlers
    private static EffectCommandSet _ResolveHealEffect(TriggerContext context, HealEffect healEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new HealIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = healEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new HealIntentTargetAction(context.Action.Source, characterTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var healPoint = healEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new HealEffectCommand(target, healPoint));
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveShieldEffect(TriggerContext context, ShieldEffect shieldEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new ShieldIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = shieldEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new ShieldIntentTargetAction(context.Action.Source, characterTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            
            var shieldPoint = shieldEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new ShieldEffectCommand(target, shieldPoint));
        }
        return new EffectCommandSet(effectCommands);
    }
    #endregion

    #region Energy Effect Handlers
    private static EffectCommandSet _ResolveGainEnergyEffect(TriggerContext context, GainEnergyEffect gainEnergyEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new GainEnergyIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = gainEnergyEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var playerTarget = new PlayerTarget(target);
            var targetIntent = new GainEnergyIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var gainEnergyPoint = gainEnergyEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new GainEnergyEffectCommand(target, gainEnergyPoint));
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveLoseEnergyEffect(TriggerContext context, LoseEnegyEffect loseEnergyEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new LoseEnergyIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = loseEnergyEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var playerTarget = new PlayerTarget(target);
            var targetIntent = new LoseEnergyIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };

            var loseEnergyPoint = loseEnergyEffect.Value.Eval(targetTriggerContext);

            effectCommands.Add(new LoseEnergyEffectCommand(target, loseEnergyPoint));
        }
        return new EffectCommandSet(effectCommands);
    }
    #endregion

    #region Disposition Effect Handlers
    private static EffectCommandSet _ResolveIncreaseDispositionEffect(TriggerContext context, IncreaseDispositionEffect increaseDispositionEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new IncreaseDispositionIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = increaseDispositionEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            if (target is AllyEntity ally)
            {
                var playerTarget = new PlayerTarget(ally);
                var targetIntent = new IncreaseDispositionIntentTargetAction(context.Action.Source, playerTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                var increasePoint = increaseDispositionEffect.Value.Eval(targetTriggerContext);

                effectCommands.Add(new IncreaseDispositionEffectCommand(ally, increasePoint));
            }
        }
        return new EffectCommandSet(effectCommands);
    }
    private static EffectCommandSet _ResolveDecreaseDispositionEffect(TriggerContext context, DecreaseDispositionEffect decreaseDispositionEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new DecreaseDispositionIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = decreaseDispositionEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            if (target is AllyEntity ally)
            {
                var playerTarget = new PlayerTarget(ally);
                var targetIntent = new DecreaseDispositionIntentTargetAction(context.Action.Source, playerTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                var decreasePoint = decreaseDispositionEffect.Value.Eval(targetTriggerContext);

                effectCommands.Add(new DecreaseDispositionEffectCommand(ally, decreasePoint));
            }
        }
        return new EffectCommandSet(effectCommands);
    }
    #endregion

    #region Player Buff Effect Handlers
    private static EffectCommandSet _ResolveAddPlayerBuffEffect(TriggerContext context, AddPlayerBuffEffect addBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new AddPlayerBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = addBuffEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var playerTarget = new PlayerTarget(target);
            var targetIntent = new AddPlayerBuffIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var level = addBuffEffect.Level.Eval(targetTriggerContext);

            if (target.BuffManager.Buffs.Any(buff => buff.PlayerBuffDataId == addBuffEffect.BuffId))
            {
                effectCommands.Add(new ModifyPlayerBuffLevelEffectCommand(
                    target,
                    addBuffEffect.BuffId,
                    level));
            }
            else
            {
                var caster = triggerContext.Action.Source switch
                {
                    PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                    CardPlaySource cardPlaySource => cardPlaySource.Card.Owner(triggerContext.Model),
                    _ => Option.None<IPlayerEntity>()
                };

                var buffLibrary = triggerContext.Model.ContextManager.PlayerBuffLibrary;
                var resultBuff = new PlayerBuffEntity(
                    addBuffEffect.BuffId, 
                    Guid.NewGuid(), 
                    level,
                    caster,
                    buffLibrary.GetBuffProperties(addBuffEffect.BuffId)
                        .Select(p => p.CreateEntity(triggerContext)),
                    buffLibrary.GetBuffLifeTime(addBuffEffect.BuffId)
                        .CreateEntity(triggerContext),
                    buffLibrary.GetBuffSessions(addBuffEffect.BuffId)
                        .ToDictionary(
                            kvp => kvp.Key, 
                            kvp => kvp.Value.CreateEntity(triggerContext)));
                
                effectCommands.Add(new AddPlayerBuffEffectCommand(target, resultBuff));
            }
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveRemovePlayerBuffEffect(TriggerContext context, RemovePlayerBuffEffect removeBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new RemovePlayerBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = removeBuffEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var existBuffOpt = OptionCollectionExtensions.FirstOrNone(
                target.BuffManager.Buffs, 
                buff => buff.PlayerBuffDataId == removeBuffEffect.BuffId);
            existBuffOpt.MatchSome(existBuff =>
            {
                effectCommands.Add(new RemovePlayerBuffEffectCommand(target, existBuff));
            });
        }
        return new EffectCommandSet(effectCommands);
    }
    #endregion

    #region Card Effect Handlers
    private static EffectCommandSet _ResolveDrawCardEffect(TriggerContext context, DrawCardEffect drawCardEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new DrawCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var targets = drawCardEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var playerTarget = new PlayerTarget(target);
            var targetIntent = new DrawCardIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var drawCount = drawCardEffect.Value.Eval(targetTriggerContext);
            
            effectCommands.Add(new DrawCardEffectCommand(target, drawCount));
        }

        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveDiscardCardEffect(TriggerContext context, DiscardCardEffect discardCardEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new DiscardCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = discardCardEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            var destinationZone = card.IsConsumable() ?
                CardCollectionType.ExclusionZone :
                card.IsDisposable() ?
                    CardCollectionType.DisposeZone :
                    CardCollectionType.Graveyard;

            card.Owner(context.Model).MatchSome(cardOwner =>
            {
                cardOwner.CardManager.HandCard.GetCardOrNone(c => c.Identity == card.Identity)
                    .Map(handCard => CardCollectionType.HandCard)
                    .Else(cardOwner.CardManager.Deck.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(deckCard => CardCollectionType.Deck))
                    .MatchSome(cardStartZone =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            card,
                            cardStartZone,
                            destinationZone,
                            MoveCardType.Discard));
                    });
            });
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveConsumeCardEffect(TriggerContext context, ConsumeCardEffect consumeCardEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new ConsumeCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = consumeCardEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            var destinationZone = card.IsDisposable() ?
                CardCollectionType.DisposeZone :
                CardCollectionType.ExclusionZone;

            card.Owner(context.Model).MatchSome(cardOwner =>
            {
                cardOwner.CardManager.HandCard.GetCardOrNone(c => c.Identity == card.Identity)
                    .Map(handCard => CardCollectionType.HandCard)
                    .Else(cardOwner.CardManager.Deck.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(deckCard => CardCollectionType.Deck))
                    .Else(cardOwner.CardManager.Graveyard.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(graveCard => CardCollectionType.Graveyard))
                    .MatchSome(cardStartZone =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            card,
                            cardStartZone,
                            destinationZone,
                            MoveCardType.Consume));
                    });
            });
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveDisposeCardEffect(TriggerContext context, DisposeCardEffect disposeCardEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new DisposeCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = disposeCardEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            card.Owner(context.Model).MatchSome(cardOwner =>
            {
                cardOwner.CardManager.HandCard.GetCardOrNone(c => c.Identity == card.Identity)
                    .Map(handCard => CardCollectionType.HandCard)
                    .Else(cardOwner.CardManager.Deck.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(deckCard => CardCollectionType.Deck))
                    .Else(cardOwner.CardManager.Graveyard.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(graveCard => CardCollectionType.Graveyard)
                    .Else(cardOwner.CardManager.ExclusionZone.GetCardOrNone(card => card.Identity == card.Identity)
                        .Map(exclusionCard => CardCollectionType.ExclusionZone)))
                    .MatchSome(cardStartZone =>
                    {
                        effectCommands.Add(new MoveCardEffectCommand(
                            cardOwner,
                            card,
                            cardStartZone,
                            CardCollectionType.DisposeZone,
                            MoveCardType.Dispose));
                    });
            });
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveCreateCardEffect(TriggerContext context, CreateCardEffect createCardEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new CreateCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var target = createCardEffect.Target.Eval(triggerContext);
        
        target.MatchSome(targetPlayer =>
        {
            foreach (var cardDataId in createCardEffect.CardDataIds)
            {
                var playerTarget = new PlayerTarget(targetPlayer);
                var targetIntent = new CreateCardIntentTargetAction(context.Action.Source, playerTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };

                var newCard = CardEntity.RuntimeCreateFromId(cardDataId, context.Model.ContextManager.CardLibrary);
                var createCardCaster = triggerContext.Action switch
                {
                    CardPlaySource cardSource => cardSource.Card.Owner(triggerContext.Model),
                    PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                    _ => Option.None<IPlayerEntity>()
                };
                createCardEffect.AddCardBuffDatas
                    .Select(addCardBuffData => CardBuffEntity.CreateFromData(
                        addCardBuffData.CardBuffId,
                        addCardBuffData.Level.Eval(targetTriggerContext),
                        createCardCaster,
                        targetTriggerContext,
                        context.Model.ContextManager.CardBuffLibrary))
                    .ForEach(cardBuff => newCard.BuffManager.AddBuff(cardBuff));
                
                effectCommands.Add(new CreateCardEffectCommand(
                    targetPlayer,
                    newCard,
                    createCardEffect.CreateDestination));
            }
        });

        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveCloneCardEffect(TriggerContext context, CloneCardEffect cloneCardEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new CloneCardIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var target = cloneCardEffect.Target.Eval(triggerContext);
        
        target.MatchSome(targetPlayer =>
        {
            var playerTarget = new PlayerTarget(targetPlayer);
            var targetIntent = new CloneCardIntentTargetAction(context.Action.Source, playerTarget);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var cards = cloneCardEffect.ClonedCards.Eval(targetTriggerContext);
            
            foreach (var originCard in cards)
            {
                var playerCardTarget = new PlayerAndCardTarget(targetPlayer, originCard);
                targetIntent = new CloneCardIntentTargetAction(context.Action.Source, playerCardTarget);
                targetTriggerContext = targetTriggerContext with { Action = targetIntent };
                
                var cloneCard = originCard.Clone(includeCardBuffs: false, includeCardProperties: false);
                var cloneCardCaster = triggerContext.Action switch
                {
                    CardPlaySource cardSource => cardSource.Card.Owner(triggerContext.Model),
                    PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                    _ => Option.None<IPlayerEntity>()
                };
                cloneCardEffect.AddCardBuffDatas
                    .Select(addCardBuffData => CardBuffEntity.CreateFromData(
                        addCardBuffData.CardBuffId,
                        addCardBuffData.Level.Eval(targetTriggerContext),
                        cloneCardCaster,
                        targetTriggerContext,
                        context.Model.ContextManager.CardBuffLibrary))
                    .ForEach(cardBuff => cloneCard.BuffManager.AddBuff(cardBuff));
                
                effectCommands.Add(new CloneCardEffectCommand(
                    targetPlayer,
                    originCard,
                    cloneCard,
                    cloneCardEffect.CloneDestination));
            }
        });
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveAddCardBuffEffect(TriggerContext context, AddCardBuffEffect addCardBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new AddCardBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = addCardBuffEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            foreach (var addCardBuff in addCardBuffEffect.AddCardBuffDatas)
            {
                var cardTarget = new CardTarget(card);
                var targetIntent = new AddCardBuffIntentTargetAction(context.Action.Source, cardTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };
                var addLevel = addCardBuff.Level.Eval(targetTriggerContext);

                if(card.BuffManager.Buffs.Any(buff => buff.CardBuffDataID == addCardBuff.CardBuffId))
                {
                    effectCommands.Add(new ModifyCardBuffLevelEffectCommand(
                        card,
                        addCardBuff.CardBuffId,
                        addLevel));
                }
                else
                {
                    var caster = targetTriggerContext.Action switch
                    {
                        CardPlaySource cardSource => cardSource.Card.Owner(targetTriggerContext.Model),
                        PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                        _ => Option.None<IPlayerEntity>()
                    };

                    var newCardBuff = CardBuffEntity.CreateFromData(
                        addCardBuff.CardBuffId,
                        addLevel,
                        caster,
                        targetTriggerContext,
                        context.Model.ContextManager.CardBuffLibrary);

                    effectCommands.Add(new AddCardBuffEffectCommand(card, newCardBuff));
                }
            }
        }
        return new EffectCommandSet(effectCommands);
    }
    private static EffectCommandSet _ResolveRemoveCardBuffEffect(TriggerContext context, RemoveCardBuffEffect removeCardBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new RemoveCardBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = removeCardBuffEffect.TargetCards.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            var existBuffOpt = OptionCollectionExtensions.FirstOrNone(
                card.BuffManager.Buffs, 
                buff => buff.CardBuffDataID == removeCardBuffEffect.BuffId);
            existBuffOpt.MatchSome(existBuff =>
            {
                effectCommands.Add(new RemoveCardBuffEffectCommand(card, existBuff));
            });
        }
        return new EffectCommandSet(effectCommands);
    }
    #endregion
    #endregion

    #region PlayBuffEffect
    public static EffectCommandSet ResolvePlayerBuffEffect(
        TriggerContext context,
        IPlayerBuffEffect buffEffect)
    {
        var resolveCommands = buffEffect switch
        {
            AdditionalDamagePlayerBuffEffect additionalDamageBuffEffect =>
                _ResolveAdditionalDamagePlayerBuffEffect(context, additionalDamageBuffEffect),
            
            EffectiveDamagePlayerBuffEffect effectiveDamageBuffEffect =>
                _ResolveEffectiveDamagePlayerBuffEffect(context, effectiveDamageBuffEffect),
            
            AddCardBuffPlayerBuffEffect addCardBuffPlayerBuffEffect =>
                _ResolveAddCardBuffPlayerBuffEffect(context, addCardBuffPlayerBuffEffect),
            RemoveCardBuffPlayerBuffEffect removeCardBuffPlayerBuffEffect =>

                _ResolveRemoveCardBuffPlayerBuffEffect(context, removeCardBuffPlayerBuffEffect),
            
            CardPlayEffectAttributeAdditionPlayerBuffEffect cardPlayEffectAttributeBuffEffect =>
                _ResolveCardPlayEffectAttributeAdditionPlayerBuffEffect(context, cardPlayEffectAttributeBuffEffect),

            _ => new EffectCommandSet(Array.Empty<IEffectCommand>())
        };

        return new EffectCommandSet(resolveCommands.Commands);
    }

    private static EffectCommandSet _ResolveAdditionalDamagePlayerBuffEffect(
        TriggerContext context,
        AdditionalDamagePlayerBuffEffect additionalDamageBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new DamageIntentAction(context.Action.Source, DamageType.Additional);
        var triggerContext = context with { Action = intent };
        var targets = additionalDamageBuffEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new DamageIntentTargetAction(context.Action.Source, characterTarget, DamageType.Additional);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var damagePoint = additionalDamageBuffEffect.Value.Eval(targetTriggerContext);
            var damageFormulaPoint = GameFormula.AdditionalDamagePoint(targetTriggerContext, damagePoint);

            effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, DamageType.Additional));
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveEffectiveDamagePlayerBuffEffect(
        TriggerContext context,
        EffectiveDamagePlayerBuffEffect effectiveDamageBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new DamageIntentAction(context.Action.Source, DamageType.Effective);
        var triggerContext = context with { Action = intent };
        var targets = effectiveDamageBuffEffect.Targets.Eval(triggerContext);

        foreach (var target in targets)
        {
            var characterTarget = new CharacterTarget(target);
            var targetIntent = new DamageIntentTargetAction(context.Action.Source, characterTarget, DamageType.Effective);
            var targetTriggerContext = triggerContext with { Action = targetIntent };
            var damagePoint = effectiveDamageBuffEffect.Value.Eval(targetTriggerContext);
            var damageFormulaPoint = GameFormula.EffectiveDamagePoint(targetTriggerContext, damagePoint);

            effectCommands.Add(new DamageEffectCommand(target, damageFormulaPoint, DamageType.Effective));
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveAddCardBuffPlayerBuffEffect(
        TriggerContext context,
        AddCardBuffPlayerBuffEffect addCardBuffPlayerBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new AddCardBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = addCardBuffPlayerBuffEffect.Targets.Eval(triggerContext);

        foreach (var card in cards)
        {
            foreach (var addCardBuff in addCardBuffPlayerBuffEffect.AddCardBuffDatas)
            {
                var cardTarget = new CardTarget(card);
                var targetIntent = new AddCardBuffIntentTargetAction(context.Action.Source, cardTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };
                var addLevel = addCardBuff.Level.Eval(targetTriggerContext);
                
                if(card.BuffManager.Buffs.Any(buff => buff.CardBuffDataID == addCardBuff.CardBuffId))
                {
                    effectCommands.Add(new ModifyCardBuffLevelEffectCommand(
                        card,
                        addCardBuff.CardBuffId,
                        addLevel));
                }
                else
                {
                    var caster = targetTriggerContext.Action switch
                    {
                        CardPlaySource cardSource => cardSource.Card.Owner(targetTriggerContext.Model),
                        PlayerBuffSource playerBuffSource => playerBuffSource.Buff.Caster,
                        _ => Option.None<IPlayerEntity>()
                    };

                    var newCardBuff = CardBuffEntity.CreateFromData(
                        addCardBuff.CardBuffId,
                        addLevel,
                        caster,
                        targetTriggerContext,
                        context.Model.ContextManager.CardBuffLibrary);

                    effectCommands.Add(new AddCardBuffEffectCommand(card, newCardBuff));
                }
            }
        }
        return new EffectCommandSet(effectCommands);
    }
    private static EffectCommandSet _ResolveRemoveCardBuffPlayerBuffEffect(
        TriggerContext context,
        RemoveCardBuffPlayerBuffEffect removeCardBuffPlayerBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();
        var intent = new RemoveCardBuffIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var cards = removeCardBuffPlayerBuffEffect.Targets.Eval(triggerContext).ToList();

        foreach (var card in cards)
        {
            var existBuffOpt = OptionCollectionExtensions.FirstOrNone(
                card.BuffManager.Buffs, 
                buff => buff.CardBuffDataID == removeCardBuffPlayerBuffEffect.BuffId);
            existBuffOpt.MatchSome(existBuff =>
            {
                effectCommands.Add(new RemoveCardBuffEffectCommand(card, existBuff));
            });
        }
        return new EffectCommandSet(effectCommands);
    }

    private static EffectCommandSet _ResolveCardPlayEffectAttributeAdditionPlayerBuffEffect(
        TriggerContext context,
        CardPlayEffectAttributeAdditionPlayerBuffEffect cardPlayEffectAttributeBuffEffect)
    {
        var effectCommands = new List<IEffectCommand>();        
        var intent = new CardPlayEffectAttributeIntentAction(context.Action.Source);
        var triggerContext = context with { Action = intent };
        var value = cardPlayEffectAttributeBuffEffect.Value.Eval(triggerContext);

        effectCommands.Add(new ModifyCardAttributeEffectCommand(
            cardPlayEffectAttributeBuffEffect.Type,
            value));

        return new EffectCommandSet(effectCommands);
    }
    #endregion
}
