using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEffectAttribute
{
    void ApplyModify(EffectAttributeAdditionType attribute, int value);
    void ApplyModify(EffectAttributeRatioType attribute, float value);

    IReadOnlyDictionary<EffectAttributeAdditionType, int> IntValues { get; }
    IReadOnlyDictionary<EffectAttributeRatioType, float> FloatValues { get; }
}

public class CardPlayAttributeEntity : IEffectAttribute
{
    private readonly Dictionary<EffectAttributeAdditionType, int> _intValues;
    private readonly Dictionary<EffectAttributeRatioType, float> _floatValues;

    public IReadOnlyDictionary<EffectAttributeAdditionType, int> IntValues => _intValues;
    public IReadOnlyDictionary<EffectAttributeRatioType, float> FloatValues => _floatValues;

    public CardPlayAttributeEntity()
    {
        _intValues = new Dictionary<EffectAttributeAdditionType, int>
        {
            { EffectAttributeAdditionType.CostAddition, 0 },
            { EffectAttributeAdditionType.PowerAddition, 0 },
            { EffectAttributeAdditionType.NormalDamageAddition, 0 }
        };

        _floatValues = new Dictionary<EffectAttributeRatioType, float>
        {
            { EffectAttributeRatioType.NormalDamageRatio, 0f }
        };
    }

    public void ApplyModify(EffectAttributeAdditionType attribute, int value)
    {
        if (!_intValues.ContainsKey(attribute))
            _intValues[attribute] = 0;

        _intValues[attribute] += value;
    }
    public void ApplyModify(EffectAttributeRatioType attribute, float value)
    {
        if (!_floatValues.ContainsKey(attribute))
            _floatValues[attribute] = 0f;

        _floatValues[attribute] += value;
    }
}
