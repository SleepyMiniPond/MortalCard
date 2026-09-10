using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public class CloneCardEffectResolver :
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
            if (effect is not CloneCardEffect cloneCardEffect)
                throw new InvalidOperationException(
                    $"CloneCardEffectResolver 不支援的效果類型：{effect.GetType().Name}");

            if (cloneCardEffect.Target == null ||
                cloneCardEffect.ClonedCards == null ||
                !cloneCardEffect.CloneDestination.IsNormalCardZone())
            {
                return EffectCommandSet.Empty;
            }

            var effectCommands = new List<IEffectCommand>();
            var triggerContext = context with
            {
                Action = new CloneCardIntentAction(context.Action.Source)
            };
            var target = cloneCardEffect.Target.Eval(triggerContext);

            target.MatchSome(targetPlayer =>
            {
                var targetTriggerContext = triggerContext with
                {
                    Action = new CloneCardIntentTargetAction(
                        context.Action.Source,
                        new PlayerTarget(targetPlayer))
                };
                var cards = cloneCardEffect.ClonedCards
                    .Eval(targetTriggerContext)
                    .GroupBy(card => card.Identity)
                    .Select(group => group.First());

                foreach (var originCard in cards)
                {
                    // PlayingCard 不屬於可複製的普通牌區；前項移牌後也會在此安全略過。
                    originCard.Owner(context.Model).MatchSome(originOwner =>
                    originOwner.CardManager.GetCardAndZoneOrNone(
                        originCard,
                        new[]
                        {
                            CardCollectionType.HandCard,
                            CardCollectionType.Deck,
                            CardCollectionType.Graveyard,
                            CardCollectionType.ExclusionZone,
                            CardCollectionType.DisposeZone
                        }).MatchSome(found =>
                    {
                        var playerCardTarget = new PlayerAndCardTarget(targetPlayer, found.Card);
                        var cardTargetContext = targetTriggerContext with
                        {
                            Action = new CloneCardIntentTargetAction(
                                context.Action.Source,
                                playerCardTarget)
                        };
                        var cloneCard = found.Card.Clone();
                        var cloneCardCaster = ReactionContextQuery.Caster(cardTargetContext);

                        foreach (var addCardBuffData in cloneCardEffect.AddCardBuffDatas ?? new List<AddCardBuffData>())
                        {
                            if (addCardBuffData?.Level == null ||
                                !addCardBuffData.Level
                                    .Eval(cardTargetContext)
                                    .TryGetValue(out var level) ||
                                level < 0)
                            {
                                continue;
                            }

                            var cardBuff = CardBuffEntity.CreateFromData(
                                addCardBuffData.CardBuffId,
                                level,
                                cloneCardCaster,
                                cardTargetContext,
                                context.Model.ContextManager.CardBuffLibrary,
                                context.Model.ContextManager.CardBuffPropertyEntityFactory,
                                context.Model.ContextManager.CardBuffLifeTimeEntityFactory,
                                context.Model.ContextManager.ReactionSessionEntityFactory);
                            cardBuff.MatchSome(buff => cloneCard.BuffManager.AddBuff(buff));
                        }

                        effectCommands.Add(new CloneCardEffectCommand(
                            targetPlayer,
                            found.Card,
                            cloneCard,
                            cloneCardEffect.CloneDestination));
                    }));
                }
            });

            return new EffectCommandSet(effectCommands);
        }
    }
}
