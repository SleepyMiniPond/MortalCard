using MortalGame.GameData;
namespace MortalGame.GameModel
{

    public interface ICardBuffEffectResolver
    {
        EffectCommandSet Resolve(TriggerContext context, ICardBuffEffect effect);
    }

}
