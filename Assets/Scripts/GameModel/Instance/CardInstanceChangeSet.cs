using System;
using System.Collections.Generic;
using System.Linq;
using Optional;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 描述一次戰鬥勝利後，指定 CardInstance 的持久形態應設為何值。
    /// PersistentFormState 為 None 時代表清除既有持久形態。
    /// </summary>
    public sealed record CardInstanceChangeSet(
        Guid InstanceGuid,
        Option<PersistentCardFormState> PersistentFormState);

    /// <summary>
    /// 從玩家原始 CardInstance 與戰鬥勝利時的卡片實體建立持久形態變更集。
    /// 非勝利結果會回傳空集合，不清除 Override，也不修改卡片狀態。
    /// </summary>
    public static class CardInstanceChangeSetCollector
    {
        public static IReadOnlyList<CardInstanceChangeSet> CollectForBattleResult(
            bool isAllyWin,
            IReadOnlyCollection<CardInstance> originalInstances,
            IPlayerCardManager cardManager)
        {
            return isAllyWin
                ? Collect(originalInstances, cardManager)
                : Array.Empty<CardInstanceChangeSet>();
        }

        public static IReadOnlyList<CardInstanceChangeSet> Collect(
            IReadOnlyCollection<CardInstance> originalInstances,
            IPlayerCardManager cardManager)
        {
            if (originalInstances == null)
            {
                throw new ArgumentNullException(nameof(originalInstances));
            }
            if (cardManager == null)
            {
                throw new ArgumentNullException(nameof(cardManager));
            }

            return originalInstances
                .Select(instance => _CreateChangeSet(instance, cardManager))
                .ToArray();
        }

        private static CardInstanceChangeSet _CreateChangeSet(
            CardInstance instance,
            IPlayerCardManager cardManager)
        {
            var card = cardManager
                .GetCardOrNone(candidate =>
                    candidate.OriginCardInstanceGuid
                        .Map(identity => identity == instance.InstanceGuid)
                        .ValueOr(false))
                .ValueOr(() => throw new InvalidOperationException(
                    $"戰鬥結束時找不到 CardInstance [{instance.InstanceGuid}] 對應的卡片實體。"));

            if (card.OverrideFormState.TryGetValue(out var overrideState))
            {
                var removeResult = card.TryRemoveOverrideForm(overrideState.Identity);
                if (!removeResult.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"無法清除卡片 [{card.Identity}] 的 External Override：{removeResult.RejectedReason}");
                }
            }

            var updatedInstance = CardInstancePersistenceMapper
                .TryUpdate(card, instance)
                .ValueOr(() => throw new InvalidOperationException(
                    $"無法建立 CardInstance [{instance.InstanceGuid}] 的持久形態輸出。"));

            return new CardInstanceChangeSet(
                instance.InstanceGuid,
                updatedInstance.PersistentFormState);
        }
    }
}
