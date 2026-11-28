using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameModel
{

    public record AllyInstance(
        Guid Identity,
        string NameKey,
        int CurrentDisposition,
        int CurrentHealth,
        int MaxHealth,
        int CurrentEnergy,
        int MaxEnergy,
        List<CardInstance> Deck,
        int HandCardMaxCount)
    {
        /// <summary>
        /// 以完整勝利快照建立新的跨戰鬥狀態，不修改原實例與牌組。
        /// </summary>
        public AllyInstance ApplyBattleCompletion(AllyBattleCompletion completion)
        {
            if (completion == null)
                throw new ArgumentNullException(nameof(completion));
            if (completion.OriginAllyInstanceGuid != Identity)
                throw new InvalidOperationException("戰後快照的來源玩家與目前 AllyInstance 不一致。");

            var changes = new Dictionary<Guid, CardInstanceChangeSet>();
            foreach (var change in completion.CardInstanceChanges)
            {
                if (!changes.TryAdd(change.InstanceGuid, change))
                    throw new InvalidOperationException($"戰後快照包含重複的卡片身份：{change.InstanceGuid}");
            }
            if (changes.Count != Deck.Count)
                throw new InvalidOperationException("戰後快照必須涵蓋完整原牌組，不得新增或遺漏卡片。");

            var identities = new HashSet<Guid>();
            var updatedDeck = new List<CardInstance>(Deck.Count);
            foreach (var card in Deck)
            {
                if (!identities.Add(card.InstanceGuid))
                    throw new InvalidOperationException($"原牌組包含重複的卡片身份：{card.InstanceGuid}");
                if (!changes.TryGetValue(card.InstanceGuid, out var change))
                    throw new InvalidOperationException($"戰後快照缺少原牌組的卡片身份：{card.InstanceGuid}");

                updatedDeck.Add(card with { PersistentFormState = change.PersistentFormState });
            }

            return this with
            {
                CurrentHealth = completion.CurrentHealth,
                CurrentDisposition = completion.CurrentDisposition,
                CurrentEnergy = MaxEnergy,
                Deck = updatedDeck
            };
        }
    }

}
