using System;

namespace MortalGame.GameModel
{
    public interface ICharacterBuffValueCondition
    {
        bool Eval(TriggerContext triggerContext, ICharacterBuffEntity characterBuff);
    }

    [Serializable]
    public class CharacterBuffDataIdCondition : ICharacterBuffValueCondition
    {
        public string BuffId;

        public bool Eval(TriggerContext triggerContext, ICharacterBuffEntity characterBuff)
        {
            return characterBuff.CharacterBuffDataId == BuffId;
        }
    }
}
