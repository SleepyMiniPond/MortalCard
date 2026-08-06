using Optional;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 負責將戰鬥中的卡片形態狀態安全寫回原始 CardInstance。
    /// </summary>
    public static class CardInstancePersistenceMapper
    {
        public static Option<CardInstance> TryUpdate(
            ICardEntity card,
            CardInstance cardInstance)
        {
            if (!card.OriginCardInstanceGuid.TryGetValue(out var originCardInstanceGuid) ||
                originCardInstanceGuid != cardInstance.InstanceGuid)
            {
                return Option.None<CardInstance>();
            }

            return (cardInstance with
            {
                PersistentFormState = _ToPersistentFormState(card)
            }).Some();
        }

        private static Option<PersistentCardFormState> _ToPersistentFormState(ICardEntity card)
        {
            if (!card.SelfFormState.TryGetValue(out var selfFormState) ||
                selfFormState.Persistence != CardFormPersistence.Persistent)
            {
                return Option.None<PersistentCardFormState>();
            }

            return new PersistentCardFormState(
                selfFormState.TransformKey,
                selfFormState.CardDataId).Some();
        }
    }
}
