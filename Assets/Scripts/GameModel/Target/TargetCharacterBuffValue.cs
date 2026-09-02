using System;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;

namespace MortalGame.GameModel
{
    public interface ITargetCharacterBuffValue
    {
        Option<ICharacterBuffEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NoneCharacterBuff : ITargetCharacterBuffValue
    {
        public Option<ICharacterBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Option.None<ICharacterBuffEntity>();
        }
    }

    [Serializable]
    public class TriggeredCharacterBuff : ITargetCharacterBuffValue
    {
        public Option<ICharacterBuffEntity> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Triggered switch
            {
                CharacterBuffTrigger characterBuff => characterBuff.Buff.SomeNotNull(),
                _ => Option.None<ICharacterBuffEntity>()
            };
        }
    }

    [Serializable]
    public class CharacterBuffById : ITargetCharacterBuffValue
    {
        [HorizontalGroup("1")]
        public ITargetCharacterBuffCollectionValue CharacterBuffs;

        [HorizontalGroup("2")]
        public string BuffId;

        public Option<ICharacterBuffEntity> Eval(TriggerContext triggerContext)
        {
            return CharacterBuffs
                .Eval(triggerContext)
                .FirstOrDefault(buff => buff.CharacterBuffDataId == BuffId)
                .SomeNotNull();
        }
    }

    public interface ITargetCharacterBuffCollectionValue
    {
        IReadOnlyCollection<ICharacterBuffEntity> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class NoneCharacterBuffs : ITargetCharacterBuffCollectionValue
    {
        public IReadOnlyCollection<ICharacterBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Array.Empty<ICharacterBuffEntity>();
        }
    }

    [Serializable]
    public class CharacterBuffsOfCharacter : ITargetCharacterBuffCollectionValue
    {
        [HorizontalGroup("1")]
        public ITargetCharacterValue Character;

        public IReadOnlyCollection<ICharacterBuffEntity> Eval(TriggerContext triggerContext)
        {
            return Character
                .Eval(triggerContext)
                .Map(character => character.BuffManager.Buffs)
                .ValueOr(Array.Empty<ICharacterBuffEntity>());
        }
    }
}
