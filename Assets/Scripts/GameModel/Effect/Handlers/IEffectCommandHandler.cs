public interface IEffectCommandHandler
{
    CommandApplyResult Handle(TriggerContext context, IEffectCommand command);
}
