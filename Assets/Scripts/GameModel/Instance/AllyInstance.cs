using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public record AllyInstance(
    Guid Identity,
    string NameKey,
    int CurrentDisposition,
    int CurrentHealth,
    int MaxHealth,
    int CurrentEnergy,
    int MaxEnergy,
    List<CardInstance> Deck,
    int HandCardMaxCount);