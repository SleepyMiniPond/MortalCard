public interface ICardEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICardEffect effect);
}
