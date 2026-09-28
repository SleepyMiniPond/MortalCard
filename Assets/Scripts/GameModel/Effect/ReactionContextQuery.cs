using Optional;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 提供 Reaction 觸發上下文的共用唯讀查詢。
    /// </summary>
    public static class ReactionContextQuery
    {
        /// <summary>
        /// 取得 Reaction 所屬的玩家；無法取得時維持 None。
        /// </summary>
        public static Option<IPlayerEntity> Owner(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                ICardTriggeredSource cardSource =>
                    cardSource.Card.Owner(triggerContext.Model),
                ICharacterTriggeredSource characterSource =>
                    characterSource.Character.Owner(triggerContext.Model),
                IPlayerTriggeredSource playerSource =>
                    playerSource.Player.SomeNotNull(),
                _ => Option.None<IPlayerEntity>()
            };
        }

        /// <summary>
        /// 取得 Reaction 的施放者；無法追溯時維持 None。
        /// </summary>
        public static Option<IPlayerEntity> Caster(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                PlayerBuffTrigger playerBuffTrigger => playerBuffTrigger.Buff.Caster,
                CharacterBuffTrigger characterBuffTrigger => characterBuffTrigger.Buff.Caster,
                CardBuffTrigger cardBuffTrigger => cardBuffTrigger.Buff.Caster,
                CardTrigger when triggerContext.Action.Source is InvokeCardEffectsSource invokeSource =>
                    invokeSource.Caster.SomeNotNull(),
                ICardTriggeredSource cardSource =>
                    cardSource.Card.Owner(triggerContext.Model),
                _ => Option.None<IPlayerEntity>()
            };
        }

        /// <summary>取得本次 Invoke 的施放者；Buff 發起時使用宿主，直接連鎖則沿用外層施放者。</summary>
        public static Option<IPlayerEntity> InvokeCaster(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                PlayerBuffTrigger playerBuffTrigger => playerBuffTrigger.Player.SomeNotNull(),
                CharacterBuffTrigger characterBuffTrigger =>
                    characterBuffTrigger.Character.Owner(triggerContext.Model),
                CardBuffTrigger cardBuffTrigger => cardBuffTrigger.Card.Owner(triggerContext.Model),
                CardTrigger when triggerContext.Action.Source is InvokeCardEffectsSource invokeSource =>
                    invokeSource.Caster.SomeNotNull(),
                _ => Owner(triggerContext)
            };
        }
    }
}
