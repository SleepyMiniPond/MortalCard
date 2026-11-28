namespace MortalGame.Editor
{
    public abstract partial record ValidationIssue
    {
        public sealed record InvalidSetCondition(string Context, string ConditionName, MortalGame.GameData.SetConditionType SetCondition) : ValidationIssue
        {
            public override string Message => $"{Context} 的 {ConditionName}.Condition 無效：{SetCondition}";
        }

        public sealed record EmptyComparisonValues(string Context, string ConditionName) : ValidationIssue
        {
            public override string Message => $"{Context} 的 {ConditionName} 比較值至少需要一項";
        }

        public sealed record InvalidComparisonValue<T>(string Context, string ConditionName, T Value) : ValidationIssue where T : System.Enum
        {
            public override string Message => $"{Context} 的 {ConditionName} 比較值無效：{Value}";
        }

        public sealed record InvalidPlayerBuffProperty(string Context, MortalGame.GameModel.PlayerBuffIntegerProperty.PlayerBuffIntegerValueType Property) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerBuffIntegerProperty.Property 無效：{Property}";
        }

        public sealed record InvalidCharacterBuffProperty(string Context, MortalGame.GameModel.CharacterBuffIntegerProperty.CharacterBuffIntegerValueType Property) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CharacterBuffIntegerProperty.Property 無效：{Property}";
        }

        public sealed record InvalidCardBuffProperty(string Context, MortalGame.GameModel.CardBuffIntegerProperty.CardBuffIntegerValueType Property) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CardBuffIntegerProperty.Property 無效：{Property}";
        }

        public sealed record MissingBuffQueryId(string Context, string BuffType) : ValidationIssue
        {
            public override string Message => $"{Context} 的 {BuffType} 查詢 BuffId 為空";
        }

        public sealed record InvalidPlayerFaction(string Context, MortalGame.GameData.Faction Faction) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerByFaction.Faction 必須是 Ally 或 Enemy：{Faction}";
        }

        public sealed record InvalidCardOrder(string Context, MortalGame.GameData.OrderType Order) : ValidationIssue
        {
            public override string Message => $"{Context} 的 IndexOfCardCollection.Order 必須是 Ascending 或 Descending：{Order}";
        }

        public sealed record UnregisteredLifeTime(string Context, System.Type ActualType) : ValidationIssue
        {
            public override string Message => $"{Context}.LifeTimeData 使用未註冊型別：{ActualType.Name}";
        }

        public sealed record NonPositiveMaxLevel(string Context) : ValidationIssue
        {
            public override string Message => $"{Context}.MaxLevel 必須大於 0";
        }

        public sealed record MissingPlayerBuffConditionSession(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerBuffSessionCondition.SessionKey 為空";
        }

        public sealed record UnknownPlayerBuffConditionSession(string Context, string SessionKey) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerBuffSessionCondition 引用不存在的 Session：{SessionKey}";
        }

        public sealed record MissingPlayerBuffIntegerSession(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerBuffSessionInteger.SessionIntegerId 為空";
        }

        public sealed record UnknownPlayerBuffIntegerSession(string Context, string SessionIntegerId) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerBuffSessionInteger 引用不存在的 Session：{SessionIntegerId}";
        }

        public sealed record WrongPlayerBuffIntegerSessionType(string Context, string SessionIntegerId) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerBuffSessionInteger 必須引用 SessionInteger：{SessionIntegerId}";
        }

        public sealed record NonPositiveLifeTimeTurn(string Context) : ValidationIssue
        {
            public override string Message => $"{Context}.LifeTimeData.Turn 必須大於 0";
        }

        public sealed record NegativeAddedCardBuffLevel(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 AddCardBuffData.Level 不可為負數";
        }

        public sealed record EmptySessionKey(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 含有空 Session Key";
        }

        public sealed record UnregisteredSessionType(string Context, string Key, System.Type ActualType) : ValidationIssue
        {
            public override string Message => $"{Context}[{Key}] 使用未註冊型別：{ActualType.Name}";
        }

        public sealed record MissingIndexedTiming(string Context, int Index) : ValidationIssue
        {
            public override string Message => $"{Context}[{Index}].Timing 不可為 None";
        }

        public sealed record InvalidTiming<T>(string Context, T Timing) : ValidationIssue where T : System.Enum
        {
            public override string Message => $"{Context} 的 Timing 無效：{Timing}";
        }
    }
}
