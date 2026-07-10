using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MortalGame.GameData
{

public interface ICardPropertyData
{
}

[Serializable]
public class PreservedPropertyData : ICardPropertyData
{
}

[Serializable]
public class InitialPriorityPropertyData : ICardPropertyData
{
}

[Serializable]
public class ConsumablePropertyData : ICardPropertyData
{
}

[Serializable]
public class DisposePropertyData : ICardPropertyData
{
}

[Serializable]
public class AutoDisposePropertyData : ICardPropertyData
{
}

[Serializable]
public class SealedPropertyData : ICardPropertyData
{
}

[Serializable]
public class RecyclePropertyData : ICardPropertyData
{
}

}
