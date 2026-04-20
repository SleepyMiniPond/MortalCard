using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ICardPropertyData
{
    ICardPropertyEntity CreateEntity();
}

[Serializable]
public class PreservedPropertyData : ICardPropertyData
{
    public ICardPropertyEntity CreateEntity()
    {
        return new PreservedPropertyEntity();
    }
}

[Serializable]
public class InitialPriorityPropertyData : ICardPropertyData
{
    public ICardPropertyEntity CreateEntity()
    {
        return new InitialPriorityPropertyEntity();
    }
}

[Serializable]
public class ConsumablePropertyData : ICardPropertyData
{
    public ICardPropertyEntity CreateEntity()
    {
        return new ConsumablePropertyEntity();
    }
}

[Serializable]
public class DisposePropertyData : ICardPropertyData
{
    public ICardPropertyEntity CreateEntity()
    {
        return new DisposePropertyEntity();
    }
}

[Serializable]
public class AutoDisposePropertyData : ICardPropertyData
{
    public ICardPropertyEntity CreateEntity()
    {
        return new AutoDisposePropertyEntity();
    }
}

[Serializable]
public class SealedPropertyData : ICardPropertyData
{
    public ICardPropertyEntity CreateEntity()
    {
        return new SealedPropertyEntity();
    }
}

[Serializable]
public class RecyclePropertyData : ICardPropertyData
{
    public ICardPropertyEntity CreateEntity()
    {
        return new RecyclePropertyEntity();
    }
}