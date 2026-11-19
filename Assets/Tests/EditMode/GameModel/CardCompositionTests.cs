using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;

namespace MortalGame.Tests
{
    public sealed class CardCompositionTests
    {
        [TestCase("template-a", "damage")]
        [TestCase("template-b", "shield")]
        public void Query_UsesSelectedTemplatePoolAndConfiguredOfferCount(string templateId, string expected)
        {
            var setup = _Create();
            var initial = (CardTemplateSelectionInfo)((CardSelectionQueryResult.NextStep)
                CardSelectionQuery.QueryNext(setup.Model, setup.Draft)).Info;
            Assert.That(initial.OfferCount, Is.EqualTo(2));
            var draft = setup.Draft.WithSelection("template", new CardTemplateSubSelectionAction(templateId));
            var info = (CardEffectSelectionInfo)((CardSelectionQueryResult.NextStep)
                CardSelectionQuery.QueryNext(setup.Model, draft)).Info;
            Assert.That(info.Options.Select(option => option.Id), Is.EqualTo(new[] { expected }));
            Assert.That(info.OfferCount, Is.EqualTo(3));
            Assert.That(info.StepIndex, Is.EqualTo(1));
            Assert.That(info.StepCount, Is.EqualTo(2));
        }

        [TestCase(true, "shield")]
        [TestCase(false, "damage,shield")]
        public void Query_ExcludesPreviouslySelectedOptionIds(bool exclude, string expected)
        {
            var setup = _Create();
            setup.Data.SubSelects.Add(new CardEffectSelectionGroup
            {
                Id = "second", TemplateSelectionId = "template", OfferCount = 2, ExcludePreviouslySelected = exclude,
                PoolsByTemplateId = new()
                {
                    ["template-a"] = new() { _Option("damage"), _Option("shield") },
                    ["template-b"] = new() { _Option("shield") }
                }
            });
            var draft = setup.Draft
                .WithSelection("template", new CardTemplateSubSelectionAction("template-a"))
                .WithSelection("first", new CardEffectSubSelectionAction("damage"));
            var info = (CardEffectSelectionInfo)((CardSelectionQueryResult.NextStep)
                CardSelectionQuery.QueryNext(setup.Model, draft)).Info;
            Assert.That(string.Join(",", info.Options.Select(option => option.Id)), Is.EqualTo(expected));
        }

        [Test]
        public void Query_RejectsEmptyPoolAfterExclusionWithoutChangingBattleState()
        {
            var setup = _Create();
            setup.Data.SubSelects.Add(new CardEffectSelectionGroup
            {
                Id = "second", TemplateSelectionId = "template", OfferCount = 1,
                PoolsByTemplateId = new() { ["template-a"] = new() { _Option("damage") } }
            });
            var draft = setup.Draft
                .WithSelection("template", new CardTemplateSubSelectionAction("template-a"))
                .WithSelection("first", new CardEffectSubSelectionAction("damage"));
            var energy = setup.Player.CurrentEnergy;
            var count = setup.Player.CardManager.HandCard.Cards.Count;
            var result = (CardSelectionQueryResult.Rejected)CardSelectionQuery.QueryNext(setup.Model, draft);
            Assert.That(result.Failure.Reason, Is.EqualTo(CardSelectionFailureReason.InsufficientCandidates));
            Assert.That(setup.Player.CurrentEnergy, Is.EqualTo(energy));
            Assert.That(setup.Player.CardManager.HandCard.Cards.Count, Is.EqualTo(count));
            Assert.That(setup.Model.ContextManager.Context, Is.EqualTo(GameContext.EMPTY));
        }

        [TestCase("foreign-template")]
        [TestCase("foreign-option")]
        [TestCase("wrong-type")]
        [TestCase("missing-group")]
        [TestCase("extra-group")]
        [TestCase("out-of-order")]
        public void Complete_RejectsMalformedInput(string input)
        {
            var setup = _Create();
            var actions = new Dictionary<string, ISubSelectionAction>
            {
                ["template"] = new CardTemplateSubSelectionAction("template-a"),
                ["first"] = new CardEffectSubSelectionAction("damage")
            };
            switch (input)
            {
                case "foreign-template": actions["template"] = new CardTemplateSubSelectionAction("unknown"); break;
                case "foreign-option": actions["first"] = new CardEffectSubSelectionAction("shield"); break;
                case "wrong-type": actions["first"] = new CardTemplateSubSelectionAction("template-a"); break;
                case "missing-group": actions.Remove("first"); break;
                case "extra-group": actions["extra"] = new CardEffectSubSelectionAction("damage"); break;
                case "out-of-order": actions.Remove("template"); break;
            }
            Assert.That(CardSelectionQuery.ValidateComplete(setup.Model, setup.Card, setup.Player,
                MainSelectionAction.Empty, actions), Is.TypeOf<CardSelectionQueryResult.Rejected>());
        }

