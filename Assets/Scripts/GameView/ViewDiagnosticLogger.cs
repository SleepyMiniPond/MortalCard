using System;
using UnityEngine;

namespace MortalGame.GameView
{
    internal static class ViewDiagnosticLogger
    {
        public static void LogRenderEvent(Type eventType) => Debug.Log($"Render Event: {eventType.Name}");
        public static void LogAllySummon(string nameKey) => _LogSummon("Ally", nameKey);
        public static void LogEnemySummon(string nameKey) => _LogSummon("Enemy", nameKey);

        private static void _LogSummon(string side, string nameKey) => Debug.Log($"Summon {side}: {nameKey}");
    }
}
