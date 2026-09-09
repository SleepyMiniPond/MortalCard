using System;
using MortalGame.GameData;
using MortalGame.GameModel;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Tests
{
    public sealed class CardPlayAttributeReactionEffectTests
    {
        public static readonly ReactionSource[] Sources =
        {
            ReactionSource.PlayerBuff,
            ReactionSource.CharacterBuff,
            ReactionSource.CardBuff
        };

        [TestCaseSource(nameof(Sources))]
        public void CardPlayAttributeAddition_ThreeBuffSources_ModifySameCardPlayAttribute(
            ReactionSource source)
        {
            var setup = _CreateSetup(source, useCardPlaySource: true);

            _Run(setup);

            Assert.That(
                setup.Attribute.IntValues[EffectAttributeAdditionType.PowerAddition],
                Is.EqualTo(2));
        }

        [TestCaseSource(nameof(Sources))]
        public void CardPlayAttributeAddition_WithoutCardPlaySource_IsSafeNoOp(
            ReactionSource source)
        {
            var setup = _CreateSetup(source, useCardPlaySource: false);

            _Run(setup);

            Assert.That(
                setup.Attribute.IntValues[EffectAttributeAdditionType.PowerAddition],
                Is.Zero);
        }

        [Test]
        public void CardPlayAttributeAddition_CharacterAndCardBuffAssets_RoundTrip()
        {
            var characterPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/CharacterBuffCardPlayAttribute.asset");
            var cardPath = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Tests/EditMode/GameModel/Effect/CardBuffCardPlayAttribute.asset");
            var characterAsset = ScriptableObject.CreateInstance<CharacterBuffDataScriptable>();
            var cardAsset = ScriptableObject.CreateInstance<CardBuffScriptable>();

            try
            {
                characterAsset.Data.ID = "character-card-play-attribute";
                characterAsset.Data.MaxLevel = 1;
                characterAsset.Data.LifeTimeData = new AlwaysLifeTimeCharacterBuffData();
                characterAsset.Data.BuffEffects[GameTiming.BeforeExecuteStart] = new[]
                {
                    new ConditionalCharacterBuffEffect
                    {
                        Effect = _CreateEffect()
                    }
                };
                cardAsset.Data.ID = "card-card-play-attribute";
                cardAsset.Data.LifeTimeData = new AlwaysLifeTimeCardBuffData();
                cardAsset.Data.BuffEffects[GameTiming.BeforeExecuteStart] = new[]
                {
                    new ConditionalCardBuffEffect
                    {
                        Effect = _CreateEffect()
                    }
                };
                AssetDatabase.CreateAsset(characterAsset, characterPath);
                AssetDatabase.CreateAsset(cardAsset, cardPath);
                AssetDatabase.SaveAssets();
                Resources.UnloadAsset(characterAsset);
                Resources.UnloadAsset(cardAsset);
                AssetDatabase.ImportAsset(characterPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(cardPath, ImportAssetOptions.ForceSynchronousImport);

                var loadedCharacter = AssetDatabase.LoadAssetAtPath<CharacterBuffDataScriptable>(characterPath);
                var loadedCard = AssetDatabase.LoadAssetAtPath<CardBuffScriptable>(cardPath);

                Assert.That(
                    loadedCharacter.Data.BuffEffects[GameTiming.BeforeExecuteStart][0].Effect,
                    Is.TypeOf<ModifyCardPlayAttributeEffect>());
                Assert.That(
                    loadedCard.Data.BuffEffects[GameTiming.BeforeExecuteStart][0].Effect,
                    Is.TypeOf<ModifyCardPlayAttributeEffect>());
                Assert.That(
                    _CreateEffect(),
                    Is.Not.InstanceOf<ICardEffect>());
            }
            finally
            {
                AssetDatabase.DeleteAsset(characterPath);
                AssetDatabase.DeleteAsset(cardPath);
            }
        }

        private static AttributeReactionSetup _CreateSetup(
            ReactionSource source,
            bool useCardPlaySource)
        {
            var sourceCardBuffData = new CardBuffData
            {
                ID = BuffTestBuilder.CardBuffId,
                LifeTimeData = new AlwaysLifeTimeCardBuffData()
            };
            var built = new GameplayManagerTestBuilder()
                .WithCardBuff(sourceCardBuffData)
                .Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Ally.CardManager.HandCard.AddCard(card);
            var createCardBuffContext = new TriggerContext(
                built.Manager,
                new PlayerTrigger(built.Ally),
                new UpdateTimingAction(GameTiming.BeforeExecuteStart, SystemSource.Instance));
            var cardBuff = BuffTestBuilder.CreateCardBuff(
                createCardBuffContext,
                built.ContextManager.CardBuffLibrary);
            card.BuffManager.AddBuff(cardBuff);
            var attribute = new CardPlayAttributeEntity();
            IActionSource actionSource = useCardPlaySource
                ? new CardPlaySource(
                    card,
                    0,
                    1,
                    new LoseEnergyEffectCommand(built.Ally, 0),
                    attribute)
                : SystemSource.Instance;
            var context = source switch
            {
                ReactionSource.PlayerBuff => new TriggerContext(
                    built.Manager,
                    new PlayerBuffTrigger(built.Ally, BuffTestBuilder.CreatePlayerBuff()),
                    new TestAction(actionSource)),
                ReactionSource.CharacterBuff => new TriggerContext(
                    built.Manager,
                    new CharacterBuffTrigger(
                        built.Ally.MainCharacter,
                        BuffTestBuilder.CreateCharacterBuff()),
                    new TestAction(actionSource)),
                ReactionSource.CardBuff => new TriggerContext(
                    built.Manager,
                    new CardBuffTrigger(card, cardBuff),
                    new TestAction(actionSource)),
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };

            return new AttributeReactionSetup(context, source, attribute);
        }

        private static ModifyCardPlayAttributeEffect _CreateEffect()
        {
            return new ModifyCardPlayAttributeEffect
            {
                Type = EffectAttributeAdditionType.PowerAddition,
                Value = new ConstInteger { Value = 2 }
            };
        }

        private static void _Run(AttributeReactionSetup setup)
        {
            var runner = new EffectQueueRunner();
            var effect = _CreateEffect();
            switch (setup.Source)
            {
                case ReactionSource.PlayerBuff:
                    runner.Enqueue(new PlayerBuffEffectQueueItem(
                        setup.Context,
                        (IPlayerBuffEffect)effect));
                    break;
                case ReactionSource.CharacterBuff:
                    runner.Enqueue(new CharacterBuffEffectQueueItem(
                        setup.Context,
                        (ICharacterBuffEffect)effect));
                    break;
                case ReactionSource.CardBuff:
                    runner.Enqueue(new CardBuffEffectQueueItem(
                        setup.Context,
                        (ICardBuffEffect)effect));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            runner.RunToCompletion();
        }

        public enum ReactionSource
        {
            PlayerBuff,
            CharacterBuff,
            CardBuff
        }

        private sealed record AttributeReactionSetup(
            TriggerContext Context,
            ReactionSource Source,
            CardPlayAttributeEntity Attribute);

        private sealed record TestAction(IActionSource Source) : IActionUnit
        {
            public GameTiming Timing => GameTiming.None;
        }
    }
}
