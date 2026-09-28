using System;
using System.Collections.Generic;
using MortalGame.GameModel;
using NUnit.Framework;
using Optional;

namespace MortalGame.Tests
{
    public sealed class InvokeCardEffectsSourceTests
    {
        [Test]
        public void InvokedEnemyCard_UsesCallerBuffAndKeepsCardOwner()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(card);
            built.Ally.BuffManager.AddBuff(_CreateDamageBuff(built.Enemy, 7));
            built.Enemy.BuffManager.AddBuff(_CreateDamageBuff(built.Ally, 11));

            var source = new InvokeCardEffectsSource(card, built.Ally, SystemSource.Instance);
            var context = new TriggerContext(
                built.Manager, new CardTrigger(card), new InvokeCardEffectsAction(source));

            Assert.That(new ActionCard().Eval(context).ValueOr((ICardEntity)null), Is.SameAs(card));
            Assert.That(new CardOwner { Card = new ActionCard() }.Eval(context).ValueOr((IPlayerEntity)null),
                Is.SameAs(built.Enemy));
            Assert.That(ReactionContextQuery.Caster(context).ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
            Assert.That(ReactionContextQuery.InvokeCaster(context).ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
            Assert.That(GameFormula.NormalDamagePoint(context, 2).ValueOr(-1), Is.EqualTo(9));
            Assert.That(GameFormula.CardPower(context, card).ValueOr(-1),
                Is.EqualTo(card.OriginPower + 7));
            var lookContext = context with { Action = new CardLookIntentAction(card, ((IPlayerEntity)built.Ally).Some()) };
            Assert.That(GameFormula.CardPower(lookContext, card).ValueOr(-1),
                Is.EqualTo(card.OriginPower + 7));
            Assert.That(new InvokeCardEffectsAction(source).Timing,
                Is.EqualTo(MortalGame.GameData.GameTiming.None));
        }

        [Test]
        public void CardLook_WithExplicitNoCaster_DoesNotFallBackToExistingOwner()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            built.Enemy.CardManager.HandCard.AddCard(card);
            built.Enemy.BuffManager.AddBuff(_CreateDamageBuff(built.Enemy, 11));
            var context = new TriggerContext(built.Manager, new CardTrigger(card),
                new CardLookIntentAction(card, Option.None<IPlayerEntity>()));

            Assert.That(card.Owner(built.Manager).HasValue, Is.True);
            Assert.That(ReactionContextQuery.Caster(context).HasValue, Is.False);
            Assert.That(GameFormula.CardPower(context, card).HasValue, Is.False);
        }

        [Test]
        public void CardInfo_ResolvesOwnerAtEntryAndKeepsOwnerlessValuesMissing()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var card = CardTestBuilder.CreateCard(built.ContextManager.CardLibrary);
            Assert.That(card.ToInfo(built.Manager).Power.HasValue, Is.False);

            built.Enemy.CardManager.HandCard.AddCard(card);
            built.Enemy.BuffManager.AddBuff(_CreateDamageBuff(built.Enemy, 11));

            Assert.That(card.ToInfo(built.Manager).Power.ValueOr(-1), Is.EqualTo(card.OriginPower + 11));
        }

        [Test]
        public void BuffInvocation_UsesHostEvenWhenOriginalBuffCasterIsOpponent()
        {
            var built = new GameplayManagerTestBuilder().Build();
            var buff = _CreateDamageBuff(built.Ally, 7);
            var context = new TriggerContext(
                built.Manager,
                new PlayerBuffTrigger(built.Enemy, buff),
                new UpdateTimingAction(MortalGame.GameData.GameTiming.AfterTurnEnd, SystemSource.Instance));

            Assert.That(ReactionContextQuery.Caster(context).ValueOr((IPlayerEntity)null), Is.SameAs(built.Ally));
            Assert.That(ReactionContextQuery.InvokeCaster(context).ValueOr((IPlayerEntity)null), Is.SameAs(built.Enemy));
        }

        private static PlayerBuffEntity _CreateDamageBuff(IPlayerEntity caster, int addition)
        {
            return new PlayerBuffEntity(
                "invoke-source-test", Guid.NewGuid(), 1, 1, caster.Some(),
                new IPlayerBuffPropertyEntity[]
                {
                    new NormalDamageAdditionPlayerBuffPropertyEntity(
                        new ConstInteger { Value = addition }),
                    new AllCardPowerPlayerBuffPropertyEntity(
                        new ConstInteger { Value = addition })
                },
                new AlwaysLifeTimePlayerBuffEntity(),
                new Dictionary<string, IReactionSessionEntity>());
        }
    }
}
