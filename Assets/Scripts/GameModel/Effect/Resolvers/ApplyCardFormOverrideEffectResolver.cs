using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public sealed class ApplyCardFormOverrideEffectResolver : ICardEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            if (effect is not ApplyCardFormOverrideEffect applyEffect)
            {
                throw new InvalidOperationException(
                    $"ApplyCardFormOverrideEffectResolver 不支援的效果類型：{effect.GetType().Name}");
            }

            var intent = new ApplyCardFormOverrideIntentAction(context.Action.Source);
            var intentContext = context with { Action = intent };
            var commands = new List<IEffectCommand>();

            foreach (var card in applyEffect.TargetCards.Eval(intentContext))
            {
                commands.Add(new ApplyCardFormOverrideEffectCommand(
                    card,
                    applyEffect.OverrideKey,
                    applyEffect.TargetCardDataId,
                    applyEffect.ReleaseRules.ToList(),
                    applyEffect.ReactionSessions.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value)));
            }

            return new EffectCommandSet(commands);
        }
    }
}
