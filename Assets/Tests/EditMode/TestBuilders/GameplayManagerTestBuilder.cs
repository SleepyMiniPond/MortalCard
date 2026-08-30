using System;
using MortalGame.GameModel;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;

namespace MortalGame.Tests
{

    public sealed class GameplayManagerTestBuilder
    {
        private readonly Dictionary<string, CardData> _cards = new();
        private readonly Dictionary<string, PlayerBuffData> _playerBuffs = new();
        private readonly Dictionary<string, CharacterBuffData> _characterBuffs = new();
        private readonly Dictionary<string, CardBuffData> _cardBuffs = new();
        private IReadOnlyList<CharacterParameter> _allyCharacters = new[]
        {
            new CharacterParameter
            {
                NameKey = "ally",
                CurrentHealth = 100,
                MaxHealth = 100
            }
        };
        private IReadOnlyList<CharacterParameter> _enemyCharacters = new[]
        {
            new CharacterParameter
            {
                NameKey = "enemy",
                CurrentHealth = 100,
                MaxHealth = 100
            }
        };

        public GameplayManagerTestBuilder WithAllyCharacters(
            params CharacterParameter[] characters)
        {
            _allyCharacters = characters;
            return this;
        }

        public GameplayManagerTestBuilder WithEnemyCharacters(
            params CharacterParameter[] characters)
        {
            _enemyCharacters = characters;
            return this;
        }

        public GameplayManagerTestBuilder WithCard(StandardCardData cardData)
        {
            _cards[cardData.ID] = cardData;
            return this;
        }

        public GameplayManagerTestBuilder WithCard(OverrideCardData cardData)
        {
            _cards[cardData.ID] = cardData;
            return this;
        }

        public GameplayManagerTestBuilder WithPlayerBuff(PlayerBuffData buffData)
        {
            _playerBuffs[buffData.ID] = buffData;
            return this;
        }

        public GameplayManagerTestBuilder WithCharacterBuff(CharacterBuffData buffData)
        {
            _characterBuffs[buffData.ID] = buffData;
            return this;
        }

        public GameplayManagerTestBuilder WithCardBuff(CardBuffData buffData)
        {
            _cardBuffs[buffData.ID] = buffData;
            return this;
        }

        public BuiltGameplay Build()
        {
            if (!_cards.ContainsKey(CardTestBuilder.CardId))
            {
                _cards[CardTestBuilder.CardId] = CardTestBuilder.CreateCardData();
            }

            var cardLibrary = GameContextTestBuilder.CreateCardLibrary(_cards);
            var cardBuffLibrary = GameContextTestBuilder.CreateCardBuffLibrary(_cardBuffs);
            var playerBuffLibrary = GameContextTestBuilder.CreatePlayerBuffLibrary(_playerBuffs);
            var characterBuffLibrary = GameContextTestBuilder.CreateCharacterBuffLibrary(_characterBuffs);
            var contextManager = GameContextTestBuilder.CreateContextManager(
                cardLibrary,
                cardBuffLibrary,
                playerBuffLibrary,
                characterBuffLibrary);

            var ally = new AllyEntity(
                Guid.NewGuid(),
                _allyCharacters.ToArray(),
                currentEnergy: 0,
                maxEnergy: 3,
                handCardMaxCount: 5,
                currentDisposition: 0,
                maxDisposition: 10,
                gameContext: contextManager);
            var enemy = new EnemyEntity(
                _enemyCharacters.ToArray(),
                currentEnergy: 0,
                maxEnergy: 3,
                handCardMaxCount: 5,
                selectedCardMaxCount: 3,
                turnStartDrawCardCount: 0,
                energyRecoverPoint: 0,
                gameContext: contextManager);

            var status = new GameStatus();
            status.SummonAlly(ally);
            status.SummonEnemy(enemy);
            var manager = new GameplayManager(default, contextManager, status);

            return new BuiltGameplay(manager, contextManager, status, ally, enemy);
        }
    }

    public sealed record BuiltGameplay(
        GameplayManager Manager,
        GameContextManager ContextManager,
        GameStatus Status,
        AllyEntity Ally,
        EnemyEntity Enemy);
}
