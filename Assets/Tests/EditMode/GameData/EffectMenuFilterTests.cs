using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;


namespace MortalGame.Tests
{
    public sealed class EffectMenuFilterTests
    {
        [TestCase(GameTiming.BeforeGameStart, false)]
        [TestCase(GameTiming.AfterGameStart, true)]
        [TestCase(GameTiming.BeforeDrawCard, true)]
        public void CardBuffMenu_FiltersDrawOnlyBeforeInitialOrder(GameTiming timing, bool allowsDraw)
        {
            var data = new CardBuffData();
            data.BuffEffects[timing] = new[] { new ConditionalCardBuffEffect() };
            using var tree = PropertyTree.Create(data);
            var property = _Effect(tree);

            var types = _EnabledTypes(property).ToArray();

            Assert.That(types.Contains(typeof(DrawCardEffect)), Is.EqualTo(allowsDraw));
            Assert.That(types, Does.Contain(typeof(GainEnergyEffect)));
            Assert.That(types.All(type => typeof(ICardBuffEffect).IsAssignableFrom(type)), Is.True);
        }

        [TestCase(CardTriggeredTiming.Initialize, false)]
        [TestCase(CardTriggeredTiming.Drawed, true)]
        public void CardBuffLifecycleMenu_UsesCardTimingKey(CardTriggeredTiming timing, bool allowsDraw)
        {
            var data = new CardBuffData();
            data.Effects[timing] = new[] { new ConditionalCardBuffEffect() };
            using var tree = PropertyTree.Create(data);

            Assert.That(_EnabledTypes(_Effect(tree)).Contains(typeof(DrawCardEffect)), Is.EqualTo(allowsDraw));
        }

        [Test]
        public void CardInitializeMenu_UsesNestedDictionaryKey()
        {
            var data = CardTestBuilder.CreateCardData();
            data.TriggeredEffects[CardTriggeredTiming.Initialize] = new[] { new ConditionalCardEffect() };
            using var tree = PropertyTree.Create(data);

            Assert.That(_EnabledTypes(_Effect(tree)), Has.No.Member(typeof(DrawCardEffect)));
        }

        [Test]
        public void BuffMenus_ApplySameRuleToPlayerAndCharacter()
        {
            var player = new PlayerBuffData();
            player.BuffEffects[GameTiming.BeforeGameStart] = new[] { new ConditionalPlayerBuffEffect() };
            var character = new CharacterBuffData();
            character.BuffEffects[GameTiming.BeforeGameStart] = new[] { new ConditionalCharacterBuffEffect() };
            using var playerTree = PropertyTree.Create(player);
            using var characterTree = PropertyTree.Create(character);

            Assert.That(_EnabledTypes(_Effect(playerTree)), Has.No.Member(typeof(DrawCardEffect)));
            Assert.That(_EnabledTypes(_Effect(characterTree)), Has.No.Member(typeof(DrawCardEffect)));
        }

        [Test]
        public void ExistingDrawEffect_RemainsIntactAndStillOffersOtherEffectTypes()
        {
            var draw = new DrawCardEffect { Value = new MortalGame.GameModel.ConstInteger { Value = 3 } };
            var data = new CardBuffData();
            var conditional = new ConditionalCardBuffEffect { Effect = draw };
            data.BuffEffects[GameTiming.BeforeGameStart] = new[] { conditional };
            using var tree = PropertyTree.Create(data);

            var types = _EnabledTypes(_Effect(tree)).ToArray();

            Assert.That(types, Does.Contain(typeof(GainEnergyEffect)));
            Assert.That(types, Has.No.Member(typeof(DrawCardEffect)));
            Assert.That(conditional.Effect, Is.SameAs(draw));
            Assert.That(((MortalGame.GameModel.ConstInteger)draw.Value).Value, Is.EqualTo(3));
        }

