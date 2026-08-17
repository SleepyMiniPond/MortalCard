using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using MortalGame.GameData;

namespace MortalGame.Editor
{

    public static class DropdownHelper
    {
        const string AssetExtension = "*.asset";

        public static IEnumerable<ValueDropdownItem> PlayerBuffNames
        {
            get
            {
                if (Directory.Exists(ProjectAssetPaths.GameContent.PlayerBuffFolder))
                {
                    var assetPaths = Directory.GetFiles(
                        ProjectAssetPaths.GameContent.PlayerBuffFolder,
                        AssetExtension);
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
                if (Directory.Exists(ProjectAssetPaths.GameContent.CardBuffFolder))
                {
                    var assetPaths = Directory.GetFiles(
                        ProjectAssetPaths.GameContent.CardBuffFolder,
                        AssetExtension);
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
}
