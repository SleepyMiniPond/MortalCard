namespace MortalGame.Editor
{
    public abstract partial record ValidationIssue
    {
        public sealed record EmptyAllConditions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 AllCondition.Conditions 至少需要一項";
        }

        public sealed record EmptyAnyConditions(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 AnyCondition.Conditions 至少需要一項";
        }

        public sealed record InvalidConditionTiming(string Context, MortalGame.GameData.GameTiming Timing) : ValidationIssue
        {
            public override string Message => $"{Context} 的 GameTimingCondition.Timing 必須是有效 Timing：{Timing}";
        }

        public sealed record InvalidIntegerComparison(string Context, MortalGame.GameData.ArithmeticConditionType Arithmetic) : ValidationIssue
        {
            public override string Message => $"{Context} 的 IntegerCompare.Arithmetic 必須是有效比較運算：{Arithmetic}";
        }

        public sealed record MissingArithmeticOperation(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 ArithmeticInteger.Operation 不可為 None";
        }

        public sealed record UnsupportedArithmeticOverwrite(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 ArithmeticInteger.Operation 不支援 Overwrite";
        }

        public sealed record ZeroArithmeticDivisor(string Context, MortalGame.GameData.ArithmeticType Operation) : ValidationIssue
        {
            public override string Message => $"{Context} 的 ArithmeticInteger.{Operation} 除數不可為 0";
        }

        public sealed record StaticArithmeticOverflow(string Context, MortalGame.GameData.ArithmeticType Operation) : ValidationIssue
        {
            public override string Message => $"{Context} 的 ArithmeticInteger.{Operation} 常數運算結果超出 Int32 範圍";
        }

        public sealed record EmptyMinimumValues(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 MinimumInteger.Values 至少需要一項";
        }

        public sealed record EmptyMaximumValues(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 MaximumInteger.Values 至少需要一項";
        }

        public sealed record InvalidCardProperty(string Context, MortalGame.GameModel.CardIntegerProperty.CardIntegerValueType Property) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CardIntegerProperty.Property 無效：{Property}";
        }

        public sealed record InvalidCharacterProperty(string Context, MortalGame.GameModel.CharacterIntegerProperty.CharacterIntegerValueType Property) : ValidationIssue
        {
            public override string Message => $"{Context} 的 CharacterIntegerProperty.Property 無效：{Property}";
        }

        public sealed record InvalidPlayerProperty(string Context, MortalGame.GameModel.PlayerIntegerProperty.PlayerIntegerValueType Property) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerIntegerProperty.Property 無效：{Property}";
        }

        public sealed record EnemyDispositionQuery(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 PlayerIntegerProperty.CurrentDisposition 不可指定 Enemy";
        }
    }
}
