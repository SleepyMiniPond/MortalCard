namespace MortalGame.GameModel
{

    public interface IEffectCommandHandler
    {
        CommandApplyResult Handle(TriggerContext context, IEffectCommand command);
    }

}
