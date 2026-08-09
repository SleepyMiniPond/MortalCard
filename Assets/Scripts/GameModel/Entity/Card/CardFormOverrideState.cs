using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 單次 External Override 的執行期狀態。
    /// Identity 用來防止已被取代的舊解除命令移除目前 Override。
    /// </summary>
    public sealed class CardFormOverrideState
    {
        public Guid Identity { get; }
        public string OverrideKey { get; }
        public string CardDataId { get; }
        public IActionSource Source { get; }
        public IReadOnlyList<CardFormOverrideReleaseRule> ReleaseRules { get; }
        public IReadOnlyDictionary<string, IReactionSessionEntity> ReactionSessions { get; }
        public CardBuffLayerHandle BuffLayerHandle { get; }

        private CardFormOverrideState(
            Guid identity,
            string overrideKey,
            string cardDataId,
            IActionSource source,
            IReadOnlyList<CardFormOverrideReleaseRule> releaseRules,
            IReadOnlyDictionary<string, IReactionSessionEntity> reactionSessions,
            CardBuffLayerHandle buffLayerHandle)
        {
            Identity = identity;
            OverrideKey = overrideKey;
            CardDataId = cardDataId;
            Source = source;
            ReleaseRules = releaseRules;
            ReactionSessions = reactionSessions;
            BuffLayerHandle = buffLayerHandle;
        }

        public static CardFormOverrideState Create(
            string overrideKey,
            string cardDataId,
            IActionSource source,
            IEnumerable<CardFormOverrideReleaseRule> releaseRules,
            IReadOnlyDictionary<string, IReactionSessionEntity> reactionSessions,
            CardBuffLayerHandle buffLayerHandle)
        {
            return new CardFormOverrideState(
                Guid.NewGuid(),
                overrideKey,
                cardDataId,
                source,
                releaseRules.ToList(),
                reactionSessions.ToDictionary(pair => pair.Key, pair => pair.Value),
                buffLayerHandle);
        }
    }
}
