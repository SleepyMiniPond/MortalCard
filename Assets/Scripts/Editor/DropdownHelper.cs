using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public static class DropdownHelper
{
    const string BuffFolderPath = "Assets/ScriptableObjects/PlayerBuff";
    const string CardBuffFolderPath = "Assets/ScriptableObjects/CardBuff";
    const string AssetExtension = "*.asset";

    public static IEnumerable<ValueDropdownItem> PlayerBuffNames
    {
        get
        {
            if (Directory.Exists(BuffFolderPath))
            {
                var assetPaths = Directory.GetFiles(BuffFolderPath, AssetExtension);
                foreach (var assetPath in assetPaths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<PlayerBuffDataScriptable>(assetPath);
                    if (asset != null && string.IsNullOrEmpty(asset.Data.ID) == false)
                    {
                        yield return new ValueDropdownItem(asset.Data.ID, asset.Data.ID);
                    }
                }
            }
        }
    }

    public static IEnumerable<ValueDropdownItem> CardBuffNames
    {
        get
        {
            if (Directory.Exists(CardBuffFolderPath))
            {
                var assetPaths = Directory.GetFiles(CardBuffFolderPath, AssetExtension);
                foreach (var assetPath in assetPaths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<CardBuffScriptable>(assetPath);
                    if (asset != null && string.IsNullOrEmpty(asset.Data.ID) == false)
                    {
                        yield return new ValueDropdownItem(asset.Data.ID, asset.Data.ID);
                    }
                }
            }
        }
    }
    
    public static IEnumerable<ValueDropdownItem<GameTiming>> UpdateTimings
    { 
        get
        { 
            var options = new List<ValueDropdownItem<GameTiming>>();

            foreach (GameTiming timing in Enum.GetValues(typeof(GameTiming)))
            {
                var field = typeof(GameTiming).GetField(timing.ToString());
                var attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                .FirstOrDefault() as DescriptionAttribute;

                string displayName = attribute?.Description ?? timing.ToString();
                options.Add(new ValueDropdownItem<GameTiming>(displayName, timing));
            }

            return options;
        }
    }
}