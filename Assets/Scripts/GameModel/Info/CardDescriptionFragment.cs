using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MortalGame.GameData;

namespace MortalGame.GameModel
{
    public sealed record CardDescriptionFragment(
        string Key, IReadOnlyDictionary<string, string> Values, IReadOnlyDictionary<string, string> Rules = null)
    {
        public static CardDescriptionFragment Create(CardEffectOption option)
        {
            var values = new Dictionary<string, string>();
            var rules = new Dictionary<string, string>();
            foreach (var pair in option.DescriptionParameters)
            {
                // 只算純常數運算；依目標、時機或亂數決定的值顯示規則，不執行任意 Eval。
                if (IsStatic(pair.Value.Value) && pair.Value.Value.Eval(null).TryGetValue(out var value))
                    values.Add(pair.Key, value.ToString(CultureInfo.InvariantCulture));
                else rules.Add(pair.Key, pair.Value.RuleKey);
            }
            return new CardDescriptionFragment(option.DescriptionKey, values, rules);
        }

        public static bool IsStatic(IIntegerValue value) => value switch
        {
            ConstInteger => true,
            ArithmeticInteger arithmetic => IsStatic(arithmetic.Left) && IsStatic(arithmetic.Right),
            MinimumInteger minimum => minimum.Values.All(IsStatic),
            MaximumInteger maximum => maximum.Values.All(IsStatic),
            _ => false
        };
    }
}
