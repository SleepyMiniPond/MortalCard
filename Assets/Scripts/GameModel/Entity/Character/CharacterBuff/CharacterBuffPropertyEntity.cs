using Rayark.Mast;
using UnityEngine;

public interface ICharacterBuffPropertyEntity
{
    CharacterBuffProperty Property { get; }
    
    int Eval(IGameplayModel gameWatcher, ITriggeredSource triggerSource);
}

public class MaxHealthPropertyCharacterBuffEntity : ICharacterBuffPropertyEntity
{
    public CharacterBuffProperty Property => CharacterBuffProperty.MaxHealth;

    public MaxHealthPropertyCharacterBuffEntity() { }
    public int Eval(IGameplayModel gameWatcher, ITriggeredSource triggerSource) => 0;
}

public class MaxEnergyPropertyCharacterBuffEntity : ICharacterBuffPropertyEntity
{
    public CharacterBuffProperty Property => CharacterBuffProperty.MaxEnergy;

    public MaxEnergyPropertyCharacterBuffEntity() { }
    public int Eval(IGameplayModel gameWatcher, ITriggeredSource triggerSource) => 0;
}