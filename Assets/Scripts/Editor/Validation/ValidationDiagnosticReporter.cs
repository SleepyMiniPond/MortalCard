using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Editor
{
    /// <summary>統一驗證結果的呈現；是否取消 Play 或中止 Build 仍由各 Gate 決定。</summary>
    internal static class ValidationDiagnosticReporter
    {
        private const string DialogTitle = "遊戲資料驗證";
        private const string ConfirmLabel = "確定";

        public static void ReportValid()
        {
            Debug.Log("遊戲資料驗證完成：未發現錯誤。");
            EditorUtility.DisplayDialog(DialogTitle, "未發現錯誤。", ConfirmLabel);
        }

        public static void ReportInvalidForMenu(IReadOnlyList<ValidationIssue> issues) => _ReportInvalid(issues, false);
        public static void ReportInvalidForPlayMode(IReadOnlyList<ValidationIssue> issues) => _ReportInvalid(issues, true);

        public static void ReportUnexpectedFailure(Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(DialogTitle,
                "驗證遊戲資料時發生例外，已取消進入 Play Mode。請查看 Console。", ConfirmLabel);
        }

        private static void _ReportInvalid(IReadOnlyList<ValidationIssue> issues, bool blocksPlayMode)
        {
            foreach (var issue in issues)
                Debug.LogError(blocksPlayMode ? $"[Play Mode Gate] {issue?.Message}" : $"[遊戲資料驗證] {issue?.Message}");
            var summary = blocksPlayMode
                ? $"發現 {issues.Count} 個錯誤，已取消進入 Play Mode。請查看 Console。"
                : $"發現 {issues.Count} 個錯誤，請查看 Console。";
            EditorUtility.DisplayDialog(DialogTitle, summary, ConfirmLabel);
        }
    }
}
