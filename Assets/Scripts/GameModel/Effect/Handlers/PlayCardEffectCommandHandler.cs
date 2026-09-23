namespace MortalGame.GameModel
{
    public sealed class PlayCardEffectCommandHandler : IEffectCommandHandler
    {
        public CommandApplyResult Handle(TriggerContext context, IEffectCommand command, IEffectQueueContext queue)
        {
            var playCardCommand = (PlayCardEffectCommand)command;
            context.Model.EnqueueCardPlay(playCardCommand.Request);
            return CommandApplyResult.Empty;
        }
    }
}
