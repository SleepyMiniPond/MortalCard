using System;
using System.Collections.Generic;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public class CreateCardEffectResolver :
        ICardEffectResolver,
        IPlayerBuffEffectResolver,
        ICharacterBuffEffectResolver,
        ICardBuffEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
            => _ResolveCore(context, effect);

        EffectCommandSet IPlayerBuffEffectResolver.Resolve(
            TriggerContext context,
            IPlayerBuffEffect effect)
            => _ResolveCore(context, effect);

        EffectCommandSet ICharacterBuffEffectResolver.Resolve(
            TriggerContext context,
            ICharacterBuffEffect effect)
            => _ResolveCore(context, effect);

        EffectCommandSet ICardBuffEffectResolver.Resolve(
            TriggerContext context,
            ICardBuffEffect effect)
            => _ResolveCore(context, effect);

        private static EffectCommandSet _ResolveCore(TriggerContext context, object effect)
        {
            if (effect is not CreateCardEffect createCardEffect)
                throw new InvalidOperationException(
                    $"CreateCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            if (createCardEffect.Target == null ||
                !createCardEffect.CreateDestination.IsNormalCardZone())
            {
                return EffectCommandSet.Empty;
            }

            var effectCommands = new List<IEffectCommand>();
            var triggerContext = context with
            {
                Action = new CreateCardIntentAction(context.Action.Source)
            };
            var target = createCardEffect.Target.Eval(triggerContext);

            target.MatchSome(targetPlayer =>
            {
                foreach (var cardDataId in createCardEffect.CardDataIds ?? new List<string>())
                {
                    if (string.IsNullOrWhiteSpace(cardDataId))
                        continue;

                    var cardData = context.Model.ContextManager.CardLibrary.GetCardData(cardDataId);
                    if (cardData == null)
                        continue;

                    var playerTarget = new PlayerTarget(targetPlayer);
                    var targetTriggerContext = triggerContext with
                    {
                        Action = new CreateCardIntentTargetAction(
                            context.Action.Source,
                            playerTarget)
                    };
                    var newCard = CardEntity.RuntimeCreateFromId(
                        cardDataId,
                        context.Model.ContextManager.CardLibrary,
                        context.Model.ContextManager.CardPropertyEntityFactory);
                    var createCardCaster = ReactionContextQuery.Caster(targetTriggerContext);

                    foreach (var addCardBuffData in createCardEffect.AddCardBuffDatas ?? new List<AddCardBuffData>())
                    {
                        if (addCardBuffData?.Level == null ||
                            !addCardBuffData.Level
                                .Eval(targetTriggerContext)
                                .TryGetValue(out var level) ||
                            level < 0)
                        {
                            continue;
                        }

                        var cardBuff = CardBuffEntity.CreateFromData(
                            addCardBuffData.CardBuffId,
                            level,
                            createCardCaster,
                            targetTriggerContext,
                            context.Model.ContextManager.CardBuffLibrary,
                            context.Model.ContextManager.CardBuffPropertyEntityFactory,
                            context.Model.ContextManager.CardBuffLifeTimeEntityFactory,
                            context.Model.ContextManager.ReactionSessionEntityFactory);
                        cardBuff.MatchSome(buff => newCard.BuffManager.AddBuff(buff));
                    }

                    effectCommands.Add(new CreateCardEffectCommand(
                        targetPlayer,
                        newCard,
                        createCardEffect.CreateDestination));
                }
            });

            return new EffectCommandSet(effectCommands);
        }
    }
}
