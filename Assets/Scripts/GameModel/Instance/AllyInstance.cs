using System;
using MortalGame.Exceptions;
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
                throw new BattleCompletionSourceMismatchException(Identity, completion.OriginAllyInstanceGuid);

            var changes = new Dictionary<Guid, CardInstanceChangeSet>();
            foreach (var change in completion.CardInstanceChanges)
            {
                if (!changes.TryAdd(change.InstanceGuid, change))
                    throw new DuplicateCardInstanceIdentityException(typeof(AllyBattleCompletion), change.InstanceGuid);
            }
            if (changes.Count != Deck.Count)
                throw new BattleCompletionCardCountMismatchException(Deck.Count, changes.Count);

            var identities = new HashSet<Guid>();
            var updatedDeck = new List<CardInstance>(Deck.Count);
            foreach (var card in Deck)
            {
                if (!identities.Add(card.InstanceGuid))
                    throw new DuplicateCardInstanceIdentityException(typeof(AllyInstance), card.InstanceGuid);
                if (!changes.TryGetValue(card.InstanceGuid, out var change))
                    throw new MissingBattleCompletionCardException(card.InstanceGuid);

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
