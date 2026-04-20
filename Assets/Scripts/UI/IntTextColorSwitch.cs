using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class IntTextColorSwitch : IntValueSwitch
{
    [SerializeField]
    private TextMeshProUGUI _text;

    [SerializeField]
    private IntColorMapping _colorMapping;
    public override int Value
    {
        set { 
            if (_text == null)
            {
                _text = GetComponent<TextMeshProUGUI>();
            }
            
            _text.color = _colorMapping.GetColor(value);
        }        
    }
}
