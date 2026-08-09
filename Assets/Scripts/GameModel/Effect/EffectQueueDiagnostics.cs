using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MortalGame.GameModel
{
    public sealed record EffectQueueHaltDiagnostic(
        Guid CorrelationId,
        int Budget,
        int ProcessedItemCount,
        IReadOnlyList<string> TriggerPath);

    /// <summary>
    /// 集中處理 Effect Queue 停止診斷的格式化與輸出。
    /// </summary>
    internal static class EffectQueueDiagnosticLogger
    {
        private const int TRIGGER_PATH_VISIBLE_SEGMENT_COUNT_PER_EDGE = 4;

        public static void LogBudgetExceeded(EffectQueueHaltDiagnostic diagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            Debug.LogError(
                "[EffectQueueRunner] 執行預算已耗盡。" +
                $"CorrelationId={diagnostic.CorrelationId}, " +
                $"Budget={diagnostic.Budget}, " +
                $"ProcessedItemCount={diagnostic.ProcessedItemCount}, " +
                $"TriggerPath={CreateTriggerPathSummary(diagnostic.TriggerPath)}");
        }

        internal static string CreateTriggerPathSummary(IReadOnlyList<string> triggerPath)
        {
            if (triggerPath == null || triggerPath.Count == 0)
                return "(empty)";

            var segments = new List<string>();
            for (var index = 0; index < triggerPath.Count;)
            {
                var itemName = triggerPath[index];
                var repeatedCount = 1;
                while (index + repeatedCount < triggerPath.Count &&
                       triggerPath[index + repeatedCount] == itemName)
                {
                    repeatedCount++;
                }

                segments.Add(repeatedCount > 1
                    ? $"{itemName} ×{repeatedCount}"
                    : itemName);
                index += repeatedCount;
            }

            var visibleSegmentCount = TRIGGER_PATH_VISIBLE_SEGMENT_COUNT_PER_EDGE * 2;
            if (segments.Count <= visibleSegmentCount)
                return string.Join(" -> ", segments);

            var omittedSegmentCount = segments.Count - visibleSegmentCount;
            var head = string.Join(
                " -> ",
                segments.Take(TRIGGER_PATH_VISIBLE_SEGMENT_COUNT_PER_EDGE));
            var tail = string.Join(
                " -> ",
                segments.Skip(segments.Count - TRIGGER_PATH_VISIBLE_SEGMENT_COUNT_PER_EDGE));
            return $"{head} -> ...（省略 {omittedSegmentCount} 個路徑片段）... -> {tail}";
        }
    }
}
