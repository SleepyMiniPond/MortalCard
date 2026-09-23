using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class AutomaticMainTargetSelectionTests
    {
        [Test]
        public void SelectMainTarget_ToEnemy_UsesExplicitPerspectiveWithoutCurrentPlayer()
        {
            var built = CreateCharacterSelectionSetup(TargetCandidateScope.ToEnemy);
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardTestBuilder.CardId);

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                card,
                built.Enemy);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TargetType, Is.EqualTo(TargetType.AllyCharacter));
            Assert.That(result.TargetIdentity, Is.EqualTo(built.Ally.Characters.Single().Identity));
            Assert.That(built.Status.CurrentPlayer.Value.HasValue, Is.False);
        }

        [Test]
        public void SelectMainTarget_ToAlly_UsesExplicitPerspectiveWithoutChangingCurrentPlayer()
        {
            var built = CreateCharacterSelectionSetup(TargetCandidateScope.ToAlly);
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardTestBuilder.CardId);

            using (built.Status.SetCurrentPlayer(built.Ally))
            {
                var result = SelectTargetLogic.SelectMainTarget(
                    built.Manager,
                    card,
                    built.Enemy);

                Assert.That(result.IsValid, Is.True);
                Assert.That(result.TargetType, Is.EqualTo(TargetType.EnemyCharacter));
                Assert.That(result.TargetIdentity, Is.EqualTo(built.Enemy.Characters.Single().Identity));
                Assert.That(
                    built.Status.CurrentPlayer.Value.TryGetValue(out var currentPlayer),
                    Is.True);
                Assert.That(currentPlayer, Is.SameAs(built.Ally));
            }
        }

        [Test]
        public void SelectMainTarget_FixedAllyCharacter_UsesGlobalFaction()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CharacterAllySelectable(),
                CandidateScope = TargetCandidateScope.None
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                card,
                built.Enemy);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TargetType, Is.EqualTo(TargetType.AllyCharacter));
            Assert.That(result.TargetIdentity, Is.EqualTo(built.Ally.Characters.Single().Identity));
        }

        [Test]
        public void EnemyUseCardAction_UsesEnemyAsAutomaticSelectionPerspective()
        {
            var built = CreateCharacterSelectionSetup(TargetCandidateScope.ToEnemy);
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, CardTestBuilder.CardId);
            built.Enemy.CardManager.HandCard.AddCard(card);
            Assert.That(built.Enemy.SelectedCards.TryAddCard(card), Is.True);

            var success = built.Enemy.TryGetNextUseCardAction(
                built.Manager,
                System.Array.Empty<System.Guid>(),
                out var action);

            Assert.That(success, Is.True);
            Assert.That(action.CardIndentity, Is.EqualTo(card.Identity));
            Assert.That(
                action.MainSelectionAction.SelectedTarget.TryGetValue(out var targetIdentity),
                Is.True);
            Assert.That(targetIdentity, Is.EqualTo(built.Ally.Characters.Single().Identity));
            Assert.That(
                action.MainSelectionAction.TargetType,
                Is.EqualTo(TargetType.AllyCharacter));
            Assert.That(built.Status.CurrentPlayer.Value.HasValue, Is.False);
        }

        [Test]
        public void SelectMainTarget_RandomCharacter_UsesGameRandomAndReportsActualFaction()
        {
            const int candidateCount = 4;
            var seed = Enumerable.Range(1, 100)
                .First(value => new GameRandom(value).Range(0, candidateCount) > 0);
            var expectedIndex = new GameRandom(seed).Range(0, candidateCount);
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CharacterSelectable(),
                CandidateScope = TargetCandidateScope.Any,
                AutomaticSelection = AutomaticTargetSelectionStrategy.Random
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithRandomSeed(seed)
                .WithAllyCharacters(
                    Character("ally-1"),
                    Character("ally-2"))
                .WithEnemyCharacters(
                    Character("enemy-1"),
                    Character("enemy-2"))
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var candidates = built.Ally.Characters
                .Concat(built.Enemy.Characters)
                .ToArray();

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                card,
                built.Ally);

            Assert.That(expectedIndex, Is.GreaterThan(0));
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TargetIdentity, Is.EqualTo(candidates[expectedIndex].Identity));
            Assert.That(
                result.TargetType,
                Is.EqualTo(expectedIndex < built.Ally.Characters.Count
                    ? TargetType.AllyCharacter
                    : TargetType.EnemyCharacter));
        }

        [Test]
        public void SelectMainTarget_RandomCard_UsesGameRandomAndReportsActualFaction()
        {
            const int candidateCount = 2;
            var seed = Enumerable.Range(1, 100)
                .First(value => new GameRandom(value).Range(0, candidateCount) == 1);
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CardSelectable(),
                CandidateScope = TargetCandidateScope.Any,
                AutomaticSelection = AutomaticTargetSelectionStrategy.Random
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithRandomSeed(seed)
                .Build();
            var selectingCard = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var allyCandidate = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            var enemyCandidate = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(allyCandidate);
            built.Enemy.CardManager.HandCard.AddCard(enemyCandidate);

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                selectingCard,
                built.Ally);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TargetType, Is.EqualTo(TargetType.EnemyCard));
            Assert.That(result.TargetIdentity, Is.EqualTo(enemyCandidate.Identity));
        }

        [Test]
        public void SelectMainTarget_RandomCharacterWithoutCandidates_IsInvalid()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CharacterSelectable(),
                CandidateScope = TargetCandidateScope.Any,
                AutomaticSelection = AutomaticTargetSelectionStrategy.Random
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithAllyCharacters()
                .WithEnemyCharacters()
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                card,
                built.Ally);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.TargetType, Is.EqualTo(TargetType.None));
            Assert.That(result.TargetIdentity, Is.EqualTo(System.Guid.Empty));
        }

        [Test]
        public void SelectMainTarget_NoneSelectableWithoutCandidates_IsValid()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new NoneSelectable(),
                CandidateScope = TargetCandidateScope.None
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithAllyCharacters()
                .WithEnemyCharacters()
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                card,
                built.Ally);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TargetType, Is.EqualTo(TargetType.None));
            Assert.That(result.TargetIdentity, Is.EqualTo(System.Guid.Empty));
        }

        [Test]
        public void SelectMainTarget_ToEnemyRandom_SelectsOnlyWithinOpponentCandidates()
        {
            const int candidateCount = 2;
            var seed = Enumerable.Range(1, 100)
                .First(value => new GameRandom(value).Range(0, candidateCount) == 1);
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CharacterSelectable(),
                CandidateScope = TargetCandidateScope.ToEnemy,
                AutomaticSelection = AutomaticTargetSelectionStrategy.Random
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithRandomSeed(seed)
                .WithAllyCharacters(
                    Character("ally-1"),
                    Character("ally-2"))
                .WithEnemyCharacters(
                    Character("enemy-1"),
                    Character("enemy-2"))
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                card,
                built.Enemy);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TargetType, Is.EqualTo(TargetType.AllyCharacter));
            Assert.That(result.TargetIdentity, Is.EqualTo(built.Ally.Characters.ElementAt(1).Identity));
        }

        [Test]
        public void SelectMainTarget_FixedAllyRandom_UsesStrategyWithinGlobalFaction()
        {
            const int candidateCount = 2;
            var seed = Enumerable.Range(1, 100)
                .First(value => new GameRandom(value).Range(0, candidateCount) == 1);
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CharacterAllySelectable(),
                CandidateScope = TargetCandidateScope.None,
                AutomaticSelection = AutomaticTargetSelectionStrategy.Random
            };
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .WithRandomSeed(seed)
                .WithAllyCharacters(
                    Character("ally-1"),
                    Character("ally-2"))
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);

            var result = SelectTargetLogic.SelectMainTarget(
                built.Manager,
                card,
                built.Enemy);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.TargetType, Is.EqualTo(TargetType.AllyCharacter));
            Assert.That(result.TargetIdentity, Is.EqualTo(built.Ally.Characters.ElementAt(1).Identity));
        }

        [TestCase(TargetCandidateScope.None, 0)]
        [TestCase(TargetCandidateScope.ToEnemy, 1)]
        [TestCase(TargetCandidateScope.ToAlly, 2)]
        [TestCase(TargetCandidateScope.Any, 3)]
        public void TargetCandidateScope_KeepsSerializedValue(
            TargetCandidateScope scope,
            int expectedValue)
        {
            Assert.That((int)scope, Is.EqualTo(expectedValue));
        }

        [TestCase(AutomaticTargetSelectionStrategy.First, 0)]
        [TestCase(AutomaticTargetSelectionStrategy.Random, 1)]
        public void AutomaticTargetSelectionStrategy_KeepsSerializedValue(
            AutomaticTargetSelectionStrategy strategy,
            int expectedValue)
        {
            Assert.That((int)strategy, Is.EqualTo(expectedValue));
        }

        private static BuiltGameplay CreateCharacterSelectionSetup(
            TargetCandidateScope candidateScope)
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CharacterSelectable(),
                CandidateScope = candidateScope,
                AutomaticSelection = AutomaticTargetSelectionStrategy.First
            };
            return new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
        }

        private static CharacterParameter Character(string name)
            => new()
            {
                NameKey = name,
                CurrentHealth = 10,
                MaxHealth = 10
            };
    }
}