        [Test]
        public void Complete_PreservesTypedSnapshotsAndDoesNotInheritOuterSelectionOrConsumeRandom()
        {
            var setup = _Create();
            var outer = GameContext.EMPTY with
            {
                SelectedTemplateGroups = ImmutableDictionary<string, string>.Empty.Add("outer", "template-b"),
                SelectedEffectGroups = ImmutableDictionary<string, SelectedCardEffect>.Empty
                    .Add("outer", new SelectedCardEffect("outer", "damage"))
            };
            using (setup.Model.ContextManager.SetContext(outer))
            {
                var draft = setup.Draft
                    .WithSelection("template", new CardTemplateSubSelectionAction("template-a"))
                    .WithSelection("first", new CardEffectSubSelectionAction("damage"));
                var completed = (CardSelectionQueryResult.Completed)CardSelectionQuery.ValidateComplete(
                    setup.Model, setup.Card, setup.Player, MainSelectionAction.Empty, draft.SelectedGroups);
                Assert.That(completed.Context.SelectedTemplateGroups.Count, Is.EqualTo(1));
                Assert.That(completed.Context.SelectedEffectGroups["first"],
                    Is.EqualTo(new SelectedCardEffect("template", "damage")));
                Assert.That(completed.Context.SelectedCardGroups, Is.Empty);
                Assert.That(setup.Model.ContextManager.Context, Is.SameAs(outer));
                Assert.That(Enumerable.Range(0, 10).Shuffle(setup.Model.ContextManager.GameRandom),
                    Is.EqualTo(Enumerable.Range(0, 10).Shuffle(new GameRandom(1))));
            }
        }

        [Test]
        public void Definition_AppendsEffectsAndTriggeredConditionsWithoutChangingTemplateFields()
        {
            var template = CardTestBuilder.CreateCardData("template");
            template.Cost = 2;
            template.Effects.Add(new DrawCardEffect());
            var condition = new ConstCondition { Value = true };
            template.TriggeredEffects[CardTriggeredTiming.Played] = new[]
            {
                new ConditionalCardEffect { Effect = new DrawCardEffect() }
            };
            var option = _Option("damage");
            option.Effects.Add(new DamageEffect { Value = new ConstInteger { Value = 3 } });
            option.TriggeredEffects[CardTriggeredTiming.Played] = new[]
            {
                new ConditionalCardEffect { Conditions = new() { condition }, Effect = new DrawCardEffect() }
            };
            var definition = ComposedCardDefinition.Create(template, new[] { option, option });
            var snapshot = definition.CreateSnapshot();
            Assert.That(snapshot.Data.Cost, Is.EqualTo(2));
            Assert.That(snapshot.Data.MainSelect, Is.Not.SameAs(template.MainSelect));
            Assert.That(snapshot.Data.Effects.Count, Is.EqualTo(3));
            Assert.That(snapshot.Data.TriggeredEffects[CardTriggeredTiming.Played].Length, Is.EqualTo(3));
            Assert.That(snapshot.Data.TriggeredEffects[CardTriggeredTiming.Played][1].Conditions.Count, Is.EqualTo(1));
            Assert.That(snapshot.SelectedOptions.Select(item => item.DescriptionKey),
                Is.EqualTo(new[] { "Description.damage", "Description.damage" }));
            Assert.That(template.Effects.Count, Is.EqualTo(1));
            Assert.That(template.TriggeredEffects[CardTriggeredTiming.Played].Length, Is.EqualTo(1));
        }

        [Test]
        public void Definition_CopiesNestedEffectDataAndKeepsDescriptionSourceLinkedToSnapshotEffects()
        {
            var template = CardTestBuilder.CreateCardData("template");
            var option = _Option("damage");
            var value = new ConstInteger { Value = 3 };
            option.Effects.Add(new DamageEffect { Value = value });
            var definition = ComposedCardDefinition.Create(template, new[] { option });
            value.Value = 99;
            option.DescriptionKey = "changed";
            template.Cost = 9;
            var snapshot = definition.CreateSnapshot();
            var damage = (DamageEffect)snapshot.Data.Effects[0];
            Assert.That(((ConstInteger)damage.Value).Value, Is.EqualTo(3));
            Assert.That(snapshot.Data.Cost, Is.Zero);
            Assert.That(snapshot.SelectedOptions[0].DescriptionKey, Is.EqualTo("Description.damage"));
            Assert.That(snapshot.SelectedOptions[0].Effects[0], Is.SameAs(damage));
            ((ConstInteger)damage.Value).Value = 20;
            Assert.That(((ConstInteger)((DamageEffect)definition.CreateSnapshot().Data.Effects[0]).Value).Value,
                Is.EqualTo(3));
        }

        private static CardEffectOption _Option(string id) => new() { Id = id, DescriptionKey = "Description." + id };

        private static Setup _Create()
        {
            var data = CardTestBuilder.CreateCardData();
            data.SubSelects.Add(new CardTemplateSelectionGroup
            {
                Id = "template", OfferCount = 2, TemplateCardDataIds = new() { "template-a", "template-b" }
            });
            data.SubSelects.Add(new CardEffectSelectionGroup
            {
                Id = "first", TemplateSelectionId = "template", OfferCount = 3,
                PoolsByTemplateId = new()
                {
                    ["template-a"] = new() { _Option("damage") },
                    ["template-b"] = new() { _Option("shield") }
                }
            });
            var built = new GameplayManagerTestBuilder().WithCard(data)
                .WithCard(CardTestBuilder.CreateCardData("template-a"))
                .WithCard(CardTestBuilder.CreateCardData("template-b")).Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            return new Setup(built.Manager, data, card, built.Ally,
                CardSelectionDraft.Create(card, built.Ally, MainSelectionAction.Empty));
        }

        private sealed record Setup(IGameplayModel Model, StandardCardData Data,
            ICardEntity Card, IPlayerEntity Player, CardSelectionDraft Draft);
    }
}
