using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MortalGame.Editor
{
    /// <summary>
    /// 遍歷遊戲資料的公開序列化欄位，供 Editor Validator 檢查深層必填引用。
    /// </summary>
    internal static class SerializedDataGraphUtility
    {
        public static IReadOnlyList<ValidationIssue> ValidateRequiredReferences(
            object root,
            string context)
        {
            var errors = new List<ValidationIssue>();
            var visited = new HashSet<object>(ReferenceComparer.Instance);
            _Validate(root, context, visited, errors);
            return errors;
        }

        public static IReadOnlyList<T> Find<T>(object root, Func<object, bool> descend = null)
        {
            var results = new List<T>();
            var visited = new HashSet<object>(ReferenceComparer.Instance);
            _Find(root, visited, results, descend);
            return results;
        }

        private static void _Validate(
            object value,
            string context,
            ISet<object> visited,
            ICollection<ValidationIssue> errors)
        {
            if (value == null || value is UnityEngine.Object unityObject && unityObject == null)
            {
                errors.Add(new ValidationIssue.MissingRequiredValue(context));
                return;
            }

            if (_IsTerminal(value.GetType()))
                return;

            if (!visited.Add(value))
                return;

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    var key = entry.Key?.ToString() ?? "<null>";
                    _Validate(entry.Value, $"{context}[{key}]", visited, errors);
                }

                return;
            }

            if (value is IEnumerable enumerable)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    _Validate(item, $"{context}[{index}]", visited, errors);
                    index++;
                }

                return;
            }

            foreach (var field in value.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType == typeof(string))
                    continue;

                _Validate(
                    field.GetValue(value),
                    $"{context}.{field.Name}",
                    visited,
                    errors);
            }
        }

        private static void _Find<T>(
            object value,
            ISet<object> visited,
            ICollection<T> results,
            Func<object, bool> descend)
        {
            if (value == null || value is UnityEngine.Object || descend != null && !descend(value))
                return;

            if (value is T match)
                results.Add(match);

            if (_IsTerminal(value.GetType()) || !visited.Add(value))
                return;

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                    _Find(entry.Value, visited, results, descend);
                return;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                    _Find(item, visited, results, descend);
                return;
            }

            foreach (var field in value.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType != typeof(string))
                    _Find(field.GetValue(value), visited, results, descend);
            }
        }

        private static bool _IsTerminal(Type type)
        {
            return type.IsValueType || type == typeof(string);
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static ReferenceComparer Instance { get; } = new();

            public new bool Equals(object left, object right)
            {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(object value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}
