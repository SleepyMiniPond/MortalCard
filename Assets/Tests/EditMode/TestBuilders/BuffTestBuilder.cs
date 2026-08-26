using System;
using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;
using Optional;

namespace MortalGame.Tests
{

    public static class BuffTestBuilder
    {
        public const string PlayerBuffId = "test-player-buff";
        public const string CharacterBuffId = "test-character-buff";
        public const string CardBuffId = "test-card-buff";

        public static PlayerBuffEntity CreatePlayerBuff(
            string buffId = PlayerBuffId,
            IPlayerEntity caster = null,
            IReadOnlyDictionary<string, IReactionSessionEntity> reactionSessions = null,
            int level = 1,
            int maxLevel = int.MaxValue)
        {
            return (PlayerBuffEntity)Activator.CreateInstance(
                typeof(PlayerBuffEntity),
                buffId,
                Guid.NewGuid(),
                level,
                maxLevel,
                PlayerCasterOption(caster),
                Array.Empty<IPlayerBuffPropertyEntity>(),
                new AlwaysLifeTimePlayerBuffEntity(),
                reactionSessions ?? new Dictionary<string, IReactionSessionEntity>());
        }

        public static CharacterBuffEntity CreateCharacterBuff(string buffId = CharacterBuffId, IPlayerEntity caster = null)
        {
            return (CharacterBuffEntity)Activator.CreateInstance(
                typeof(CharacterBuffEntity),
                buffId,
                Guid.NewGuid(),
                1,
                PlayerCasterOption(caster),
                Array.Empty<ICharacterBuffPropertyEntity>(),
                new AlwaysLifeTimeCharacterBuffEntity(),
                new Dictionary<string, IReactionSessionEntity>());
        }

        public static CardBuffEntity CreateCardBuff(
            TriggerContext context,
            CardBuffLibrary cardBuffLibrary,
            string buffId = CardBuffId,
            IPlayerEntity caster = null,
            int level = 1)
        {
            var result = (Option<CardBuffEntity>)typeof(CardBuffEntity)
                .GetMethod(nameof(CardBuffEntity.CreateFromData))
                .Invoke(null, new[]
                {
                buffId,
                (object)level,
                PlayerCasterOption(caster),
                context,
                cardBuffLibrary,
                context.Model.ContextManager.CardBuffPropertyEntityFactory,
                context.Model.ContextManager.CardBuffLifeTimeEntityFactory,
                context.Model.ContextManager.ReactionSessionEntityFactory
                });

            if (!result.TryGetValue(out var buff))
            {
                throw new InvalidOperationException("測試 CardBuff 資料應能建立有效的 Runtime Entity。");
            }

            return buff;
        }

        public static PlayerBuffData CreatePlayerBuffData(
            string buffId,
            GameTiming timing,
            ConditionalPlayerBuffEffect conditionalEffect)
        {
            return new PlayerBuffData
            {
                ID = buffId,
                MaxLevel = 99,
                LifeTimeData = new AlwaysLifeTimePlayerBuffData(),
                BuffEffects = new Dictionary<GameTiming, ConditionalPlayerBuffEffect[]>
            {
                { timing, new[] { conditionalEffect } }
            }
            };
        }

        public static CharacterBuffData CreateCharacterBuffData(
            string buffId,
            GameTiming timing,
            ConditionalCharacterBuffEffect conditionalEffect)
        {
            return new CharacterBuffData
            {
                ID = buffId,
                MaxLevel = 99,
                LifeTimeData = new AlwaysLifeTimeCharacterBuffData(),
                BuffEffects = new Dictionary<GameTiming, ConditionalCharacterBuffEffect[]>
            {
                { timing, new[] { conditionalEffect } }
            }
            };
        }

        public static CardBuffData CreateCardBuffData(
            string buffId,
            GameTiming timing,
            ConditionalCardBuffEffect conditionalEffect)
        {
            return new CardBuffData
            {
                ID = buffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData(),
                BuffEffects = new Dictionary<GameTiming, ConditionalCardBuffEffect[]>
            {
                { timing, new[] { conditionalEffect } }
            }
            };
        }

        private static object PlayerCasterOption(IPlayerEntity caster)
        {
            return caster == null
                ? OptionTestValue.None(typeof(IPlayerEntity))
                : OptionTestValue.Some(typeof(IPlayerEntity), caster);
        }
    }
}
