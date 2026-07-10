using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.GameModel
{

public interface ICharacterBuffEffectResolver
{
    EffectCommandSet Resolve(TriggerContext context, ICharacterBuffEffect effect);
}

}
