using System;
using System.Collections.Generic;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Editor
{
    /// <summary>
    /// 檢查巢狀條件與整數運算的語意；資產遍歷及診斷彙整由 GameDataValidator 負責。
    /// </summary>
    internal static class ValueConditionValidator
    {
        internal static void ValidateConditions(
            object data,
            string context,
            ICollection<string> errors)
        {
            foreach (var condition in SerializedDataGraphUtility.Find<AllCondition>(data))
            {
                if (condition.Conditions == null || condition.Conditions.Count == 0)
                    errors.Add($"{context} 的 AllCondition.Conditions 至少需要一項");
            }

            foreach (var condition in SerializedDataGraphUtility.Find<AnyCondition>(data))
            {
                if (condition.Conditions == null || condition.Conditions.Count == 0)
                    errors.Add($"{context} 的 AnyCondition.Conditions 至少需要一項");
            }

            foreach (var condition in SerializedDataGraphUtility.Find<GameTimingCondition>(data))
            {
                if (condition.Timing == GameTiming.None ||
                    !Enum.IsDefined(typeof(GameTiming), condition.Timing))
                {
                    errors.Add(
                        $"{context} 的 GameTimingCondition.Timing 必須是有效 Timing：{condition.Timing}");
                }
            }

            foreach (var condition in SerializedDataGraphUtility.Find<IntegerCompare>(data))
            {
                if (condition.Arithmetic == ArithmeticConditionType.None ||
                    !Enum.IsDefined(typeof(ArithmeticConditionType), condition.Arithmetic))
                {
                    errors.Add(
                        $"{context} 的 IntegerCompare.Arithmetic 必須是有效比較運算：{condition.Arithmetic}");
                }
            }
        }

        internal static void ValidateIntegerValues(
            object data,
            string context,
            ICollection<string> errors)
        {
            foreach (var arithmetic in SerializedDataGraphUtility.Find<ArithmeticInteger>(data))
            {
                if (arithmetic.Operation == ArithmeticType.None)
                {
                    errors.Add($"{context} 的 ArithmeticInteger.Operation 不可為 None");
                }
                else if (arithmetic.Operation == ArithmeticType.Overwrite)
                {
                    errors.Add(
                        $"{context} 的 ArithmeticInteger.Operation 不支援 Overwrite");
                }

                if (arithmetic.Operation is ArithmeticType.Divide or ArithmeticType.Remainder &&
                    arithmetic.Right is ConstInteger { Value: 0 })
                {
                    errors.Add(
                        $"{context} 的 ArithmeticInteger.{arithmetic.Operation} 除數不可為 0");
                }

                if (_HasStaticArithmeticOverflow(arithmetic))
                {
                    errors.Add(
                        $"{context} 的 ArithmeticInteger.{arithmetic.Operation} 常數運算結果超出 Int32 範圍");
                }
            }

            foreach (var minimum in SerializedDataGraphUtility.Find<MinimumInteger>(data))
            {
                if (minimum.Values == null || minimum.Values.Count == 0)
                    errors.Add($"{context} 的 MinimumInteger.Values 至少需要一項");
            }

            foreach (var maximum in SerializedDataGraphUtility.Find<MaximumInteger>(data))
            {
                if (maximum.Values == null || maximum.Values.Count == 0)
                    errors.Add($"{context} 的 MaximumInteger.Values 至少需要一項");
            }

            foreach (var cardValue in SerializedDataGraphUtility.Find<CardIntegerProperty>(data))
            {
                if (!Enum.IsDefined(
                    typeof(CardIntegerProperty.CardIntegerValueType),
                    cardValue.Property))
                {
                    errors.Add(
                        $"{context} 的 CardIntegerProperty.Property 無效：{cardValue.Property}");
                }
            }

            foreach (var characterValue in SerializedDataGraphUtility.Find<CharacterIntegerProperty>(data))
            {
                if (!Enum.IsDefined(
                    typeof(CharacterIntegerProperty.CharacterIntegerValueType),
                    characterValue.Property))
                {
                    errors.Add(
                        $"{context} 的 CharacterIntegerProperty.Property 無效：{characterValue.Property}");
                }
            }

            foreach (var playerValue in SerializedDataGraphUtility.Find<PlayerIntegerProperty>(data))
            {
                if (!Enum.IsDefined(
                    typeof(PlayerIntegerProperty.PlayerIntegerValueType),
                    playerValue.Property))
                {
                    errors.Add(
                        $"{context} 的 PlayerIntegerProperty.Property 無效：{playerValue.Property}");
                }
                else if (playerValue.Property ==
                         PlayerIntegerProperty.PlayerIntegerValueType.CurrentDisposition &&
                         playerValue.Player is PlayerByFaction { Faction: Faction.Enemy })
                {
                    errors.Add(
                        $"{context} 的 PlayerIntegerProperty.CurrentDisposition 不可指定 Enemy");
                }
            }
        }

        private static bool _HasStaticArithmeticOverflow(ArithmeticInteger arithmetic)
        {
            if (!TryEvaluateStaticInteger(arithmetic.Left, out var left) ||
                !TryEvaluateStaticInteger(arithmetic.Right, out var right))
            {
                return false;
            }

            return arithmetic.Operation switch
            {
                ArithmeticType.Add => (long)left + right is > int.MaxValue or < int.MinValue,
                ArithmeticType.Subtract => (long)left - right is > int.MaxValue or < int.MinValue,
                ArithmeticType.Multiply => (long)left * right is > int.MaxValue or < int.MinValue,
                ArithmeticType.Divide => left == int.MinValue && right == -1,
                _ => false
            };
        }

        internal static bool TryEvaluateStaticInteger(IIntegerValue value, out int result)
        {
            switch (value)
            {
                case ConstInteger constant:
                    result = constant.Value;
                    return true;
                case ArithmeticInteger arithmetic
                    when TryEvaluateStaticInteger(arithmetic.Left, out var left) &&
                         TryEvaluateStaticInteger(arithmetic.Right, out var right):
                    return _TryEvaluateStaticArithmetic(
                        arithmetic.Operation,
                        left,
                        right,
                        out result);
                case MinimumInteger minimum
                    when minimum.Values != null && minimum.Values.Count > 0:
                    return _TryEvaluateStaticExtremum(minimum.Values, true, out result);
                case MaximumInteger maximum
                    when maximum.Values != null && maximum.Values.Count > 0:
                    return _TryEvaluateStaticExtremum(maximum.Values, false, out result);
                default:
                    result = default;
                    return false;
            }
        }

        private static bool _TryEvaluateStaticArithmetic(
            ArithmeticType operation,
            int left,
            int right,
            out int result)
        {
            var evaluated = operation switch
            {
                ArithmeticType.Add => GameplayIntegerMath.Add(left, right),
                ArithmeticType.Subtract => GameplayIntegerMath.Subtract(left, right),
                ArithmeticType.Multiply => GameplayIntegerMath.Multiply(left, right),
                ArithmeticType.Divide => GameplayIntegerMath.Divide(left, right),
                ArithmeticType.Remainder => GameplayIntegerMath.Remainder(left, right),
                _ => Optional.Option.None<int>()
            };
            return evaluated.TryGetValue(out result);
        }

        private static bool _TryEvaluateStaticExtremum(
            IReadOnlyList<IIntegerValue> values,
            bool minimum,
            out int result)
        {
            if (!TryEvaluateStaticInteger(values[0], out result))
                return false;

            for (var index = 1; index < values.Count; index++)
            {
                if (!TryEvaluateStaticInteger(values[index], out var current))
                    return false;

                result = minimum
                    ? Math.Min(result, current)
                    : Math.Max(result, current);
            }

            return true;
        }
    }
}
