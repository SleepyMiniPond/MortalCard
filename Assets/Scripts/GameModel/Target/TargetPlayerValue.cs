using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface ITargetPlayerValue
    {
        Option<IPlayerEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NonePlayer : ITargetPlayerValue
    {
        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return Option.None<IPlayerEntity>();
        }
    }
    [Serializable]
    public class CurrentPlayer : ITargetPlayerValue
    {
        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Model.GameStatus.CurrentPlayer.Value;
        }
    }
    [Serializable]
    public class PlayerByFaction : ITargetPlayerValue
    {
        public Faction Faction;

        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return Faction switch
            {
                Faction.Ally => (triggerContext.Model.GameStatus.Ally as IPlayerEntity)
                    .SomeNotNull(),
                Faction.Enemy => (triggerContext.Model.GameStatus.Enemy as IPlayerEntity)
                    .SomeNotNull(),
                _ => Option.None<IPlayerEntity>()
            };
        }
    }
    [Serializable]
    public class OppositePlayer : ITargetPlayerValue
    {
        [HorizontalGroup("1")]
        public ITargetPlayerValue Reference;

        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            var referenceOpt = Reference.Eval(triggerContext);
            return
                referenceOpt.FlatMap(reference =>
                    reference.Faction == Faction.Ally ? (triggerContext.Model.GameStatus.Enemy as IPlayerEntity).Some() :
                    reference.Faction == Faction.Enemy ? (triggerContext.Model.GameStatus.Ally as IPlayerEntity).Some() :
                    Option.None<IPlayerEntity>());
        }
    }
    [Serializable]
    public class CardOwner : ITargetPlayerValue
    {
        [HorizontalGroup("1")]
        public ITargetCardValue Card;

        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            var cardOpt = Card.Eval(triggerContext);
            return cardOpt.FlatMap(card => card.Owner(triggerContext.Model));
        }
    }
    [Serializable]
    public class PlayerBuffContentPlayer : ITargetPlayerValue
    {
        public enum PlayerType
        {
            Owner,
            Caster,
        }

        [HorizontalGroup("1")]
        public ITargetPlayerBuffValue PlayerBuff;

        public PlayerType Type;

        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            var playerBuffOpt = PlayerBuff.Eval(triggerContext);
            return playerBuffOpt.FlatMap(playerBuff => Type switch
            {
                PlayerType.Owner => playerBuff.Owner(triggerContext.Model),
                PlayerType.Caster => playerBuff.Caster,
                _ => Option.None<IPlayerEntity>()
            });
        }
    }
    [Serializable]
    public class CharacterOwner : ITargetPlayerValue
    {
        [HorizontalGroup("1")]
        public ITargetCharacterValue Character;

        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            var characterOpt = Character.Eval(triggerContext);
            return characterOpt.FlatMap(character => character.Owner(triggerContext.Model));
        }
    }
    [Serializable]
    public class SelectedPlayer : ITargetPlayerValue
    {
        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Model.GameStatus.GetPlayer(triggerContext.Model.ContextManager.Context.SelectedPlayer);
        }
    }

    public interface ITargetPlayerCollectionValue
    {
        IReadOnlyCollection<IPlayerEntity> Eval(TriggerContext triggerContext);
    }
    [Serializable]
    public class NonePlayers : ITargetPlayerCollectionValue
    {
        public IReadOnlyCollection<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return Array.Empty<IPlayerEntity>();
        }
    }
    [Serializable]
    public class SinglePlayerCollection : ITargetPlayerCollectionValue
    {
        [HorizontalGroup("1")]
        public ITargetPlayerValue Target;

        public IReadOnlyCollection<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return Target.Eval(triggerContext).ToEnumerable().ToList();
        }
    }
    [Serializable]
    public class TriggeredPlayer : ITargetPlayerValue
    {
        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered is IPlayerTriggeredSource source
                ? source.Player.SomeNotNull()
                : Option.None<IPlayerEntity>();
        }
    }

    /// <summary>
    /// 取得目前 Reaction 來源的持有者。卡牌與卡牌 Buff 的持有者由卡片所在玩家決定；
    /// CharacterBuff 的持有者則是掛載該 Buff 角色所屬的玩家。
    /// </summary>
    [Serializable]
    public class ReactionOwnerPlayer : ITargetPlayerValue
    {
        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                ICardTriggeredSource cardSource =>
                    cardSource.Card.Owner(triggerContext.Model),
                ICharacterTriggeredSource characterSource =>
                    characterSource.Character.Owner(triggerContext.Model),
                IPlayerTriggeredSource playerSource =>
                    playerSource.Player.SomeNotNull(),
                _ => Option.None<IPlayerEntity>()
            };
        }
    }

    /// <summary>
    /// 取得目前 Reaction 的施放者。沒有可追溯施放者時保留 None，
    /// 不以 CurrentPlayer 偽造施放者。
    /// </summary>
    [Serializable]
    public class ReactionCasterPlayer : ITargetPlayerValue
    {
        public Option<IPlayerEntity> Eval(TriggerContext triggerContext)
        {
            return ReactionCasterResolver.Resolve(triggerContext);
        }
    }

}
