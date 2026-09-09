using Optional;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 依 Reaction 的觸發來源取得施放者；無法追溯時維持 None。
    /// </summary>
    public static class ReactionCasterResolver
    {
        public static Option<IPlayerEntity> Resolve(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                PlayerBuffTrigger playerBuffTrigger => playerBuffTrigger.Buff.Caster,
                CharacterBuffTrigger characterBuffTrigger => characterBuffTrigger.Buff.Caster,
                CardBuffTrigger cardBuffTrigger => cardBuffTrigger.Buff.Caster,
                ICardTriggeredSource cardSource =>
                    cardSource.Card.Owner(triggerContext.Model),
                _ => Option.None<IPlayerEntity>()
            };
        }
    }
}
