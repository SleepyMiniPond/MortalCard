using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class IntColorMapping : SerializedMonoBehaviour
{
    [SerializeField]
    private Dictionary<int, Color> _colorMap = new();

    private Dictionary<int, string> _colorHtmlMap = new();

    private void Awake()
    {
        _colorHtmlMap = _colorMap.ToDictionary(
            kvp => kvp.Key,
            kvp => ColorUtility.ToHtmlStringRGBA(kvp.Value)
        );
    }

    public Color GetColor(int value)
    {
        if (_colorMap.TryGetValue(value, out var color))
        {
            return color;
        }
        else
        {
            return Color.white;
        }
    }

    public string GetHtmlColor(int value)
    {
        if (_colorHtmlMap.TryGetValue(value, out var color))
        {
            return color;
        }
        else
        {
            return ColorUtility.ToHtmlStringRGBA(Color.white);
        }
    }
}