        [Test]
        public void RestrictedDraw_IsDisabledAndPlacedLastInActualSelector()
        {
            var data = new CardBuffData();
            data.BuffEffects[GameTiming.BeforeGameStart] = new[] { new ConditionalCardBuffEffect() };
            using var tree = PropertyTree.Create(data);
            var options = EffectMenuFilter.GetOptions(_Effect(tree));
            var selector = new EffectMenuSelector(options);
            var item = selector.SelectionTree.MenuItems.Last();

            Assert.That(options.Last().Type, Is.EqualTo(typeof(DrawCardEffect)));
            Assert.That(options.Last().Enabled, Is.False);
            Assert.That(item.Name, Is.EqualTo(nameof(DrawCardEffect)));
            Assert.That(item.IsEnabled, Is.False);
            Assert.That(item.IsSelectable, Is.False);
        }

        [Test]
        public void ChangingTiming_UpdatesFilterWithoutReplacingExistingEffect()
        {
            var draw = new DrawCardEffect();
            var conditional = new ConditionalCardBuffEffect { Effect = draw };
            var data = new CardBuffData();
            data.BuffEffects[GameTiming.AfterGameStart] = new[] { conditional };
            using var tree = PropertyTree.Create(data);
            Assert.That(_EnabledTypes(_Effect(tree)), Does.Contain(typeof(DrawCardEffect)));

            data.BuffEffects.Remove(GameTiming.AfterGameStart);
            data.BuffEffects[GameTiming.BeforeGameStart] = new[] { conditional };

            Assert.That(_EnabledTypes(_Effect(tree)), Has.No.Member(typeof(DrawCardEffect)));
            Assert.That(conditional.Effect, Is.SameAs(draw));
        }

        [Test]
        public void RestrictedSelection_DoesNotReplaceExistingEffect()
        {
            var data = new CardBuffData();
            var existing = new GainEnergyEffect();
            data.BuffEffects[GameTiming.BeforeGameStart] = new[] { new ConditionalCardBuffEffect { Effect = existing } };
            using var tree = PropertyTree.Create(data);
            var property = _Effect(tree);

            Assert.That(EffectMenuFilter.TrySetEffect(property, typeof(DrawCardEffect)), Is.False);
            Assert.That(data.BuffEffects[GameTiming.BeforeGameStart][0].Effect, Is.SameAs(existing));
            Assert.That(property.GetAttribute<TypeFilterAttribute>(), Is.Null);
            Assert.That(property.GetAttribute<EffectMenuFilterAttribute>(), Is.Not.Null);
        }

        [Test]
        public void AllowedSelection_InstantiatesAndClearsEffect()
        {
            var data = new CardBuffData();
            data.BuffEffects[GameTiming.AfterGameStart] = new[] { new ConditionalCardBuffEffect() };
            using var tree = PropertyTree.Create(data);
            var property = _Effect(tree);

            Assert.That(EffectMenuFilter.TrySetEffect(property, typeof(DrawCardEffect)), Is.True);
            Assert.That(data.BuffEffects[GameTiming.AfterGameStart][0].Effect, Is.TypeOf<DrawCardEffect>());
            property = _Effect(tree);
            Assert.That(EffectMenuFilter.TrySetEffect(property, null), Is.True);
            Assert.That(data.BuffEffects[GameTiming.AfterGameStart][0].Effect, Is.Null);
        }

        private static IEnumerable<Type> _EnabledTypes(InspectorProperty property) =>
            EffectMenuFilter.GetOptions(property).Where(option => option.Enabled && option.Type != null).Select(option => option.Type);

        private static InspectorProperty _Effect(PropertyTree tree)
        {
            tree.UpdateTree();
            return _Effects(tree.RootProperty).First();
        }

        private static IEnumerable<InspectorProperty> _Effects(InspectorProperty property)
        {
            if (property.Name == "Effect") yield return property;
            foreach (var child in property.Children)
                foreach (var effect in _Effects(child))
                    yield return effect;
        }
    }
}
