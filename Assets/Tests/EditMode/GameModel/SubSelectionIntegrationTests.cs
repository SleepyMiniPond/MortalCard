using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class SubSelectionIntegrationTests
    {
        [Test]
        public void QueryInfo_DeduplicatesCandidatesAndUsesAchievableCount()
        {
            const string cardId = "sub-selection-info";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var playedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            var firstCandidate = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            var secondCandidate = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            built.Ally.CardManager.HandCard.AddCard(playedCard);
            built.Ally.CardManager.HandCard.AddCard(firstCandidate);
            built.Ally.CardManager.HandCard.AddCard(secondCandidate);
            cardData.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "targets",
                CardCandidates = new FixedCardCollection(
                    firstCandidate,
                    secondCandidate,
                    firstCandidate),
                SelectCount = new ConstInteger { Value = 5 },
                IsMustSelect = new TrueValue()
            });

            var result = built.Manager.QueryCardSubSelectionInfos(playedCard.Identity, MainSelectionAction.Empty);

            Assert.That(result.TryGetValue(out var info), Is.True);
            Assert.That(
                info.SelectionInfos["targets"],
                Is.TypeOf<ExistCardSelectionInfo>());
            var existCardInfo = (ExistCardSelectionInfo)info.SelectionInfos["targets"];
            Assert.That(
                existCardInfo.CardInfos.Select(card => card.Identity),
                Is.EqualTo(new[]
                {
                    firstCandidate.Identity,
                    secondCandidate.Identity
                }));
            Assert.That(existCardInfo.Count, Is.EqualTo(5));
            Assert.That(existCardInfo.EffectiveCount, Is.EqualTo(2));
        }

        [Test]
        public void QueryInfo_WhenCountIsNegative_ReturnsNone()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "targets",
                CardCandidates = new FixedCardCollection(),
                SelectCount = new ConstInteger { Value = -1 },
                IsMustSelect = new TrueValue()
            });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);

            var result = built.Manager.QueryCardSubSelectionInfos(card.Identity, MainSelectionAction.Empty);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void QueryInfo_WhenSelectionTypeIsUnsupported_ReturnsNone()
        {
            var cardData = CardTestBuilder.CreateCardData();
            cardData.SubSelects.Add(new NewCardSelectionGroup { Id = "unsupported" });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);

            var result = built.Manager.QueryCardSubSelectionInfos(card.Identity, MainSelectionAction.Empty);

            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void SelectTargets_EvaluatesSubSelectionUnderMainTargetContext()
        {
            const string cardId = "sub-selection-main-context";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CardSelectable(),
                CandidateScope = TargetCandidateScope.ToEnemy,
                AutomaticSelection = AutomaticTargetSelectionStrategy.First
            };
            cardData.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "main-target",
                CardCandidates = new SingleCardCollection
                {
                    TargetCard = new SelectedCard()
                },
                SelectCount = new ConstInteger { Value = 1 },
                IsMustSelect = new TrueValue()
            });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var playedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            var enemyCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            built.Ally.CardManager.HandCard.AddCard(playedCard);
            built.Enemy.CardManager.HandCard.AddCard(enemyCard);

            var result = SelectTargetLogic.SelectTargets(
                built.Manager,
                playedCard,
                built.Ally);

            Assert.That(result.TryGetValue(out var selection), Is.True);
            Assert.That(
                selection.MainSelectionAction.SelectedTarget.ValueOr(Guid.Empty),
                Is.EqualTo(enemyCard.Identity));
            Assert.That(
                ((ExistCardSubSelectionAction)selection
                    .SubSelectionActions["main-target"])
                    .CardIdentity,
                Is.EqualTo(new[] { enemyCard.Identity }));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void PlayerQuery_EvaluatesSubSelectionUnderMainTargetContext()
        {
            const string cardId = "player-sub-selection-main-context";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            cardData.MainSelect = new MainTargetSelectLogic
            {
                MainSelectable = new CardEnemySelectable()
            };
            cardData.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "main-target",
                CardCandidates = new SingleCardCollection
                {
                    TargetCard = new SelectedCard()
                },
                SelectCount = new ConstInteger { Value = 1 },
                IsMustSelect = new TrueValue()
            });
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var playedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            var enemyCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            built.Ally.CardManager.HandCard.AddCard(playedCard);
            built.Enemy.CardManager.HandCard.AddCard(enemyCard);
            var mainSelection = MainSelectionAction.Create(
                new SelectMainTargetResult(
                    true,
                    TargetType.EnemyCard,
                    enemyCard.Identity));

            var result = built.Manager.QueryCardSubSelectionInfos(
                playedCard.Identity,
                mainSelection);

            Assert.That(result.TryGetValue(out var info), Is.True);
            Assert.That(
                ((ExistCardSelectionInfo)info.SelectionInfos["main-target"])
                    .CardInfos
                    .Select(card => card.Identity),
                Is.EqualTo(new[] { enemyCard.Identity }));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void TryCreateUseCardContext_NormalizesAndIsolatesGroups()
        {
            const string cardId = "sub-selection-groups";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var playedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            var firstCandidate = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            var secondCandidate = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            built.Ally.CardManager.HandCard.AddCard(playedCard);
            built.Ally.CardManager.HandCard.AddCard(firstCandidate);
            built.Ally.CardManager.HandCard.AddCard(secondCandidate);
            cardData.SubSelects.Add(CreateGroup(
                "first-group",
                2,
                firstCandidate,
                secondCandidate));
            cardData.SubSelects.Add(CreateGroup(
                "second-group",
                1,
                secondCandidate));
            var action = new UseCardAction(
                playedCard.Identity,
                MainSelectionAction.Empty,
                new Dictionary<string, ISubSelectionAction>
                {
                    ["first-group"] = new ExistCardSubSelectionAction(new[]
                    {
                        secondCandidate.Identity,
                        secondCandidate.Identity,
                        Guid.NewGuid(),
                        firstCandidate.Identity
                    }),
                    ["second-group"] = new ExistCardSubSelectionAction(new[]
                    {
                        secondCandidate.Identity
                    })
                });

            var result = SelectionInfoUtility.TryCreateUseCardContext(
                built.Manager,
                playedCard,
                action);

            Assert.That(result.TryGetValue(out var context), Is.True);
            Assert.That(
                context.SelectedCardGroups["first-group"],
                Is.EqualTo(new[]
                {
                    secondCandidate.Identity,
                    firstCandidate.Identity
                }));
            Assert.That(
                context.SelectedCardGroups["second-group"],
                Is.EqualTo(new[] { secondCandidate.Identity }));

            using (built.ContextManager.SetContext(context))
            {
                var triggerContext = new TriggerContext(
                    built.Manager,
                    new CardTrigger(playedCard),
                    new CardLookIntentAction(playedCard));
                Assert.That(
                    new SubSelectedCardCollection { SelectionId = "first-group" }
                        .Eval(triggerContext)
                        .Select(card => card.Identity),
                    Is.EqualTo(new[]
                    {
                        secondCandidate.Identity,
                        firstCandidate.Identity
                    }));
                Assert.That(
                    new SubSelectedCardCollection { SelectionId = "second-group" }
                        .Eval(triggerContext)
                        .Select(card => card.Identity),
                    Is.EqualTo(new[] { secondCandidate.Identity }));
                Assert.That(
                    new SubSelectedCardCollection { SelectionId = "missing" }
                        .Eval(triggerContext),
                    Is.Empty);
            }

            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void UseCard_ExistCardResultIsConsumedAndContextIsRestored()
        {
            const string cardId = "sub-selection-effect-consumption";
            var cardData = CardTestBuilder.CreateCardData(cardId);
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var playedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            var selectedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary,
                cardId);
            built.Ally.CardManager.HandCard.AddCard(playedCard);
            built.Ally.CardManager.HandCard.AddCard(selectedCard);
            cardData.SubSelects.Add(CreateGroup("discard", 2, selectedCard));
            cardData.Effects.Add(new DiscardCardEffect
            {
                TargetCards = new SubSelectedCardCollection
                {
                    SelectionId = "discard"
                }
            });
            var action = new UseCardAction(
                playedCard.Identity,
                MainSelectionAction.Empty,
                new Dictionary<string, ISubSelectionAction>
                {
                    ["discard"] = new ExistCardSubSelectionAction(new[]
                    {
                        selectedCard.Identity
                    })
                });
            InitializeEventBuffer(built.Manager);

            Assert.That(
                TrySetUseCardSelection(built.Manager, action, out var selectionScope),
                Is.True);
            using (selectionScope)
            {
                InvokeUseCard(built.Manager, built.Ally, playedCard.Identity);
            }

            Assert.That(
                built.Ally.CardManager.Graveyard.Cards
                    .Select(card => card.Identity),
                Does.Contain(selectedCard.Identity));
            Assert.That(built.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void TryCreateUseCardContext_WhenGroupResultIsMissing_ReturnsNone()
        {
            var cardData = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder()
                .WithCard(cardData)
                .Build();
            var playedCard = CardTestBuilder.CreateCard(
                built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(playedCard);
            cardData.SubSelects.Add(CreateGroup("required-group", 0));
            var action = new UseCardAction(
                playedCard.Identity,
                MainSelectionAction.Empty,
                new Dictionary<string, ISubSelectionAction>());

            var result = SelectionInfoUtility.TryCreateUseCardContext(
                built.Manager,
                playedCard,
                action);

            Assert.That(result.HasValue, Is.False);
        }

        private static ExistCardSelectionGroup CreateGroup(
            string id,
            int count,
            params ICardEntity[] candidates)
        {
            return new ExistCardSelectionGroup
            {
                Id = id,
                CardCandidates = new FixedCardCollection(candidates),
                SelectCount = new ConstInteger { Value = count },
                IsMustSelect = new TrueValue()
            };
        }

        private static void InitializeEventBuffer(GameplayManager manager)
        {
            typeof(GameplayManager)
                .GetField("_gameEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, new List<IGameEvent>());
        }

        private static bool TrySetUseCardSelection(
            GameplayManager manager,
            UseCardAction action,
            out IGameContextManager selectionScope)
        {
            var arguments = new object[] { action, null };
            var result = (bool)typeof(GameplayManager)
                .GetMethod(
                    "_TrySetUseCardSelection",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(manager, arguments)!;
            selectionScope = (IGameContextManager)arguments[1];
            return result;
        }

        private static void InvokeUseCard(
            GameplayManager manager,
            IPlayerEntity player,
            Guid cardIdentity)
        {
            typeof(GameplayManager)
                .GetMethod("_UseCard", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(manager, new object[] { player, cardIdentity });
        }

        private sealed class FixedCardCollection : ITargetCardCollectionValue
        {
            private readonly IReadOnlyCollection<ICardEntity> _cards;

            public FixedCardCollection(params ICardEntity[] cards)
            {
                _cards = cards;
            }

            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext triggerContext)
            {
                return _cards;
            }
        }
    }
}
