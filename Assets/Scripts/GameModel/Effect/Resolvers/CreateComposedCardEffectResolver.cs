using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public sealed class CreateComposedCardEffectResolver : ICardEffectResolver
    {
        public EffectCommandSet Resolve(TriggerContext context, ICardEffect effect)
        {
            var create = (CreateComposedCardEffect)effect;
            var selections = context.Model.ContextManager.Context;
            if (!create.CreateDestination.IsNormalCardZone() ||
                !selections.ComposedCardDefinitions.TryGetValue(create.TemplateSelectionId, out var selected) ||
                context.Triggered is not ICardTriggeredSource source || source.Card.Identity != selected.CardIdentity ||
                !create.Target.Eval(context with { Action = new CreateCardIntentAction(context.Action.Source) })
                    .TryGetValue(out var target)) return EffectCommandSet.Empty;
            var manager = context.Model.ContextManager;
            var cardEntity = CardEntity.RuntimeCreateFromDefinition(selected.Definition, manager.CardLibrary, manager.CardPropertyEntityFactory);
            return new EffectCommandSet(new IEffectCommand[]
            {
                new CreateCardEffectCommand(target, cardEntity, create.CreateDestination)
            });
        }
    }
}
