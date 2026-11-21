using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using MortalGame.GameData;
using MortalGame.GameModel;
using MortalGame.GameView;
using NUnit.Framework;
using Optional;
using Optional.Unsafe;

namespace MortalGame.Tests
{
    public sealed class CardCompositionFlowTests
    {
        [Test]
        public void Prepare_CachesOffersWhileQueryAndValidationDoNotResample()
        {
            using var a = _Create();
            using var b = _Create();
            var draftA = CardSelectionDraft.Create(a.Card, a.Built.Ally, MainSelectionAction.Empty);
            var draftB = CardSelectionDraft.Create(b.Card, b.Built.Ally, MainSelectionAction.Empty);
            var first = _Prepare(a.Built, draftA);
            var expected = _Prepare(b.Built, draftB);
            Assert.That(_Prepare(a.Built, draftA), Is.SameAs(first));
            CardSelectionQuery.QueryNext(a.Built.Manager, draftA);
            Assert.That(((CardTemplateSelectionInfo)first).TemplateCardDataIds.ToArray(),
                Is.EqualTo(((CardTemplateSelectionInfo)expected).TemplateCardDataIds.ToArray()));
            Assert.That(a.Built.ContextManager.GameRandom.Range(0, int.MaxValue),
                Is.EqualTo(b.Built.ContextManager.GameRandom.Range(0, int.MaxValue)));
        }

        [Test]
        public void Complete_RejectsOptionInPoolThatWasNotOfferedWithoutPayingOrCreatingCard()
        {
            using var setup = _Create();
            setup.Group.OfferCount = 1;
            var draft = _Template(setup);
            var offered = (CardEffectSelectionInfo)_Prepare(setup.Built, draft);
            var hidden = setup.Group.PoolsByTemplateId["template"].First(option => option.Id != offered.Options[0].Id);
            draft = draft.WithSelection("effect", new CardEffectSubSelectionAction(hidden.Id, offered.OfferIdentity));
            var energy = setup.Built.Ally.CurrentEnergy;
            Assert.That(_Use(setup.Built, setup.Built.Ally, setup.Card, draft.SelectedGroups), Is.False);
            Assert.That(setup.Built.Ally.CurrentEnergy, Is.EqualTo(energy));
            Assert.That(setup.Built.Ally.CardManager.HandCard.Cards, Is.EqualTo(new[] { setup.Card }));
            Assert.That(setup.Built.Manager.PopAllEvents(), Is.Empty);
        }

        [TestCase("caster")]
        [TestCase("card")]
        [TestCase("missing-proof")]
        [TestCase("expired")]
        [TestCase("pool-changed")]
        public void Complete_RejectsOfferFromAnotherScope(string change)
        {
            using var setup = _Create();
            var draft = _Complete(setup);
            var caster = (IPlayerEntity)setup.Built.Ally;
            var card = setup.Card;
            if (change == "caster") caster = setup.Built.Enemy;
            if (change == "card")
            {
                card = CardTestBuilder.CreateCard(setup.Built.ContextManager.CardLibrary, "source");
                setup.Built.Ally.CardManager.HandCard.AddCard(card);
            }
            if (change == "missing-proof") draft = draft with
            {
                SelectedGroups = draft.SelectedGroups.SetItem("template", new CardTemplateSubSelectionAction("template"))
            };
            if (change == "expired") setup.Session.Dispose();
            if (change == "pool-changed") setup.Group.PoolsByTemplateId["template"].Add(_Option("new", 7));
            Assert.That(CardSelectionQuery.ValidateComplete(setup.Built.Manager, card, caster,
                MainSelectionAction.Empty, draft.SelectedGroups), Is.TypeOf<CardSelectionQueryResult.Rejected>());
        }

