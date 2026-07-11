using MortalGame.GameData;
namespace MortalGame.GameModel
{

    public interface ICharacterBuffEffectResolver
    {
        EffectCommandSet Resolve(TriggerContext context, ICharacterBuffEffect effect);
    }

}
