public interface ICardBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICardBuffEffect effect);
}