        [Test]
        public void Prepare_OffersAllRemainingCandidatesWhenBelowConfiguredCount()
        {
            using var setup = _Create();
            setup.Group.OfferCount = 9;
            var offered = (CardEffectSelectionInfo)_Prepare(setup.Built, _Template(setup));
            Assert.That(offered.Options.Length, Is.EqualTo(2));
            Assert.That(offered.Options.Select(option => option.Id).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void ActivePlay_CreatesIndependentCardWhoseEffectsAndConditionsRunOnlyWhenPlayed()
        {
            using var setup = _Create();
            var health = setup.Built.Enemy.MainCharacter.CurrentHealth;
            Assert.That(_Use(setup.Built, setup.Built.Ally, setup.Card, _Complete(setup).SelectedGroups), Is.True);
            var composed = setup.Built.Ally.CardManager.HandCard.Cards.Single();
            Assert.That(setup.Built.Enemy.MainCharacter.CurrentHealth, Is.EqualTo(health));
            Assert.That(setup.Built.Ally.CurrentEnergy, Is.EqualTo(2));
            Assert.That(composed.OriginCardInstanceGuid.HasValue, Is.False);
            Assert.That(composed.Effects.Count(), Is.EqualTo(2));
            Assert.That(setup.Template.Effects.Count, Is.EqualTo(1));
            Assert.That(_Use(setup.Built, setup.Built.Ally, composed, new Dictionary<string, ISubSelectionAction>()), Is.True);
            Assert.That(setup.Built.Enemy.MainCharacter.CurrentHealth, Is.EqualTo(health - 6));
            Assert.That(setup.Built.ContextManager.Context, Is.SameAs(GameContext.EMPTY));
        }

        [TestCase("enemy")]
        [TestCase("effect-play")]
        [TestCase("invoke")]
        public void AutomaticEntries_CompleteCompositionAndRestoreOuterContext(string entry)
        {
            using var setup = _Create();
            var owner = entry == "enemy" ? (IPlayerEntity)setup.Built.Enemy : setup.Built.Ally;
            if (entry == "enemy")
            {
                setup.Built.Ally.CardManager.HandCard.RemoveCard(setup.Card);
                owner.CardManager.HandCard.AddCard(setup.Card);
                owner.EnergyManager.GainEnergy(3);
                var selection = SelectTargetLogic.SelectTargets(setup.Built.Manager, setup.Card, owner).ValueOrFailure();
                using var selectedSession = selection;
                Assert.That(_Use(setup.Built, owner, setup.Card, selection.SubSelectionActions, selection.MainSelectionAction), Is.True);
            }
            else
            {
                var source = new SystemExectueStartSource(owner);
                var context = new TriggerContext(setup.Built.Manager, new PlayerTrigger(owner),
                    new UpdateTimingAction(GameTiming.AfterTurnEnd, source));
                IEffectCommand command = entry == "effect-play"
                    ? new PlayCardEffectCommand(new CardPlayRequest(setup.Card.Identity, owner.Identity, source))
                    : new InvokeCardEffectsCommand(new InvokeCardEffectsRequest(setup.Card.Identity, owner.Identity,
                        CardCollectionType.HandCard, owner.Identity, source));
                setup.Built.Manager.RunEffectBatch(new[] { new EffectCommandQueueItem(context, command) });
            }
            Assert.That(owner.CardManager.HandCard.Cards.Count(card => card.CardDataId == "template"), Is.EqualTo(1));
            Assert.That(setup.Built.ContextManager.Context, Is.SameAs(GameContext.EMPTY));
            if (entry == "invoke") Assert.That(owner.CardManager.HandCard.Cards, Does.Contain(setup.Card));
        }

        [Test]
        public void CloneAndFormRestoration_PreserveCompositionWithoutSharingEffects()
        {
            using var setup = _Create();
            var definition = ComposedCardDefinition.Create(setup.Template, new[] { setup.Group.PoolsByTemplateId["template"][0] });
            var card = CardEntity.RuntimeCreateFromDefinition(definition, setup.Built.ContextManager.CardLibrary,
                setup.Built.ContextManager.CardPropertyEntityFactory);
            var clone = card.Clone();
            Assert.That(clone.Identity, Is.Not.EqualTo(card.Identity));
            ((ConstInteger)((DamageEffect)clone.Effects.Last()).Value).Value = 19;
            Assert.That(((ConstInteger)((DamageEffect)card.Effects.Last()).Value).Value, Is.EqualTo(2));
            Assert.That(card.TryApplySelfForm("form", "source", CardFormPersistence.BattleOnly).Status,
                Is.EqualTo(CardFormOperationStatus.Applied));
            Assert.That(card.DescriptionOptions, Is.Empty);
            Assert.That(card.Clone().DescriptionOptions, Is.Empty);
            card.TryRevertSelfForm("form");
            Assert.That(card.DescriptionOptions.Count(), Is.EqualTo(1));
            Assert.That(card.Effects.Count(), Is.EqualTo(2));
            card.TryApplyOverrideForm("override", "override", SystemSource.Instance,
                Array.Empty<CardFormOverrideReleaseRule>(), new Dictionary<string, IReactionSessionEntity>());
            Assert.That(card.DescriptionOptions, Is.Empty);
            card.TryRemoveOverrideForm(card.OverrideFormState.ValueOrFailure().Identity);
            Assert.That(card.DescriptionOptions.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Description_UsesActualEffectValueAndLocalizesFragmentsAtRenderTime()
        {
            using var setup = _Create();
            var option = setup.Group.PoolsByTemplateId["template"][0];
            var definition = ComposedCardDefinition.Create(setup.Template, new[] { option });
            var card = CardEntity.RuntimeCreateFromDefinition(definition, setup.Built.ContextManager.CardLibrary,
                setup.Built.ContextManager.CardPropertyEntityFactory);
            ((ConstInteger)((DamageEffect)card.Effects.Last()).Value).Value = 8;
            var info = card.ToInfo(setup.Built.Manager);
            Assert.That(CardDescriptionText.Format(info, _Localization("造成 {:damage} 點傷害")),
                Is.EqualTo("模板敘述\n造成 8 點傷害"));
            Assert.That(CardDescriptionText.Format(info, _Localization("傷害數值：{:damage}")),
                Is.EqualTo("模板敘述\n傷害數值：8"));
            Assert.That(((ConstInteger)((DamageEffect)option.Effects[0]).Value).Value, Is.EqualTo(2));
        }

        [Test]
        public void Description_DoesNotEvaluateContextDependentValues()
        {
            var option = _Option("dynamic", 2);
            var dynamicValue = new UnsafeInteger();
            ((DamageEffect)option.Effects[0]).Value = dynamicValue;
            option.DescriptionParameters["damage"] = new CardDescriptionParameter { Value = dynamicValue, RuleKey = "rule" };
            var fragment = CardDescriptionFragment.Create(option);
            Assert.That(fragment.Rules["damage"], Is.EqualTo("rule"));
            Assert.That(fragment.Values, Is.Empty);
        }

        [Test]
        public void QueuedComposition_UsesValidatedSnapshotAfterSourceChangesFormAndDoesNotLeakToOtherCards()
        {
            using var setup = _Create();
            var draft = _Complete(setup);
            var complete = (CardSelectionQueryResult.Completed)CardSelectionQuery.ValidateComplete(
                setup.Built.Manager, setup.Card, setup.Built.Ally, draft.MainSelectionAction, draft.SelectedGroups);
            var create = (CreateComposedCardEffect)setup.Built.ContextManager.CardLibrary.GetStandardCardData("source").Effects[0];
            setup.Card.TryApplySelfForm("form", "template", CardFormPersistence.BattleOnly);
            ((ConstInteger)((DamageEffect)setup.Group.PoolsByTemplateId["template"][0].Effects[0]).Value).Value = 88;
            var context = new TriggerContext(setup.Built.Manager, new CardTrigger(setup.Card),
                new InvokeCardEffectsAction(new InvokeCardEffectsSource(setup.Card, setup.Built.Ally, SystemSource.Instance)));
            using (setup.Built.ContextManager.SetContext(complete.Context))
            {
                var result = new CreateComposedCardEffectResolver().Resolve(context, create);
                var card = ((CreateCardEffectCommand)result.Commands.Single()).NewCard;
                Assert.That(((ConstInteger)((DamageEffect)card.Effects.Last()).Value).Value, Is.EqualTo(2));
                var other = CardTestBuilder.CreateCard(setup.Built.ContextManager.CardLibrary, "source");
                Assert.That(new CreateComposedCardEffectResolver().Resolve(context with { Triggered = new CardTrigger(other) }, create).Commands,
                    Is.Empty);
            }
        }

        [TestCase("dispose")]
        [TestCase("exception")]
        [TestCase("cancel")]
        [TestCase("complete")]
        [TestCase("replace")]
        public void Session_EndingLifetimeInvalidatesOffersAndKeepsCompletedDefinitionIndependent(string ending)
        {
            using var setup = _Create();
            using var cancellation = new CancellationTokenSource();
            using var session = CardSelectionSession.Begin(setup.Built.Manager, setup.Card.Identity, cancellation.Token);
            var draft = _Complete(setup);
            if (ending == "dispose") session.Dispose();
            if (ending == "cancel") cancellation.Cancel();
            if (ending == "exception")
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (session) throw new InvalidOperationException("測試選取流程中止");
                });
            if (ending == "complete")
            {
                Assert.That(SelectionInfoUtility.TryCreateCardEffectContext(setup.Built.Manager, setup.Card,
                    draft.MainSelectionAction, draft.SelectedGroups, setup.Built.Ally).TryGetValue(out var context), Is.True);
                var copy = context.ComposedCardDefinitions["template"].Definition.CopyForNewCard();
                Assert.That(copy.Data.Effects.Count, Is.EqualTo(2));
            }
            if (ending == "replace")
            {
                using var replacement = CardSelectionSession.Begin(setup.Built.Manager, setup.Card.Identity);
                session.Dispose();
                var fresh = _Complete(setup);
                Assert.That(CardSelectionQuery.ValidateComplete(setup.Built.Manager, setup.Card, setup.Built.Ally,
                    fresh.MainSelectionAction, fresh.SelectedGroups), Is.TypeOf<CardSelectionQueryResult.Completed>());
            }
            Assert.That(CardSelectionQuery.ValidateComplete(setup.Built.Manager, setup.Card, setup.Built.Ally,
                draft.MainSelectionAction, draft.SelectedGroups), Is.TypeOf<CardSelectionQueryResult.Rejected>());
            Assert.That(setup.Built.Ally.CardManager.HandCard.Cards, Does.Contain(setup.Card));
        }

        private static LocalizeLibrary _Localization(string text) => new(
            new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>
            { [LocalizeTitleInfoType.Card] = new Dictionary<string, LocalizeTitleInfoData> { ["template"] = new("模板", "模板敘述") } },
            new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>
            { [LocalizeInfoType.UI] = new Dictionary<string, LocalizeInfoData> { ["damage"] = new(text) } });

        private static SubSelectionStepInfo _Prepare(BuiltGameplay built, CardSelectionDraft draft)
            => ((CardSelectionQueryResult.NextStep)CardSelectionQuery.PrepareNext(built.Manager, draft)).Info;

        private static CardSelectionDraft _Template(Setup setup)
        {
            var draft = CardSelectionDraft.Create(setup.Card, setup.Built.Ally, MainSelectionAction.Empty);
            var templates = _Prepare(setup.Built, draft);
            return draft.WithSelection("template", new CardTemplateSubSelectionAction("template", templates.OfferIdentity));
        }

        private static CardSelectionDraft _Complete(Setup setup)
        {
            var draft = _Template(setup);
            var effects = _Prepare(setup.Built, draft);
            return draft.WithSelection("effect", new CardEffectSubSelectionAction("damage", effects.OfferIdentity));
        }

        private static bool _Use(BuiltGameplay built, IPlayerEntity owner, ICardEntity card,
            IReadOnlyDictionary<string, ISubSelectionAction> selections, MainSelectionAction main = null)
            => (bool)typeof(GameplayManager).GetMethod("_UseCard", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(built.Manager, new object[] { owner, new UseCardAction(card.Identity, main ?? MainSelectionAction.Empty, selections) });

        private static Setup _Create()
        {
            var source = CardTestBuilder.CreateCardData("source");
            source.Cost = 1;
            source.SubSelects.Add(new CardTemplateSelectionGroup { Id = "template", OfferCount = 2, TemplateCardDataIds = new() { "template" } });
            var group = new CardEffectSelectionGroup
            {
                Id = "effect", TemplateSelectionId = "template", OfferCount = 2,
                PoolsByTemplateId = new() { ["template"] = new() { _Option("damage", 2), _Option("other", 4) } }
            };
            source.SubSelects.Add(group);
            source.Effects.Add(new CreateComposedCardEffect
            {
                Target = new CardOwner { Card = new ActionCard() }, TemplateSelectionId = "template",
                CreateDestination = CardCollectionType.HandCard
            });
            var template = CardTestBuilder.CreateCardData("template");
            template.Effects.Add(_Damage(1));
            var overrideData = new OverrideCardData { ID = "override", MainSelect = new MainTargetSelectLogic { MainSelectable = new NoneSelectable() } };
            var built = new GameplayManagerTestBuilder().WithCard(source).WithCard(template).WithCard(overrideData).Build();
            typeof(GameplayManager).GetField("_gameEvents", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(built.Manager, new List<IGameEvent>());
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary, "source");
            built.Ally.CardManager.HandCard.AddCard(card);
            built.Ally.EnergyManager.GainEnergy(3);
            return new Setup(built, card, template, group, CardSelectionSession.Begin(built.Manager, card.Identity));
        }

        private static DamageEffect _Damage(int value) => new()
        {
            Targets = new CharactersOfPlayer { Player = new PlayerByFaction { Faction = Faction.Enemy } },
            Value = new ConstInteger { Value = value }
        };

        private static CardEffectOption _Option(string id, int value)
        {
            var damage = _Damage(value);
            return new CardEffectOption
            {
                Id = id, DescriptionKey = "damage", Effects = new() { damage },
                DescriptionParameters = new() { ["damage"] = new() { Value = damage.Value } },
                TriggeredEffects = new() { [CardTriggeredTiming.Played] = new[]
                {
                    new ConditionalCardEffect { Conditions = new() { new ConstCondition { Value = true } }, Effect = _Damage(3) },
                    new ConditionalCardEffect { Conditions = new() { new ConstCondition { Value = false } }, Effect = _Damage(99) }
                } }
            };
        }

        private sealed class UnsafeInteger : IIntegerValue
        {
            public Option<int> Eval(TriggerContext context) => throw new InvalidOperationException("敘述不可執行此值");
        }
        private sealed record Setup(BuiltGameplay Built, ICardEntity Card, StandardCardData Template,
            CardEffectSelectionGroup Group, CardSelectionSession Session) : IDisposable
        {
            public void Dispose() => Session.Dispose();
        }
    }
}
