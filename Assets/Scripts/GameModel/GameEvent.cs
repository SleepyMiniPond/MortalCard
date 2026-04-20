using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Optional;
using Sirenix.Serialization.Utilities;
using Sirenix.Utilities;
using UnityEngine;

public interface IGameEvent
{
}

public interface IAnimationNumberEvent : IGameEvent
{
}

public record NoneEvent : IGameEvent
{
    public static readonly NoneEvent Instance = new NoneEvent();
}

public record AllySummonEvent(AllyEntity Player) : IGameEvent;
public record EnemySummonEvent(EnemyEntity Enemy) : IGameEvent;
public record RoundStartEvent(int Round, AllyEntity Player, EnemyEntity Enemy) : IGameEvent;

public record RecycleGraveyardToDeckEvent(
    Faction Faction, 
    CardManagerInfo CardManagerInfo) : IGameEvent;
public record DiscardHandCardEvent(
    Faction Faction,
    IReadOnlyCollection<CardInfo> DiscardedCardInfos,
    IReadOnlyCollection<CardInfo> ExcludedCardInfos,
    CardManagerInfo CardManagerInfo) : IGameEvent;
public record RecycleGraveyardToHandCardEvent(
    Faction Faction,
    CardInfo RecycledCardInfo, 
    CardManagerInfo CardManagerInfo) : IGameEvent;
public record DrawCardEvent(
    Faction Faction, 
    CardInfo NewCardInfo, 
    CardManagerInfo CardManagerInfo) : IGameEvent;

public record MoveCardEvent(
    Faction Faction,
    CardInfo CardInfo,
    CardCollectionType Start,
    CardCollectionType Destination,
    CardManagerInfo CardManagerInfo) : IGameEvent;
public record AddCardEvent(
    Faction Faction,
    CardInfo CardInfo,
    CardCollectionType Destination,
    CardManagerInfo CardManagerInfo) : IGameEvent;

public record EnemySelectCardEvent(CardInfo SelectedCardInfo, ImmutableArray<Guid> SelectedCards) : IGameEvent;
public record EnemyUnselectedCardEvent(ImmutableArray<Guid> UnselectedCards) : IGameEvent;
public record PlayerExecuteStartEvent(Faction Faction, CardManagerInfo CardManagerInfo, CardCollectionInfo HandCardInfo) : IGameEvent;
public record PlayerExecuteEndEvent(Faction Faction, CardManagerInfo CardManagerInfo) : IGameEvent;
public record UsedCardEvent(Faction Faction, CardInfo UsedCardInfo, CardManagerInfo CardManagerInfo) : IGameEvent;

public record GainEnergyEvent(Faction Faction, EnergyInfo Info, GainEnergyResult GainEnergyResult) : IGameEvent, IAnimationNumberEvent;
public record LoseEnergyEvent(Faction Faction, EnergyInfo Info, LoseEnergyResult LoseEnergyResult) : IGameEvent, IAnimationNumberEvent;

public record IncreaseDispositionEvent(DispositionInfo Info, int DeltaDisposition) : IGameEvent, IAnimationNumberEvent;
public record DecreaseDispositionEvent(DispositionInfo Info, int DeltaDisposition) : IGameEvent, IAnimationNumberEvent;

public abstract record HealthEvent(Faction Faction, Guid CharacterIdentity, int Hp, int Dp, int MaxHp) : IGameEvent, IAnimationNumberEvent;
public record DamageEvent(
    Faction Faction,
    ICharacterEntity Character,
    TakeDamageResult TakeDamageResult) : HealthEvent(
        Faction, Character.Identity, Character.CurrentHealth, Character.CurrentArmor, Character.MaxHealth);
public record GetHealEvent(
    Faction Faction, 
    ICharacterEntity Character, 
    GetHealResult GetHealResult) : HealthEvent(
        Faction, Character.Identity, Character.CurrentHealth, Character.CurrentArmor, Character.MaxHealth);
public record GetShieldEvent(
    Faction Faction, 
    ICharacterEntity Character, 
    GetShieldResult GetShieldResult) : HealthEvent(
        Faction, Character.Identity, Character.CurrentHealth, Character.CurrentArmor, Character.MaxHealth);

public record AddPlayerBuffEvent(Faction Faction, PlayerBuffInfo Buff) : IGameEvent;
public record RemovePlayerBuffEvent(Faction Faction, PlayerBuffInfo Buff) : IGameEvent;
public record ModifyPlayerBuffLevelEvent(Faction Faction, PlayerBuffInfo Buff) : IGameEvent;

public record AddCardBuffEvent(Faction Faction, CardInfo CardInfo) : IGameEvent;
public record RemoveCardBuffEvent(Faction Faction, CardInfo CardInfo) : IGameEvent;
public record ModifyCardBuffLevelEvent(Faction Faction, CardInfo CardInfo) : IGameEvent;

public record GeneralUpdateEvent(
    IReadOnlyList<PlayerBuffInfo> PlayerBuffInfos,
    IReadOnlyList<CharacterBuffInfo> CharacterBuffInfos,
    IReadOnlyList<CardInfo> CardInfos) : IGameEvent
{
    public GeneralUpdateEvent(
        IEnumerable<PlayerBuffInfo> playerBuffInfos,
        IEnumerable<CharacterBuffInfo> characterBuffInfos,
        IEnumerable<CardInfo> cardInfos)
        : this(playerBuffInfos.ToList(),
            characterBuffInfos.ToList(),
            cardInfos.ToList()) {}
               
    public GeneralUpdateEvent(IReadOnlyCollection<PlayerBuffInfo> playerBuffInfos)
        : this(playerBuffInfos.ToList(),
            Array.Empty<CharacterBuffInfo>(),
            Array.Empty<CardInfo>()) {}
    public GeneralUpdateEvent(IReadOnlyCollection<CharacterBuffInfo> characterBuffInfos)
        : this(Array.Empty<PlayerBuffInfo>(),
            characterBuffInfos.ToList(),
            Array.Empty<CardInfo>()) {}
    public GeneralUpdateEvent(IReadOnlyCollection<CardInfo> cardInfos)
        : this(Array.Empty<PlayerBuffInfo>(),
            Array.Empty<CharacterBuffInfo>(),
            cardInfos.ToList()) {}
    public GeneralUpdateEvent(PlayerBuffInfo playerBuffInfo)
        : this(new List<PlayerBuffInfo> { playerBuffInfo },
              Array.Empty<CharacterBuffInfo>(),
              Array.Empty<CardInfo>()) {}
    public GeneralUpdateEvent(CharacterBuffInfo characterBuffInfo)
        : this(Array.Empty<PlayerBuffInfo>(),
            new List<CharacterBuffInfo> { characterBuffInfo },
            Array.Empty<CardInfo>()) {}
    public GeneralUpdateEvent(CardInfo cardInfo)
        : this(Array.Empty<PlayerBuffInfo>(),
            Array.Empty<CharacterBuffInfo>(),
            new List<CardInfo> { cardInfo }) {}
}
