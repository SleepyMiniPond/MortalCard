using System;
using MortalGame.Editor.Exceptions;
using System.Collections.Generic;
using System.Linq;
using MortalGame.GameData;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;

namespace MortalGame.Editor
{
    /// <summary>
    /// 僅供 Inspector 選單使用的停用規則，不限制既有資產或 Runtime。
    /// </summary>
    public sealed class EffectMenuSettings : SerializedScriptableObject
    {
        [LabelText("全局時機規則")]
        [OdinSerialize, NonSerialized]
        public List<EffectMenuRule<GameTiming>> GameTimingRules = new();

        [LabelText("卡片時機規則")]
        [OdinSerialize, NonSerialized]
        public List<EffectMenuRule<CardTriggeredTiming>> CardTimingRules = new();

        public bool IsDisabled(object timing, Type effectType) => timing switch
        {
            GameTiming gameTiming => GameTimingRules.Any(rule => rule.Timing == gameTiming && rule.DisabledEffects.Contains(effectType)),
            CardTriggeredTiming cardTiming => CardTimingRules.Any(rule => rule.Timing == cardTiming && rule.DisabledEffects.Contains(effectType)),
            _ => false
        };

        public static EffectMenuSettings Load() =>
            AssetDatabase.LoadAssetAtPath<EffectMenuSettings>(ProjectAssetPaths.EditorSettings.EffectMenu)
            ?? throw new MissingEffectMenuSettingsException(ProjectAssetPaths.EditorSettings.EffectMenu);

        [MenuItem("MortalGame/編輯器設定/效果選單規則")]
        public static void SelectSettings()
        {
            var settings = Load();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        public static IEnumerable<ValueDropdownItem<Type>> EffectTypes => typeof(ICardEffect).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition &&
                (typeof(ICardEffect).IsAssignableFrom(type) || typeof(IReactionEffect).IsAssignableFrom(type)))
            .OrderBy(type => type.Name)
            .Select(type => new ValueDropdownItem<Type>(type.Name, type));
    }

    [Serializable]
    [InlineProperty, HideReferenceObjectPicker]
    public sealed class EffectMenuRule<TTiming> where TTiming : Enum
    {
        [LabelText("時機")]
        public TTiming Timing;

        [OdinSerialize]
        [LabelText("停用效果")]
        [ValueDropdown("@MortalGame.Editor.EffectMenuSettings.EffectTypes")]
        public List<Type> DisabledEffects = new();
    }
}
