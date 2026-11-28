namespace MortalGame.Editor
{
    public abstract partial record ValidationIssue
    {
        public sealed record DuplicateReactionTiming(string Context, string Key, MortalGame.GameData.GameTiming Timing) : ValidationIssue
        {
            public override string Message => $"{Context}[{Key}] 的 TimingRule 重複：{Timing}";
        }

        public sealed record MissingCardData(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CardData 為空";
        }

        public sealed record MissingSelfTransformTarget(string Context, string CardDataId, string TargetCardDataId) : ValidationIssue
        {
            public override string Message => $"{Context} / CardData[{CardDataId}] 的 Self Transform Target " +
                            $"指向不存在的 CardData：{TargetCardDataId}";
        }

        public sealed record WrongSelfTransformTargetType(string Context, string CardDataId, string TargetCardDataId) : ValidationIssue
        {
            public override string Message => $"{Context} / CardData[{CardDataId}] 的 Self Transform Target " +
                            $"必須是 Standard CardData：{TargetCardDataId}";
        }

        public sealed record MissingOverrideTargets(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 TargetCards 為空";
        }

        public sealed record MissingOverrideKey(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 OverrideKey 為空";
        }

        public sealed record MissingOverrideTargetId(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 TargetCardDataId 為空";
        }

        public sealed record MissingOverrideTarget(string Context, string TargetCardDataId) : ValidationIssue
        {
            public override string Message => $"{Context} 指向不存在的 Override CardData：{TargetCardDataId}";
        }

        public sealed record WrongOverrideTargetType(string Context, string TargetCardDataId) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Target 必須是 Override CardData：{TargetCardDataId}";
        }

        public sealed record MissingOverrideReleaseRules(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 ReleaseRules 不可為空";
        }

        public sealed record MissingRequiredValue(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 為空";
        }

        public sealed record MissingTiming(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Timing 不可為 None";
        }

        public sealed record UndispatchedTiming(string Context, MortalGame.GameData.GameTiming Timing) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Timing 不會進入 Timing Dispatch：{Timing}";
        }

        public sealed record MissingOverrideSessions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 ReactionSessions 為空";
        }

        public sealed record EmptyOverrideSessionKey(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 ReactionSession Key 為空";
        }

        public sealed record MissingOverrideSessionData(string Context, string Key) : ValidationIssue
        {
            public override string Message => $"{Context}.ReactionSessions[{Key}] 的資料為空";
        }

        public sealed record NullConditions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Conditions 含有空值";
        }

        public sealed record MissingOverrideConditionSessionKey(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Override SessionKey 為空";
        }

        public sealed record UnknownOverrideConditionSession(string Context, string SessionKey) : ValidationIssue
        {
            public override string Message => $"{Context} 引用不存在的 Override SessionKey：{SessionKey}";
        }

        public sealed record NullSessionConditions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Session Conditions 含有空值";
        }

        public sealed record NullTransformRule(string Context, string CardDataId) : ValidationIssue
        {
            public override string Message => $"{Context} / CardData[{CardDataId}].TransformRules 含有空規則";
        }

        public sealed record MultipleTransformKeys(string Context, string CardDataId) : ValidationIssue
        {
            public override string Message => $"{Context} / CardData[{CardDataId}].TransformRules 第一版只能使用一個 TransformKey";
        }

        public sealed record DuplicateTransformRuleId(string Context, string CardDataId, string RuleId) : ValidationIssue
        {
            public override string Message => $"{Context} / CardData[{CardDataId}].TransformRules 的 RuleId 重複：{RuleId}";
        }

        public sealed record MissingTransformRuleId(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 RuleId 為空";
        }

        public sealed record MissingTransformKey(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 TransformKey 為空";
        }

        public sealed record MissingTransformOperation(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Operation 為空";
        }

        public sealed record MissingApplyTargetId(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 Apply 缺少 TargetCardDataId";
        }

        public sealed record UnknownBuffQueryId(string AssetPath, string BuffType, string QueryId) : ValidationIssue
        {
            public override string Message => $"{AssetPath} 的 {BuffType} 查詢引用不存在的 Buff ID：{QueryId}";
        }

        public sealed record MissingPlayerBuffData(string AssetPath) : ValidationIssue
        {
            public override string Message => $"{AssetPath} 的 PlayerBuffData 為空";
        }

        public sealed record MissingPlayerBuffResolver(string AssetPath, string BuffId, MortalGame.GameData.GameTiming Key, System.Type ActualType) : ValidationIssue
        {
            public override string Message => $"{AssetPath} / PlayerBuffData[{BuffId}].BuffEffects[{Key}] 的 {ActualType.Name} 缺少 IPlayerBuffEffectResolver 註冊";
        }

        public sealed record MissingCharacterBuffData(string AssetPath) : ValidationIssue
        {
            public override string Message => $"{AssetPath} 的 CharacterBuffData 為空";
        }

        public sealed record MissingCharacterBuffResolver(string AssetPath, string BuffId, MortalGame.GameData.GameTiming Key, System.Type ActualType) : ValidationIssue
        {
            public override string Message => $"{AssetPath} / CharacterBuffData[{BuffId}].BuffEffects[{Key}] 的 {ActualType.Name} 缺少 ICharacterBuffEffectResolver 註冊";
        }

        public sealed record MissingCardBuffData(string AssetPath) : ValidationIssue
        {
            public override string Message => $"{AssetPath} 的 CardBuffData 為空";
        }

        public sealed record MissingCardResolver(string Context, System.Type ActualType) : ValidationIssue
        {
            public override string Message => $"{Context} 的 {ActualType.Name} 缺少 ICardEffectResolver 註冊";
        }

        public sealed record MissingCardBuffResolver(string Context, System.Type ActualType) : ValidationIssue
        {
            public override string Message => $"{Context} 的 {ActualType.Name} 缺少 ICardBuffEffectResolver 註冊";
        }

        public sealed record InvalidDamageType(string Context, MortalGame.GameData.DamageType Type) : ValidationIssue
        {
            public override string Message => $"{Context} 的 DamageEffect.Type 無效：{Type}";
        }

        public sealed record NullAddedCardBuff(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 含有空 AddCardBuffData";
        }

        public sealed record UnknownReferenceId(string Context, string Id) : ValidationIssue
        {
            public override string Message => $"{Context} 指向不存在的 ID：{Id}";
        }

        public sealed record UnknownCompositionTemplate(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CreateComposedCardEffect.TemplateSelectionId 找不到模板群組";
        }

        public sealed record EmptySubSelectionId(string Context, int Index) : ValidationIssue
        {
            public override string Message => $"{Context}.SubSelects[{Index}].Id 為空";
        }

        public sealed record DuplicateSubSelectionId(string Context, string DuplicateId) : ValidationIssue
        {
            public override string Message => $"{Context}.SubSelects 的 Id 重複：{DuplicateId}";
        }

        public sealed record InvalidSelectionCountRule(string Context, MortalGame.GameModel.CardSelectionCountRule CountRule) : ValidationIssue
        {
            public override string Message => $"{Context}.CountRule 無效：{CountRule}";
        }

        public sealed record NegativeSelectionCount(string Context, int Count) : ValidationIssue
        {
            public override string Message => $"{Context}.SelectCount 不得為負數：{Count}";
        }

        public sealed record InvalidPriorSelectionReference(string Context, string SelectionId) : ValidationIssue
        {
            public override string Message => $"{Context} 的 SubSelectedCardCollection.SelectionId 必須引用前面的 ExistCard 群組：{SelectionId}";
        }

        public sealed record UnknownExistCardSelection(string Context, string SelectionId) : ValidationIssue
        {
            public override string Message => $"{Context} 的 SubSelectedCardCollection.SelectionId 找不到 ExistCard 群組：{SelectionId}";
        }

        public sealed record NullTriggeredEffect(string Context, MortalGame.GameData.CardTriggeredTiming Key, int Index) : ValidationIssue
        {
            public override string Message => $"{Context}.TriggeredEffects[{Key}][{Index}] 為空";
        }

        public sealed record InvalidCardSetOperation(string Context, MortalGame.GameData.SetOperationType Operation) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CombinedCardCollection.Operation 無效：{Operation}";
        }

        public sealed record MissingSubSelectionId(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 SubSelectedCardCollection.SelectionId 為空";
        }

        public sealed record InvalidCardZone(string Context, MortalGame.GameData.CardCollectionType Zone) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CardsOfPlayer.Zone 必須是有效的一般卡片區域：{Zone}";
        }

        public sealed record EmptyFilteredCardConditions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 FilteredCardCollection.Conditions 至少需要一項";
        }

        public sealed record EmptyAnyCardConditions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CardCollectionAnyCondition.Conditions 至少需要一項";
        }

        public sealed record EmptyAllCardConditions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CardCollectionAllCondition.Conditions 至少需要一項";
        }

        public sealed record MissingComposedCardTarget(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CreateComposedCardEffect.Target 為空";
        }

        public sealed record InvalidComposedCardDestination(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CreateComposedCardEffect.CreateDestination 必須是有效的一般卡片區域";
        }

        public sealed record MissingCreateCardTarget(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CreateCardEffect.Target 為空";
        }

        public sealed record InvalidCreateCardDestination(string Context, MortalGame.GameData.CardCollectionType CreateDestination) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CreateCardEffect.CreateDestination 必須是有效的一般卡片區域：{CreateDestination}";
        }

        public sealed record EmptyCreatedCardIds(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CreateCardEffect.CardDataIds 至少需要一項";
        }

        public sealed record MissingCloneCardTarget(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CloneCardEffect.Target 為空";
        }

        public sealed record MissingClonedCards(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CloneCardEffect.ClonedCards 為空";
        }

        public sealed record InvalidCloneCardDestination(string Context, MortalGame.GameData.CardCollectionType CloneDestination) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CloneCardEffect.CloneDestination 必須是有效的一般卡片區域：{CloneDestination}";
        }
    }
}
