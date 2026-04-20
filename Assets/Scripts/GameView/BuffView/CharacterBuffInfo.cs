using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBuffInfo
{
    public readonly string Id;
    public readonly Guid Identity;
    public readonly int Level;
    public IReadOnlyDictionary<string, int> SessionIntegers;

    public CharacterBuffInfo(string id, Guid identity, int level, Dictionary<string, int> sessionIntegers)
    {
        Id = id;
        Identity = identity;
        Level = level;
        SessionIntegers = sessionIntegers;
    }    

    public const string KEY_LEVEL = "level";

    public Dictionary<string, string> GetTemplateValues()
    {
        var template = SessionIntegers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        template.Add(KEY_LEVEL, Level.ToString());
        return template;
    }
}