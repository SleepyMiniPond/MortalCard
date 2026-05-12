public interface ICharacterBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICharacterBuffEffect effect);
}
