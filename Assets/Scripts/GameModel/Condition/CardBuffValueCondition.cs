using System;

namespace MortalGame.GameModel
{
    public interface ICardBuffValueCondition
    {
        bool Eval(TriggerContext triggerContext, ICardBuffEntity cardBuff);
    }

    [Serializable]
    public class CardBuffDataIdCondition : ICardBuffValueCondition
    {
        public string BuffId;

        public bool Eval(TriggerContext triggerContext, ICardBuffEntity cardBuff)
        {
            return cardBuff.CardBuffDataID == BuffId;
        }
    }
}
