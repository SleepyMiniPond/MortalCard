namespace MortalGame.Editor
{
    public abstract partial record ValidationIssue
    {
        public sealed record NonPositiveTemplateOfferCount(string Context, string TemplateSelectionId) : ValidationIssue
        {
            public override string Message => $"{Context}[{TemplateSelectionId}].OfferCount 必須大於零";
        }

        public sealed record EmptyTemplateCardIds(string Context, string TemplateSelectionId) : ValidationIssue
        {
            public override string Message => $"{Context}[{TemplateSelectionId}].TemplateCardDataIds 至少需要一項";
        }

        public sealed record DuplicateTemplateCardId(string Context, string TemplateSelectionId) : ValidationIssue
        {
            public override string Message => $"{Context}[{TemplateSelectionId}] 的模板 ID 重複";
        }

        public sealed record InvalidTemplateCardType(string Context, string TemplateSelectionId, string Id) : ValidationIssue
        {
            public override string Message => $"{Context}[{TemplateSelectionId}] 的模板必須是 Catalog 中的 StandardCardData：{Id}";
        }

        public sealed record NonPositiveEffectOfferCount(string Context) : ValidationIssue
        {
            public override string Message => $"{Context}.OfferCount 必須大於零";
        }

        public sealed record InvalidTemplateSelectionReference(string Context) : ValidationIssue
        {
            public override string Message => $"{Context}.TemplateSelectionId 必須引用前面的模板群組";
        }

        public sealed record MissingTemplatePool(string Context, string TemplateId) : ValidationIssue
        {
            public override string Message => $"{Context} 缺少模板候選池：{TemplateId}";
        }

        public sealed record UnexpectedTemplatePool(string Context, string Key) : ValidationIssue
        {
            public override string Message => $"{Context} 含未提供的模板候選池：{Key}";
        }

        public sealed record EmptyTemplatePool(string Context, string Key) : ValidationIssue
        {
            public override string Message => $"{Context}[{Key}] 候選池不可為空";
        }

        public sealed record EmptyEffectOptionId(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的選項 Id 為空";
        }

        public sealed record DuplicateEffectOptionId(string Context, string OptionId) : ValidationIssue
        {
            public override string Message => $"{Context} 的選項 Id 重複：{OptionId}";
        }

        public sealed record MissingDescriptionKey(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 DescriptionKey 為空";
        }

        public sealed record InvalidDescriptionParameterName(string Context, string OptionId, string ParameterName) : ValidationIssue
        {
            public override string Message => $"{Context}[{OptionId}] 的描述參數名稱無效或保留：{ParameterName}";
        }

        public sealed record InvalidDescriptionParameterValue(string Context, string OptionId, string ParameterName) : ValidationIssue
        {
            public override string Message => $"{Context}[{OptionId}].DescriptionParameters[{ParameterName}] 必須直接引用本選項效果的 Value";
        }

        public sealed record MissingDescriptionRuleKey(string Context, string OptionId, string ParameterName) : ValidationIssue
        {
            public override string Message => $"{Context}[{OptionId}].DescriptionParameters[{ParameterName}].RuleKey 不可為空";
        }

        public sealed record EmptyEffectOption(string Context, string OptionId) : ValidationIssue
        {
            public override string Message => $"{Context}[{OptionId}] 至少需要一個普通或觸發效果";
        }

        public sealed record MissingDescriptionParameter(string CardDataId, string DescriptionKey, string ParameterName) : ValidationIssue
        {
            public override string Message => $"CardData[{CardDataId}] 的描述 {DescriptionKey} 缺少參數：{ParameterName}";
        }

        public sealed record MissingDescriptionRuleKeyReference(string CardDataId, string RuleKey) : ValidationIssue
        {
            public override string Message => $"CardData[{CardDataId}] 的描述規則鍵不存在：{RuleKey}";
        }
    }
}
