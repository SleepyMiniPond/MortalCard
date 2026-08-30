using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public sealed class EntityRelationshipTargetTests
    {
        [TestCase(Faction.Ally)]
        [TestCase(Faction.Enemy)]
        public void PlayerByFaction_InGlobalTiming_ReturnsRequestedPlayer(Faction faction)
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateGlobalTimingContext(built);
            var target = new PlayerByFaction { Faction = faction };

            var result = target.Eval(context);

            Assert.That(result.TryGetValue(out var player), Is.True);
            Assert.That(
                player,
                Is.SameAs(faction == Faction.Ally ? built.Ally : built.Enemy));
            Assert.That(built.Status.CurrentPlayer.Value.HasValue, Is.False);
        }

        [TestCase(Faction.None)]
        [TestCase((Faction)999)]
        public void PlayerByFaction_WhenFactionIsInvalid_ReturnsNone(Faction faction)
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateGlobalTimingContext(built);

            var result = new PlayerByFaction { Faction = faction }.Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void MainCharacterOfPlayer_WhenPlayerExists_ReturnsFirstCharacter()
        {
            var built = _BuildWithMultipleAllyCharacters();
            var context = _CreateGlobalTimingContext(built);
            var target = new MainCharacterOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally }
            };

            var result = target.Eval(context);

            Assert.That(result.TryGetValue(out var character), Is.True);
            Assert.That(character, Is.SameAs(built.Ally.Characters.First()));
            Assert.That(character, Is.SameAs(built.Ally.MainCharacter));
        }

        [Test]
        public void MainCharacterOfPlayer_WhenPlayerIsMissing_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateGlobalTimingContext(built);

            var result = new MainCharacterOfPlayer
            {
                Player = new NonePlayer()
            }.Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void CharactersOfPlayer_WhenPlayerExists_PreservesMainAndAssistOrder()
        {
            var built = _BuildWithMultipleAllyCharacters();
            var context = _CreateGlobalTimingContext(built);
            var target = new CharactersOfPlayer
            {
                Player = new PlayerByFaction { Faction = Faction.Ally }
            };

            var result = target.Eval(context).ToArray();

            Assert.That(result, Is.EqualTo(built.Ally.Characters.ToArray()));
            Assert.That(result.Select(character => character.NameKey),
                Is.EqualTo(new[] { "ally-main", "ally-assist-1", "ally-assist-2" }));
        }

        [Test]
        public void CharactersOfPlayer_WhenPlayerIsMissing_ReturnsEmptyCollection()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateGlobalTimingContext(built);
            var target = new CharactersOfPlayer { Player = new NonePlayer() };

            var result = target.Eval(context);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void CharacterOwner_WhenAssistCharacterExists_ReturnsOwningPlayer()
        {
            var built = _BuildWithMultipleAllyCharacters();
            var context = _CreateGlobalTimingContext(built);
            var target = new CharacterOwner
            {
                Character = new FixedCharacter(built.Ally.Characters.ElementAt(1))
            };

            var result = target.Eval(context);

            Assert.That(result.TryGetValue(out var player), Is.True);
            Assert.That(player, Is.SameAs(built.Ally));
        }

        [Test]
        public void CharacterOwner_WhenCharacterIsOutsideBattle_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateGlobalTimingContext(built);
            var target = new CharacterOwner
            {
                Character = new FixedCharacter(DummyCharacter.Instance)
            };

            var result = target.Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void CharacterOwner_WhenCharacterIsMissing_ReturnsNone()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var context = _CreateGlobalTimingContext(built);

            var result = new CharacterOwner
            {
                Character = new NoneCharacter()
            }.Eval(context);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void CompleteRelationshipChain_InGlobalTiming_ReturnsOwnersCharacters()
        {
            var built = _BuildWithMultipleAllyCharacters();
            var context = _CreateGlobalTimingContext(built);
            var target = new CharactersOfPlayer
            {
                Player = new CharacterOwner
                {
                    Character = new MainCharacterOfPlayer
                    {
                        Player = new PlayerByFaction { Faction = Faction.Ally }
                    }
                }
            };

            var result = target.Eval(context);

            Assert.That(result, Is.EqualTo(built.Ally.Characters));
        }

        private static BuiltGameplay _BuildWithMultipleAllyCharacters()
        {
            return new GameplayManagerTestBuilder()
                .WithAllyCharacters(
                    new CharacterParameter
                    {
                        NameKey = "ally-main",
                        CurrentHealth = 100,
                        MaxHealth = 100
                    },
                    new CharacterParameter
                    {
                        NameKey = "ally-assist-1",
                        CurrentHealth = 80,
                        MaxHealth = 80
                    },
                    new CharacterParameter
                    {
                        NameKey = "ally-assist-2",
                        CurrentHealth = 60,
                        MaxHealth = 60
                    })
                .Build();
        }

        private static TriggerContext _CreateGlobalTimingContext(BuiltGameplay built)
        {
            return new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.BeforeTurnEnd, SystemSource.Instance));
        }

        private sealed class FixedCharacter : ITargetCharacterValue
        {
            private readonly ICharacterEntity _character;

            public FixedCharacter(ICharacterEntity character)
            {
                _character = character;
            }

            public Option<ICharacterEntity> Eval(TriggerContext triggerContext)
            {
                return _character.Some();
            }
        }
    }
}
