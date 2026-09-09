using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
namespace MortalGame.GameModel
{

    public class AddPlayerBuffEffectResolver :
        ICardEffectResolver,
        IPlayerBuffEffectResolver,
        ICharacterBuffEffectResolver,
        ICardBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet IPlayerBuffEffectResolver.Resolve(
            TriggerContext context,
            IPlayerBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet ICharacterBuffEffectResolver.Resolve(
            TriggerContext context,
            ICharacterBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        EffectCommandSet ICardBuffEffectResolver.Resolve(
            TriggerContext context,
            ICardBuffEffect effect)
        {
            return _ResolveCore(context, effect);
        }

        private static EffectCommandSet _ResolveCore(
            TriggerContext context,
            object effect)
        {
            if (effect is not AddPlayerBuffEffect addBuffEffect)
                throw new InvalidOperationException($"AddPlayerBuffEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            var effectCommands = new List<IEffectCommand>();
            var intent = new AddPlayerBuffIntentAction(context.Action.Source);
            var triggerContext = context with { Action = intent };
            var targets = addBuffEffect.Targets.Eval(triggerContext);

            foreach (var target in targets)
            {
                var playerTarget = new PlayerTarget(target);
                var targetIntent = new AddPlayerBuffIntentTargetAction(context.Action.Source, playerTarget);
                var targetTriggerContext = triggerContext with { Action = targetIntent };
                if (!addBuffEffect.Level.Eval(targetTriggerContext).TryGetValue(out var level) ||
                    level < 0)
                {
                    continue;
                }

                if (target.BuffManager.Buffs.Any(buff => buff.PlayerBuffDataId == addBuffEffect.BuffId))
                {
                    effectCommands.Add(new ModifyPlayerBuffLevelEffectCommand(
                        target,
                        addBuffEffect.BuffId,
                        level));
                }
                else
                {
                    var caster = ReactionContextQuery.Caster(targetTriggerContext);

                    var buffLibrary = triggerContext.Model.ContextManager.PlayerBuffLibrary;
                    var lifeTime = context.Model.ContextManager.PlayerBuffLifeTimeEntityFactory.Create(
                        buffLibrary.GetBuffLifeTime(addBuffEffect.BuffId),
                        triggerContext);
                    if (!lifeTime.TryGetValue(out var lifeTimeEntity))
                    {
                        continue;
                    }

                    var resultBuff = new PlayerBuffEntity(
                        addBuffEffect.BuffId,
                        Guid.NewGuid(),
                        level,
                        buffLibrary.GetBuffMaxLevel(addBuffEffect.BuffId),
                        caster,
                        buffLibrary.GetBuffProperties(addBuffEffect.BuffId)
                            .Select(context.Model.ContextManager.PlayerBuffPropertyEntityFactory.Create),
                        lifeTimeEntity,
                        buffLibrary.GetBuffSessions(addBuffEffect.BuffId)
                            .ToDictionary(
                                kvp => kvp.Key,
                                kvp => context.Model.ContextManager.ReactionSessionEntityFactory.Create(kvp.Value)));

                    effectCommands.Add(new AddPlayerBuffEffectCommand(target, resultBuff));
                }
            }
            return new EffectCommandSet(effectCommands);
        }
    }

}
