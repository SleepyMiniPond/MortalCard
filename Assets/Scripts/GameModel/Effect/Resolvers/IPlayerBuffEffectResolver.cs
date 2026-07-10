using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.GameModel
{

public interface IPlayerBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, IPlayerBuffEffect effect);
}

}
