public interface IPlayerBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, IPlayerBuffEffect effect);
}
