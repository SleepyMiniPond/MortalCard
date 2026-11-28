using System.Collections.Generic;
using System.Linq;
using MortalGame.Editor;

namespace MortalGame.Tests
{
    /// <summary>讓既有文案相容性測試明確讀取問題的呈現文字。</summary>
    internal static class ValidationTestMessages
    {
        public static IReadOnlyList<string> Of(IReadOnlyList<ValidationIssue> issues) =>
            issues.Select(issue => issue.Message).ToArray();
    }
}
