using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ICardPropertyEntity
{
    CardProperty Property { get; }
    IEnumerable<string> Keywords { get; }

    int Eval(TriggerContext triggerContext);

    ICardPropertyEntity Clone();
}

public class PreservedPropertyEntity : ICardPropertyEntity
{
    public CardProperty Property => CardProperty.Preserved;
    public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

    public PreservedPropertyEntity() { }
    public int Eval(TriggerContext triggerContext) => 0;
    public ICardPropertyEntity Clone() => new PreservedPropertyEntity();
}

public class InitialPriorityPropertyEntity : ICardPropertyEntity
{
    public CardProperty Property => CardProperty.Initialize;
    public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

    public InitialPriorityPropertyEntity() { }
    public int Eval(TriggerContext triggerContext) => 0;
    public ICardPropertyEntity Clone() => new InitialPriorityPropertyEntity();
}   

public class ConsumablePropertyEntity : ICardPropertyEntity
{
    public CardProperty Property => CardProperty.Consumable;
    public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

    public ConsumablePropertyEntity() { }
    public int Eval(TriggerContext triggerContext) => 0;
    public ICardPropertyEntity Clone() => new ConsumablePropertyEntity();
}

public class DisposePropertyEntity : ICardPropertyEntity
{
    public CardProperty Property => CardProperty.Dispose;
    public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

    public DisposePropertyEntity() { }
    public int Eval(TriggerContext triggerContext) => 0;
    public ICardPropertyEntity Clone() => new DisposePropertyEntity();
}

public class AutoDisposePropertyEntity : ICardPropertyEntity
{
    public CardProperty Property => CardProperty.AutoDispose;
    public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

    public AutoDisposePropertyEntity() { }
    public int Eval(TriggerContext triggerContext) => 0;
    public ICardPropertyEntity Clone() => new AutoDisposePropertyEntity();
}

public class SealedPropertyEntity : ICardPropertyEntity
{
    public CardProperty Property => CardProperty.Sealed;
    public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

    public SealedPropertyEntity() { }
    public int Eval(TriggerContext triggerContext) => 0;
    public ICardPropertyEntity Clone() => new SealedPropertyEntity();
}

public class RecyclePropertyEntity : ICardPropertyEntity
{
    public CardProperty Property => CardProperty.Recycle;
    public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

    public RecyclePropertyEntity() { }
    public int Eval(TriggerContext triggerContext) => 0;
    public ICardPropertyEntity Clone() => new RecyclePropertyEntity();
}