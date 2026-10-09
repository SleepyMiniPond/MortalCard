using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class CardSelectionQueryTests
    {
        [TestCase(CardSelectionCountRule.Optional, 2, 3, 0, 2)]
        [TestCase(CardSelectionCountRule.Optional, 2, 1, 0, 1)]
        [TestCase(CardSelectionCountRule.Optional, 2, 0, 0, 0)]
        [TestCase(CardSelectionCountRule.RequiredAvailable, 2, 3, 2, 2)]
        [TestCase(CardSelectionCountRule.RequiredAvailable, 2, 1, 1, 1)]
        [TestCase(CardSelectionCountRule.RequiredAvailable, 2, 0, 0, 0)]
        [TestCase(CardSelectionCountRule.RequiredExact, 2, 3, 2, 2)]
        [TestCase(CardSelectionCountRule.RequiredExact, 2, 2, 2, 2)]
        [TestCase(CardSelectionCountRule.Optional, 0, 1, 0, 0)]
        [TestCase(CardSelectionCountRule.RequiredAvailable, 0, 1, 0, 0)]
        [TestCase(CardSelectionCountRule.RequiredExact, 0, 0, 0, 0)]
        public void CountRules_OfferCorrectBoundsAndValidateCommittedResults(
            CardSelectionCountRule rule, int requested, int candidates, int min, int max)
        {
            var setup = _Create(rule, requested, candidates);
            var step = (CardSelectionQueryResult.NextStep)CardSelectionQuery.QueryNext(setup.Model, setup.Draft);
            Assert.That(step.Info.MinCount, Is.EqualTo(min));
            Assert.That(step.Info.MaxCount, Is.EqualTo(max));
            Assert.That(step.Info.Id, Is.EqualTo("first"));
            Assert.That(step.Info.CardDataId, Is.EqualTo(setup.Card.CardDataId));
            for (var count = 0; count <= candidates; count++)
            {
                var selected = setup.Draft.WithSelection("first", setup.Candidates.Take(count).Select(card => card.Identity));
                Assert.That(_ValidateSubmitted(setup, selected)
                    is CardSelectionQueryResult.Completed, Is.EqualTo(count >= min && count <= max), $"選取張數：{count}");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ExactShortage_RejectsBeforeReturningAnUnfinishableStep(int candidates)
        {
            var setup = _Create(CardSelectionCountRule.RequiredExact, 2, candidates);
            _AssertFailure(CardSelectionQuery.QueryNext(setup.Model, setup.Draft), CardSelectionFailureReason.InsufficientCandidates);
            Assert.That(setup.Model.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void NextStep_UsesConfirmedPrefixAndPreservesOuterContextWithoutConsumingRandom()
        {
            var setup = _Create(CardSelectionCountRule.RequiredExact, 1, 3);
            setup.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second",
                CardCandidates = new CombinedCardCollection
                {
                    FirstCollection = new FixedCards(setup.Candidates),
                    SecondCollection = new SubSelectedCardCollection { SelectionId = "first" },
                    Operation = SetOperationType.Difference
                },
                SelectCount = new ConstInteger { Value = 1 },
                CountRule = CardSelectionCountRule.RequiredExact
            });
            var baseline = new GameRandom(1);
            var outer = GameContext.EMPTY with { SelectedCard = Guid.NewGuid() };
            using (setup.Model.ContextManager.SetContext(outer))
            {
                var first = (CardSelectionQueryResult.NextStep)CardSelectionQuery.QueryNext(setup.Model, setup.Draft);
                Assert.That(first.Info.StepIndex, Is.Zero);
                var draft = setup.Draft.WithSelection("first", new[] { setup.Candidates[0].Identity });
                var second = (CardSelectionQueryResult.NextStep)CardSelectionQuery.QueryNext(setup.Model, draft);
                Assert.That(second.Info.StepIndex, Is.EqualTo(1));
                Assert.That(second.Info.StepCount, Is.EqualTo(2));
                Assert.That(second.Info.CardInfos.Select(card => card.Identity),
                    Is.EqualTo(setup.Candidates.Skip(1).Select(card => card.Identity)));
                var complete = (CardSelectionQueryResult.Completed)_ValidateSubmitted(setup,
                    draft.WithSelection("second", new[] { setup.Candidates[1].Identity }));
                Assert.That(complete.Context.SelectedCard, Is.EqualTo(Guid.Empty));
                Assert.That(complete.Context.SelectedCardGroups["first"], Is.EqualTo(new[] { setup.Candidates[0].Identity }));
                Assert.That(setup.Model.ContextManager.Context, Is.SameAs(outer));
                Assert.That(Enumerable.Range(0, 10).Shuffle(setup.Model.ContextManager.GameRandom),
                    Is.EqualTo(Enumerable.Range(0, 10).Shuffle(baseline)));
            }
        }

        [Test]
        public void Prefix_ControlsNextCountAndCanCauseAnExactShortage()
        {
            var setup = _Create(CardSelectionCountRule.Optional, 2, 2);
            setup.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", CardCandidates = new FixedCards(setup.Candidates.Take(1).ToArray()),
                SelectCount = new SelectedCount("first"), CountRule = CardSelectionCountRule.RequiredExact
            });
            var empty = setup.Draft.WithSelection("first", Array.Empty<Guid>());
            var zero = (CardSelectionQueryResult.NextStep)CardSelectionQuery.QueryNext(setup.Model, empty);
            Assert.That(zero.Info.MaxCount, Is.Zero);
            var full = setup.Draft.WithSelection("first", setup.Candidates.Select(card => card.Identity));
            _AssertFailure(CardSelectionQuery.QueryNext(setup.Model, full), CardSelectionFailureReason.InsufficientCandidates);
        }

        [Test]
        public void MissingZeroCountGroup_IsNotAConfirmedEmptyGroup()
        {
            var setup = _Create(CardSelectionCountRule.Optional, 0, 0);
            _AssertFailure(_ValidateSubmitted(setup, setup.Draft), CardSelectionFailureReason.MissingGroup);
            var confirmed = setup.Draft.WithSelection("first", Array.Empty<Guid>());
            Assert.That(_ValidateSubmitted(setup, confirmed), Is.TypeOf<CardSelectionQueryResult.Completed>());
            Assert.That(setup.Draft.SelectedGroups.ContainsKey("first"), Is.False);
        }

        [TestCase("duplicate")]
        [TestCase("foreign")]
        [TestCase("excess")]
        [TestCase("short")]
        [TestCase("extra-group")]
        public void InvalidCommittedInput_IsRejectedWithoutNormalization(string input)
        {
            var setup = _Create(CardSelectionCountRule.RequiredExact, 1, 2);
            var first = setup.Candidates[0].Identity;
            var ids = input switch
            {
                "duplicate" => new[] { first, first },
                "foreign" => new[] { Guid.NewGuid() },
                "excess" => setup.Candidates.Select(card => card.Identity).ToArray(),
                "short" => Array.Empty<Guid>(),
                _ => new[] { first }
            };
            var draft = setup.Draft.WithSelection(input == "extra-group" ? "unknown" : "first", ids);
            Assert.That(_ValidateSubmitted(setup, draft), Is.TypeOf<CardSelectionQueryResult.Rejected>());
            Assert.That(setup.Model.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [Test]
        public void Draft_CopiesResultsAndRejectsSkippedPrefix()
        {
            var setup = _Create(CardSelectionCountRule.Optional, 1, 1);
            setup.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", SelectCount = new ConstInteger { Value = 0 },
                CardCandidates = new FixedCards(Array.Empty<ICardEntity>())
            });
            _AssertFailure(CardSelectionQuery.QueryNext(setup.Model,
                setup.Draft.WithSelection("second", Array.Empty<Guid>())), CardSelectionFailureReason.OutOfOrder);
            var ids = new List<Guid> { setup.Candidates[0].Identity };
            var draft = setup.Draft.WithSelection("first", ids);
            ids.Clear();
            Assert.That(draft.SelectedGroups["first"].Length, Is.EqualTo(1));
        }

        [Test]
        public void Revalidation_RecomputesPrefixAgainstCurrentCandidates()
        {
            var setup = _Create(CardSelectionCountRule.Optional, 1, 1);
            var group = (ExistCardSelectionGroup)setup.Data.SubSelects[0];
            var draft = setup.Draft.WithSelection("first", new[] { setup.Candidates[0].Identity });
            Assert.That(_ValidateSubmitted(setup, draft), Is.TypeOf<CardSelectionQueryResult.Completed>());
            group.CardCandidates = new FixedCards(Array.Empty<ICardEntity>());
            _AssertFailure(_ValidateSubmitted(setup, draft), CardSelectionFailureReason.InvalidSelection);
        }

        [Test]
        public void CompleteValidation_FollowsConfigurationOrderAndAllowsCrossGroupReuse()
        {
            var setup = _Create(CardSelectionCountRule.RequiredExact, 1, 1);
            setup.Data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "second", SelectCount = new ConstInteger { Value = 1 },
                CardCandidates = new SubSelectedCardCollection { SelectionId = "first" },
                CountRule = CardSelectionCountRule.RequiredExact
            });
            var ids = new[] { setup.Candidates[0].Identity };
            var actions = new Dictionary<string, ISubSelectionAction>
            {
                ["second"] = new ExistCardSubSelectionAction(ids),
                ["first"] = new ExistCardSubSelectionAction(ids)
            };
            Assert.That(CardSelectionQuery.ValidateComplete(setup.Model, setup.Card, setup.Caster,
                MainSelectionAction.Empty, actions), Is.TypeOf<CardSelectionQueryResult.Completed>());
            actions["first"] = new NewCardSubSelectionAction();
            _AssertFailure(CardSelectionQuery.ValidateComplete(setup.Model, setup.Card, setup.Caster,
                MainSelectionAction.Empty, actions), CardSelectionFailureReason.InvalidSelection);
        }

        [Test]
        public void Exceptions_RestoreOuterContext()
        {
            var setup = _Create(CardSelectionCountRule.Optional, 1, 1);
            ((ExistCardSelectionGroup)setup.Data.SubSelects[0]).CardCandidates =
                new FixedCards(Array.Empty<ICardEntity>(), throwOnEval: true);
            var outer = GameContext.EMPTY with { SelectedCard = Guid.NewGuid() };
            using (setup.Model.ContextManager.SetContext(outer))
            {
                Assert.Throws<InvalidOperationException>(() => CardSelectionQuery.QueryNext(setup.Model, setup.Draft));
                Assert.That(setup.Model.ContextManager.Context, Is.SameAs(outer));
            }
        }

        [TestCase(SetOperationType.Difference, new[] { 2, 0 })]
        [TestCase(SetOperationType.Union, new[] { 2, 0, 1, 3 })]
        [TestCase(SetOperationType.Intersection, new[] { 1 })]
        public void CombinedCollections_UseIdentityAndPreserveOrder(SetOperationType operation, int[] expected)
        {
            var setup = _Create(CardSelectionCountRule.Optional, 1, 4);
            var cards = setup.Candidates;
            var collection = new CombinedCardCollection
            {
                FirstCollection = new FixedCards(new[] { cards[2], cards[0], cards[2], cards[1] }),
                SecondCollection = new FixedCards(new[] { cards[1], cards[3], cards[1] }),
                Operation = operation
            };
            Assert.That(collection.Eval(null).Select(card => card.Identity),
                Is.EqualTo(expected.Select(index => cards[index].Identity)));
        }

        [TestCase(SetOperationType.Difference, false, new[] { 0, 1 })]
        [TestCase(SetOperationType.Difference, true, new int[0])]
        [TestCase(SetOperationType.Union, false, new[] { 0, 1 })]
        [TestCase(SetOperationType.Union, true, new[] { 0, 1 })]
        [TestCase(SetOperationType.Intersection, false, new int[0])]
        [TestCase(SetOperationType.Intersection, true, new int[0])]
        public void CombinedCollections_HandleEmptyOperands(SetOperationType operation, bool firstEmpty, int[] expected)
        {
            var setup = _Create(CardSelectionCountRule.Optional, 1, 2);
            var collection = new CombinedCardCollection
            {
                FirstCollection = new FixedCards(firstEmpty ? Array.Empty<ICardEntity>() : setup.Candidates),
                SecondCollection = new FixedCards(firstEmpty ? setup.Candidates : Array.Empty<ICardEntity>()),
                Operation = operation
            };
            Assert.That(collection.Eval(null).Select(card => card.Identity),
                Is.EqualTo(expected.Select(index => setup.Candidates[index].Identity)));
        }

        [Test]
        public void CombinedCollections_RejectInvalidOperation()
        {
            var collection = new CombinedCardCollection
            {
                FirstCollection = new FixedCards(Array.Empty<ICardEntity>()),
                SecondCollection = new FixedCards(Array.Empty<ICardEntity>()),
                Operation = (SetOperationType)999
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Eval(null));
        }

        [Test]
        public void InvalidSource_IsRejected()
        {
            var setup = _Create(CardSelectionCountRule.Optional, 1, 1);
            _AssertFailure(CardSelectionQuery.QueryNext(setup.Model, setup.Draft with { CasterIdentity = Guid.NewGuid() }),
                CardSelectionFailureReason.InvalidSource);
        }

        private static Setup _Create(CardSelectionCountRule rule, int requested, int candidateCount)
        {
            var data = CardTestBuilder.CreateCardData();
            var built = new GameplayManagerTestBuilder().WithCard(data).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var candidates = Enumerable.Range(0, candidateCount)
                .Select(_ => (ICardEntity)CardTestBuilder.CreateCard(built.ContextManager.CardLibrary)).ToArray();
            foreach (var candidate in candidates) built.Ally.CardManager.HandCard.AddCard(candidate);
            data.SubSelects.Add(new ExistCardSelectionGroup
            {
                Id = "first", SelectCount = new ConstInteger { Value = requested },
                CardCandidates = new FixedCards(candidates), CountRule = rule
            });
            return new Setup(built.Manager, data, card, built.Ally, candidates,
                CardSelectionDraft.Create(card, built.Ally, MainSelectionAction.Empty));
        }

        private static CardSelectionQueryResult _ValidateSubmitted(Setup setup, CardSelectionDraft draft)
        {
            var actions = draft.SelectedGroups.ToDictionary(
                pair => pair.Key,
                pair => (ISubSelectionAction)new ExistCardSubSelectionAction(pair.Value));
            return CardSelectionQuery.ValidateComplete(
                setup.Model, setup.Card, setup.Caster, draft.MainSelectionAction, actions);
        }

        private static void _AssertFailure(CardSelectionQueryResult result, CardSelectionFailureReason reason)
        {
            Assert.That(result, Is.TypeOf<CardSelectionQueryResult.Rejected>());
            Assert.That(((CardSelectionQueryResult.Rejected)result).Failure.Reason, Is.EqualTo(reason));
        }

        private record Setup(IGameplayModel Model, StandardCardData Data, ICardEntity Card,
            IPlayerEntity Caster, ICardEntity[] Candidates, CardSelectionDraft Draft);

        private sealed class FixedCards : ITargetCardCollectionValue
        {
            private readonly ICardEntity[] _cards;
            private readonly bool _throwOnEval;

            public FixedCards(ICardEntity[] cards, bool throwOnEval = false)
            {
                _cards = cards;
                _throwOnEval = throwOnEval;
            }

            public IReadOnlyCollection<ICardEntity> Eval(TriggerContext context)
                => _throwOnEval ? throw new InvalidOperationException("測試查詢中斷") : _cards;
        }

        private sealed class SelectedCount : IIntegerValue
        {
            private readonly string _id;
            public SelectedCount(string id) => _id = id;
            public Optional.Option<int> Eval(TriggerContext context)
                => Optional.Option.Some(context.Model.ContextManager.Context.SelectedCardGroups[_id].Length);
        }
    }
}
