using System;

public interface IBooleanValue
{
    bool Eval(TriggerContext triggerContext);
}

[Serializable]
public class TrueValue : IBooleanValue
{
    public bool Eval(TriggerContext triggerContext)
    {
        return true;
    }
}

[Serializable]
public class FalseValue : IBooleanValue
{
    public bool Eval(TriggerContext triggerContext)
    {
        return false;
    }
}