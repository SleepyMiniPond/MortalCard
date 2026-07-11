using MortalGame.GameData;
namespace MortalGame.GameModel
{

    public interface IPlayerBuffEffectResolver
    {
        EffectCommandSet Resolve(TriggerContext context, IPlayerBuffEffect effect);
    }

}
