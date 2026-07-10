using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.GameModel
{

public interface ICardEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICardEffect effect);
}

}
