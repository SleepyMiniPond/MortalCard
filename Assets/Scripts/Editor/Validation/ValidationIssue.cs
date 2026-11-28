using System;
using System.Collections.Generic;

namespace MortalGame.Editor
{
    /// <summary>保存驗證問題的必要資料；文字由各領域問題定義集中產生。</summary>
    public abstract partial record ValidationIssue
    {
        public abstract string Message { get; }
        public string Code => GetType().Name.Split('`')[0];
    }

    /// <summary>維持原驗證流程依完整診斷文字去重的相容性。</summary>
    internal sealed class ValidationIssueMessageComparer : IEqualityComparer<ValidationIssue>
    {
        public static ValidationIssueMessageComparer Instance { get; } = new();
        public bool Equals(ValidationIssue first, ValidationIssue second) =>
            StringComparer.Ordinal.Equals(first?.Message, second?.Message);
        public int GetHashCode(ValidationIssue issue) => StringComparer.Ordinal.GetHashCode(issue.Message);
    }
}
