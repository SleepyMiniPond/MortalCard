using MortalGame.GameData;
namespace MortalGame.GameModel
{

    public interface ICardEffectResolver
    {
        EffectCommandSet Resolve(TriggerContext context, ICardEffect effect);
    }

}
