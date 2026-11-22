using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.Editor;
using MortalGame.GameData;
using NUnit.Framework;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class EffectMenuSettingsTests
    {
        private EffectMenuSettings _settings;
        private string _assetPath;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<EffectMenuSettings>();

        [TearDown]
        public void TearDown()
        {
            if (_assetPath != null) AssetDatabase.DeleteAsset(_assetPath);
            else if (_settings != null) UnityEngine.Object.DestroyImmediate(_settings);
        }

        [Test]
        public void UnlistedTimingAndEffect_AreAllowed()
        {
            _settings.GameTimingRules.Add(_Rule(GameTiming.BeforeGameStart, typeof(DrawCardEffect)));

            Assert.That(_settings.IsDisabled(GameTiming.BeforeGameStart, typeof(GainEnergyEffect)), Is.False);
            Assert.That(_settings.IsDisabled(GameTiming.AfterGameStart, typeof(DrawCardEffect)), Is.False);
        }

        [Test]
        public void RepeatedTimingRules_CombineDisabledTypes()
        {
            _settings.GameTimingRules.Add(_Rule(GameTiming.BeforeGameStart, typeof(DrawCardEffect)));
            _settings.GameTimingRules.Add(_Rule(GameTiming.BeforeGameStart, typeof(GainEnergyEffect), typeof(DrawCardEffect)));

            Assert.That(_settings.IsDisabled(GameTiming.BeforeGameStart, typeof(DrawCardEffect)), Is.True);
            Assert.That(_settings.IsDisabled(GameTiming.BeforeGameStart, typeof(GainEnergyEffect)), Is.True);
        }

        [Test]
        public void TimingEnumsWithSameNumericValue_DoNotShareRules()
        {
            _settings.GameTimingRules.Add(_Rule(GameTiming.BeforeGameStart, typeof(DrawCardEffect)));

            Assert.That((int)GameTiming.BeforeGameStart, Is.EqualTo((int)CardTriggeredTiming.Drawed));
            Assert.That(_settings.IsDisabled(CardTriggeredTiming.Drawed, typeof(DrawCardEffect)), Is.False);
        }

        [Test]
        public void EditingSettings_ImmediatelyChangesMenuAndRejectsStaleSelection()
        {
            var data = new CardBuffData();
            data.BuffEffects[GameTiming.AfterGameStart] = new[] { new ConditionalCardBuffEffect() };
            using var tree = PropertyTree.Create(data);
            var property = _Effect(tree);
            Assert.That(_Enabled(property, typeof(GainEnergyEffect)), Is.True);

            _settings.GameTimingRules.Add(_Rule(GameTiming.AfterGameStart, typeof(GainEnergyEffect)));

            Assert.That(_Enabled(property, typeof(GainEnergyEffect)), Is.False);
            Assert.That(EffectMenuFilter.GetOptions(property, _settings).Last().Type, Is.EqualTo(typeof(GainEnergyEffect)));
            Assert.That(EffectMenuFilter.TrySetEffect(property, typeof(GainEnergyEffect), _settings), Is.False);
            Assert.That(data.BuffEffects[GameTiming.AfterGameStart][0].Effect, Is.Null);

            _settings.GameTimingRules.Clear();
            Assert.That(_Enabled(property, typeof(GainEnergyEffect)), Is.True);
        }

        [Test]
        public void CardTimingRule_CanDisableAnotherEffectWithoutAffectingOtherTimings()
        {
            _settings.CardTimingRules.Add(new EffectMenuRule<CardTriggeredTiming>
            {
                Timing = CardTriggeredTiming.Drawed,
                DisabledEffects = new() { typeof(GainEnergyEffect) }
            });
            var data = CardTestBuilder.CreateCardData();
            data.TriggeredEffects[CardTriggeredTiming.Drawed] = new[] { new ConditionalCardEffect() };
            using var tree = PropertyTree.Create(data);

            Assert.That(_Enabled(_Effect(tree), typeof(GainEnergyEffect)), Is.False);
            Assert.That(_Enabled(_Effect(tree), typeof(DrawCardEffect)), Is.True);
            Assert.That(_settings.IsDisabled(CardTriggeredTiming.Initialize, typeof(GainEnergyEffect)), Is.False);
        }

        [Test]
        public void SavedAsset_ReimportPreservesTimingAndEffectTypes()
        {
            _settings.GameTimingRules.Add(_Rule(GameTiming.AfterGameStart, typeof(DrawCardEffect), typeof(GainEnergyEffect)));
            _settings.CardTimingRules.Add(new EffectMenuRule<CardTriggeredTiming>
            {
                Timing = CardTriggeredTiming.Initialize,
                DisabledEffects = new() { typeof(CreateCardEffect) }
            });
            _assetPath = "Assets/Tests/EditMode/EffectMenuSettings_" + Guid.NewGuid().ToString("N") + ".asset";
            AssetDatabase.CreateAsset(_settings, _assetPath);
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssetIfDirty(_settings);
            Resources.UnloadAsset(_settings);
            _settings = null;
            AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate);

            var reloaded = AssetDatabase.LoadAssetAtPath<EffectMenuSettings>(_assetPath);

            Assert.That(reloaded.GameTimingRules.Single().Timing, Is.EqualTo(GameTiming.AfterGameStart));
            Assert.That(reloaded.GameTimingRules.Single().DisabledEffects,
                Is.EqualTo(new[] { typeof(DrawCardEffect), typeof(GainEnergyEffect) }));
            Assert.That(reloaded.CardTimingRules.Single().Timing, Is.EqualTo(CardTriggeredTiming.Initialize));
            Assert.That(reloaded.IsDisabled(CardTriggeredTiming.Initialize, typeof(CreateCardEffect)), Is.True);
        }

        [Test]
        public void EffectTypeDropdown_ContainsConcreteEffectsAsTypes()
        {
            var choices = EffectMenuSettings.EffectTypes.ToArray();
            Assert.That(choices.Select(item => item.Value), Does.Contain(typeof(DrawCardEffect)));
            Assert.That(choices.Select(item => item.Value), Does.Contain(typeof(CreateCardEffect)));
            Assert.That(choices.All(item => item.Value.IsAbstract == false && item.Value.IsInterface == false), Is.True);
        }

        private bool _Enabled(InspectorProperty property, Type type) =>
            EffectMenuFilter.GetOptions(property, _settings).Single(option => option.Type == type).Enabled;

        private static EffectMenuRule<GameTiming> _Rule(GameTiming timing, params Type[] types) =>
            new() { Timing = timing, DisabledEffects = types.ToList() };

        private static InspectorProperty _Effect(PropertyTree tree)
        {
            tree.UpdateTree();
            return _Find(tree.RootProperty).First();
        }

        private static IEnumerable<InspectorProperty> _Find(InspectorProperty property)
        {
            if (property.Name == "Effect") yield return property;
            foreach (var child in property.Children)
                foreach (var item in _Find(child)) yield return item;
        }
    }
}
