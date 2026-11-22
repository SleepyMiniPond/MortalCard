using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MortalGame.GameData;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MortalGame.Editor
{
    /// <summary>
    /// 僅引導 Inspector 的效果選擇，不改寫資產或限制執行時效果。
    /// </summary>
    public static class EffectMenuFilter
    {
        public static IReadOnlyList<EffectMenuOption> GetOptions(InspectorProperty property, EffectMenuSettings settings = null)
        {
            settings ??= EffectMenuSettings.Load();
            var effectType = ((FieldInfo)property.Info.GetMemberInfo()).FieldType;
            var timings = _GetTimings(property);
            return new[] { new EffectMenuOption(null, true) }
                .Concat(TypeCache.GetTypesDerivedFrom(effectType)
                .Where(type => type.Assembly == typeof(ICardEffect).Assembly &&
                    !type.IsAbstract && !type.IsGenericTypeDefinition)
                .Select(type => new EffectMenuOption(type, timings.Count == 0 || !timings.All(timing => settings.IsDisabled(timing, type))))
                .OrderBy(option => !option.Enabled)
                .ThenBy(option => option.Type.Name))
                .ToArray();
        }

        public static bool TrySetEffect(InspectorProperty property, Type type, EffectMenuSettings settings = null)
        {
            if (!GetOptions(property, settings).Any(option => option.Type == type && option.Enabled)) return false;
            property.RecordForUndo("變更條件效果");
            for (var i = 0; i < property.ValueEntry.ValueCount; i++)
                property.ValueEntry.WeakValues[i] = type == null ? null : Activator.CreateInstance(type);
            property.ValueEntry.ApplyChanges();
            property.State.Expanded = type != null;
            return true;
        }

        private static IReadOnlyList<object> _GetTimings(InspectorProperty property)
        {
            for (var parent = property.Parent; parent != null; parent = parent.Parent)
            {
                var type = parent.ValueEntry?.TypeOfValue;
                if (type == null || !type.IsGenericType ||
                    type.GetGenericTypeDefinition() != typeof(EditableKeyValuePair<,>))
                    continue;

                var key = parent.Children["Key"]?.ValueEntry;
                if (key == null || (key.TypeOfValue != typeof(GameTiming) && key.TypeOfValue != typeof(CardTriggeredTiming)))
                    continue;

                // 多物件編輯只停用所有選取時機共同限制的型別。
                return key.WeakValues.Cast<object>().ToArray();
            }
            return Array.Empty<object>();
        }
    }

    public sealed class EffectMenuOption
    {
        public Type Type { get; }
        public bool Enabled { get; }

        public EffectMenuOption(Type type, bool enabled)
        {
            Type = type;
            Enabled = enabled;
        }
    }

    public sealed class EffectMenuFilterAttribute : Attribute { }

    public sealed class EffectMenuFilterAttributeProcessor : OdinAttributeProcessor
    {
        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member) =>
            member.Name == "Effect" &&
            (member.DeclaringType == typeof(ConditionalCardEffect) ||
             member.DeclaringType == typeof(ConditionalCardBuffEffect) ||
             member.DeclaringType == typeof(ConditionalPlayerBuffEffect) ||
             member.DeclaringType == typeof(ConditionalCharacterBuffEffect));

        public override void ProcessChildMemberAttributes(
            InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            attributes.Add(new EffectMenuFilterAttribute());
        }
    }

    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public sealed class EffectMenuFilterAttributeDrawer : OdinAttributeDrawer<EffectMenuFilterAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var value = Property.ValueEntry.WeakSmartValue;
            var rect = EditorGUILayout.GetControlRect();
            var valueRect = label == null ? rect : EditorGUI.PrefixLabel(rect, label);
            if (value != null)
            {
                var foldoutRect = new Rect(rect.x - 12, rect.y, 12, rect.height);
                Property.State.Expanded = EditorGUI.Foldout(foldoutRect, Property.State.Expanded, GUIContent.none);
            }
            if (EditorGUI.DropdownButton(valueRect, new GUIContent(value?.GetType().Name ?? "Null"), FocusType.Keyboard))
            {
                var selector = new EffectMenuSelector(EffectMenuFilter.GetOptions(Property));
                selector.EnableSingleClickToSelect();
                selector.SelectionConfirmed += selected =>
                {
                    var option = selected.FirstOrDefault();
                    if (option == null) return;
                    Property.Tree.DelayActionUntilRepaint(() => EffectMenuFilter.TrySetEffect(Property, option.Type));
                };
                selector.ShowInPopup(valueRect);
            }
            if (value != null && Property.State.Expanded)
                using (new EditorGUI.IndentLevelScope())
                    foreach (var child in Property.Children) child.Draw();
        }
    }

    public sealed class EffectMenuSelector : OdinSelector<EffectMenuOption>
    {
        private readonly IReadOnlyList<EffectMenuOption> _options;

        public EffectMenuSelector(IReadOnlyList<EffectMenuOption> options) => _options = options;

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;
            tree.Selection.SupportsMultiSelect = false;
            foreach (var option in _options)
                tree.MenuItems.Add(new OdinMenuItem(tree, option.Type?.Name ?? "Null", option)
                {
                    IsEnabled = option.Enabled,
                    IsSelectable = option.Enabled
                });
        }

        public override bool IsValidSelection(IEnumerable<EffectMenuOption> selection) =>
            selection.Any() && selection.All(option => option.Enabled);
    }
}
