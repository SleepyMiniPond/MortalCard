using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.GameModel
{

public interface ICardBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICardBuffEffect effect);
}

}
