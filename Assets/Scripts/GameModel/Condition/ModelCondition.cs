using System;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using Optional;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace MortalGame.GameModel
{


    public interface ICondition
    {
        bool Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class ConstCondition : ICondition
    {
        public bool Value;
        public bool Eval(TriggerContext triggerContext)
        {
            return Value;
        }
    }

    [Serializable]
    public class GameTimingCondition : ICondition
    {
        public GameTiming Timing;

        public bool Eval(TriggerContext triggerContext)
        {
            return Timing != GameTiming.None &&
                triggerContext.ReactionOriginTiming
                    .Map(timing => timing == Timing)
                    .ValueOr(false);
        }
    }

    [Serializable]
    public class AllCondition : ICondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ICondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return Conditions.All(condition => condition.Eval(triggerContext));
        }
    }

    [Serializable]
    public class AnyCondition : ICondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ICondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return Conditions.Any(condition => condition.Eval(triggerContext));
        }
    }

    [Serializable]
    public class InverseCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ICondition Condition;

        public bool Eval(TriggerContext triggerContext)
        {
            return !Condition.Eval(triggerContext);
        }
    }

    [Serializable]
    public class IsTriggeredOwnerTurnCondition : ICondition
    {
        public bool Eval(TriggerContext triggerContext)
        {

            var triggeredOwner = triggerContext.Triggered switch
            {
                ICardTriggeredSource cardSource => cardSource.Card.Owner(triggerContext.Model),
                ICharacterTriggeredSource characterSource =>
                    characterSource.Character.Owner(triggerContext.Model),
                IPlayerTriggeredSource playerSource => playerSource.Player.Some(),
                _ => Option.None<IPlayerEntity>()
            };
            return triggerContext.Model.GameStatus.CurrentPlayer.Value
                .Combine(triggeredOwner)
                .Map(pair => pair.Item1 == pair.Item2)
                .ValueOr(false);
        }
    }

    [Serializable]
    public class IntegerCondition : ICondition
    {
        [HorizontalGroup("1")]
        public IIntegerValue Value;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<IIntegerValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return Value
                .Eval(triggerContext)
                .Map(value => Conditions.All(condition => condition.Eval(triggerContext, value)))
                .ValueOr(false);
        }
    }

    [Serializable]
    public class CardCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetCardValue Card;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<ICardValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            var cardOpt = Card.Eval(triggerContext);
            return cardOpt.Match(
                card => Conditions.All(c => c.Eval(triggerContext, card)),
                () => false
            );
        }
    }

    [Serializable]
    public class CardCollectionContainsCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetCardCollectionValue CardCollection;

        [HorizontalGroup("2")]
        public ITargetCardValue Card;

        public bool Eval(TriggerContext triggerContext)
        {
            return Card
                .Eval(triggerContext)
                .Map(card => CardCollection
                    .Eval(triggerContext)
                    .Any(collectionCard => collectionCard.Identity == card.Identity))
                .ValueOr(false);
        }
    }

    [SerializeField]
    public class CardPlayCondition : ICondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ICardPlayValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return triggerContext.Action.Source switch
            {
                CardPlaySource cardPlaySource =>
                    Conditions.All(c => c.Eval(triggerContext, cardPlaySource)),
                CardPlayResultSource cardPlayResultSource =>
                    Conditions.All(c => c.Eval(triggerContext, cardPlayResultSource.CardPlaySource)),
                _ => false
            };
        }
    }

    [SerializeField]
    public class CardPlayResultCondition : ICondition
    {
        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ICardPlayResultValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return triggerContext.Action.Source switch
            {
                CardPlayResultSource cardPlayResultSource =>
                    Conditions.All(c => c.Eval(triggerContext, cardPlayResultSource)),
                _ => false
            };
        }
    }

    [Serializable]
    public class PlayerCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetPlayerValue Player;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<IPlayerValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            var playerOpt = Player.Eval(triggerContext);
            return playerOpt.Match(
                player => Conditions.All(c => c.Eval(triggerContext, player)),
                () => false
            );
        }
    }

    [Serializable]
    public class CharacterCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetCharacterValue Character;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<ICharacterValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            var characterOpt = Character.Eval(triggerContext);
            return characterOpt.Match(
                character => Conditions.All(c => c.Eval(triggerContext, character)),
                () => false
            );
        }
    }

    [Serializable]
    public class PlayerBuffCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetPlayerBuffValue PlayerBuff;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<IPlayerBuffValueCondition> Conditions = new();
        public bool Eval(TriggerContext triggerContext)
        {
            var playerBuffOpt = PlayerBuff.Eval(triggerContext);
            return playerBuffOpt.Match(
                playerBuff => Conditions.All(c => c.Eval(triggerContext, playerBuff)),
                () => false
            );
        }
    }

    [Serializable]
    public class CharacterBuffCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetCharacterBuffValue CharacterBuff;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<ICharacterBuffValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return CharacterBuff
                .Eval(triggerContext)
                .Map(characterBuff => Conditions.All(
                    condition => condition.Eval(triggerContext, characterBuff)))
                .ValueOr(false);
        }
    }

    [Serializable]
    public class CardBuffCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetCardBuffValue CardBuff;

        [ShowInInspector]
        [HorizontalGroup("2")]
        public List<ICardBuffValueCondition> Conditions = new();

        public bool Eval(TriggerContext triggerContext)
        {
            return CardBuff
                .Eval(triggerContext)
                .Map(cardBuff => Conditions.All(
                    condition => condition.Eval(triggerContext, cardBuff)))
                .ValueOr(false);
        }
    }

    [Serializable]
    public class PlayerBuffCollectionContainsIdCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetPlayerBuffCollectionValue PlayerBuffs;

        [HorizontalGroup("2")]
        public string BuffId;

        public bool Eval(TriggerContext triggerContext)
        {
            return PlayerBuffs
                .Eval(triggerContext)
                .Any(buff => buff.PlayerBuffDataId == BuffId);
        }
    }

    [Serializable]
    public class CharacterBuffCollectionContainsIdCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetCharacterBuffCollectionValue CharacterBuffs;

        [HorizontalGroup("2")]
        public string BuffId;

        public bool Eval(TriggerContext triggerContext)
        {
            return CharacterBuffs
                .Eval(triggerContext)
                .Any(buff => buff.CharacterBuffDataId == BuffId);
        }
    }

    [Serializable]
    public class CardBuffCollectionContainsIdCondition : ICondition
    {
        [HorizontalGroup("1")]
        public ITargetCardBuffCollectionValue CardBuffs;

        [HorizontalGroup("2")]
        public string BuffId;

        public bool Eval(TriggerContext triggerContext)
        {
            return CardBuffs
                .Eval(triggerContext)
                .Any(buff => buff.CardBuffDataID == BuffId);
        }
    }

}
