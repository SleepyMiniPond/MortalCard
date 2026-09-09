using System;
using MortalGame.GameModel;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace MortalGame.GameData
{

    public interface ICardEffect
    {

    }

    // ==============================
    // Target-Character Effect
    // ==============================
    [Serializable]
    public class DamageEffect :
        ICardEffect,
        IPlayerBuffEffect,
        ICharacterBuffEffect,
        ICardBuffEffect
    {
        /// <summary>傷害計算公式的種類。</summary>
        public DamageType Type = DamageType.Normal;
        public ITargetCharacterCollectionValue Targets;
        public IIntegerValue Value;
    }
    [Serializable]
    public class ShieldEffect :
        ICardEffect, IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        public ITargetCharacterCollectionValue Targets;
        public IIntegerValue Value;
    }
    [Serializable]
    public class HealEffect :
        ICardEffect, IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        public ITargetCharacterCollectionValue Targets;
        public IIntegerValue Value;
    }

    // ==============================
    // Target-Player Effect
    // ==============================
    [Serializable]
    public class GainEnergyEffect :
        ICardEffect, IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        public ITargetPlayerCollectionValue Targets;
        public IIntegerValue Value;
    }
    [Serializable]
    public class LoseEnegyEffect :
        ICardEffect, IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        public ITargetPlayerCollectionValue Targets;
        public IIntegerValue Value;
    }

    [Serializable]
    public class AddPlayerBuffEffect : ICardEffect
    {
        public ITargetPlayerCollectionValue Targets;
        [ValueDropdown("@DropdownHelper.PlayerBuffNames")]
        public string BuffId;
        public IIntegerValue Level;
    }
    [Serializable]
    public class ModifyPlayerBuffLevelEffect : ICardEffect
    {
        public ITargetPlayerCollectionValue Targets;
        [ValueDropdown("@DropdownHelper.PlayerBuffNames")]
        public string BuffId;
        public IIntegerValue DeltaLevel;
    }
    [Serializable]
    public class RemovePlayerBuffEffect : ICardEffect
    {
        public ITargetPlayerCollectionValue Targets;
        [ValueDropdown("@DropdownHelper.PlayerBuffNames")]
        public string BuffId;
    }
    [Serializable]
    public class IncreaseDispositionEffect :
        ICardEffect, IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        // Only Ally Has Disposition
        public ITargetPlayerCollectionValue Targets;
        public IIntegerValue Value;
    }
    [Serializable]
    public class DecreaseDispositionEffect :
        ICardEffect, IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        // Only Ally Has Disposition
        public ITargetPlayerCollectionValue Targets;
        public IIntegerValue Value;
    }

    // ==============================
    // Target-Card Effect
    // ==============================
    [Serializable]
    public class DrawCardEffect :
        ICardEffect, IPlayerBuffEffect, ICharacterBuffEffect, ICardBuffEffect
    {
        public ITargetPlayerCollectionValue Targets;
        public IIntegerValue Value;
    }
    [Serializable]
    public class DiscardCardEffect : ICardEffect
    {
        public ITargetCardCollectionValue TargetCards;
    }
    [Serializable]
    public class ConsumeCardEffect : ICardEffect
    {
        public ITargetCardCollectionValue TargetCards;
    }
    [Serializable]
    public class DisposeCardEffect : ICardEffect
    {
        public ITargetCardCollectionValue TargetCards;
    }
    [Serializable]
    public class CreateCardEffect : ICardEffect
    {
        public ITargetPlayerValue Target;
        [ShowInInspector]
        public List<string> CardDataIds = new();
        [ShowInInspector]
        public List<AddCardBuffData> AddCardBuffDatas = new();
        public CardCollectionType CreateDestination;
    }
    [Serializable]
    public class CloneCardEffect : ICardEffect
    {
        public ITargetPlayerValue Target;
        public ITargetCardCollectionValue ClonedCards;
        [ShowInInspector]
        public List<AddCardBuffData> AddCardBuffDatas = new();
        public CardCollectionType CloneDestination;
    }
    [Serializable]
    public class AddCardBuffEffect : ICardEffect
    {
        public ITargetCardCollectionValue TargetCards;
        [ShowInInspector]
        public List<AddCardBuffData> AddCardBuffDatas = new();
    }
    [Serializable]
    public class RemoveCardBuffEffect : ICardEffect
    {
        public ITargetCardCollectionValue TargetCards;
        [ValueDropdown("@DropdownHelper.CardBuffNames")]
        public string BuffId;
    }

}
