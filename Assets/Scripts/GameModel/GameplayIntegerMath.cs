using Optional;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 定義所有會影響 Gameplay 結果的整數運算政策。
    /// </summary>
    public static class GameplayIntegerMath
    {
        public static Option<int> Add(int left, int right)
        {
            return _Saturate((long)left + right).Some();
        }

        public static Option<int> Subtract(int left, int right)
        {
            return _Saturate((long)left - right).Some();
        }

        public static Option<int> Multiply(int left, int right)
        {
            return _Saturate((long)left * right).Some();
        }

        public static Option<int> Divide(int dividend, int divisor)
        {
            if (divisor == 0)
            {
                return Option.None<int>();
            }

            if (dividend == int.MinValue && divisor == -1)
            {
                return int.MaxValue.Some();
            }

            var quotient = dividend / divisor;
            var remainder = dividend % divisor;
            var requiresFloorAdjustment = remainder != 0 && (dividend < 0) != (divisor < 0);

            return (requiresFloorAdjustment ? quotient - 1 : quotient).Some();
        }

        public static Option<int> Remainder(int dividend, int divisor)
        {
            return divisor == 0
                ? Option.None<int>()
                : (dividend % divisor).Some();
        }

        private static int _Saturate(long value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            return (int)value;
        }
    }
}
