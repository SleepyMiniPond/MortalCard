using System;
using MortalGame.GameData;
using System.Collections.Generic;
using System.Linq;
using Optional;
using Sirenix.OdinInspector;

namespace MortalGame.GameModel
{

    public interface IIntegerValue
    {
        Option<int> Eval(TriggerContext triggerContext);
    }

    [Serializable]
    public class ConstInteger : IIntegerValue
    {
        public int Value;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return Value.Some();
        }
    }

    [Serializable]
    public class TurnCountInteger : IIntegerValue
    {
        public Option<int> Eval(TriggerContext triggerContext)
        {
            return triggerContext.Model.GameStatus.TurnCount.Some();
        }
    }

    [Serializable]
    public class ArithmeticInteger : IIntegerValue
    {
        public ArithmeticType Operation;
        public IIntegerValue Left;
        public IIntegerValue Right;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return Left
                .Eval(triggerContext)
                .Combine(Right.Eval(triggerContext))
                .FlatMap(values => Operation switch
                {
                    ArithmeticType.Add => GameplayIntegerMath.Add(values.Item1, values.Item2),
                    ArithmeticType.Subtract => GameplayIntegerMath.Subtract(values.Item1, values.Item2),
                    ArithmeticType.Multiply => GameplayIntegerMath.Multiply(values.Item1, values.Item2),
                    ArithmeticType.Divide => GameplayIntegerMath.Divide(values.Item1, values.Item2),
                    ArithmeticType.Remainder => GameplayIntegerMath.Remainder(values.Item1, values.Item2),
                    _ => Option.None<int>()
                });
        }
    }

    [Serializable]
    public class MinimumInteger : IIntegerValue
    {
        [ShowInInspector]
        public List<IIntegerValue> Values = new();

        public Option<int> Eval(TriggerContext triggerContext)
        {
            if (Values.Count == 0)
            {
                return Option.None<int>();
            }

            var firstValue = Values[0].Eval(triggerContext);
            if (!firstValue.TryGetValue(out var minimum))
            {
                return Option.None<int>();
            }

            foreach (var value in Values.Skip(1))
            {
                var evaluatedValue = value.Eval(triggerContext);
                if (!evaluatedValue.TryGetValue(out var currentValue))
                {
                    return Option.None<int>();
                }

                minimum = Math.Min(minimum, currentValue);
            }

            return minimum.Some();
        }
    }

    [Serializable]
    public class MaximumInteger : IIntegerValue
    {
        [ShowInInspector]
        public List<IIntegerValue> Values = new();

        public Option<int> Eval(TriggerContext triggerContext)
        {
            if (Values.Count == 0)
            {
                return Option.None<int>();
            }

            var firstValue = Values[0].Eval(triggerContext);
            if (!firstValue.TryGetValue(out var maximum))
            {
                return Option.None<int>();
            }

            foreach (var value in Values.Skip(1))
            {
                var evaluatedValue = value.Eval(triggerContext);
                if (!evaluatedValue.TryGetValue(out var currentValue))
                {
                    return Option.None<int>();
                }

                maximum = Math.Max(maximum, currentValue);
            }

            return maximum.Some();
        }
    }

    [Serializable]
    public class CardIntegerProperty : IIntegerValue
    {
        public enum CardIntegerValueType
        {
            CardPower = 0,
            CardCost = 1,
            CardBasePower = 2,
            CardBaseCost = 3,
        }

        [HorizontalGroup("1")]
        public ITargetCardValue Card;
        public CardIntegerValueType Property;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return Card
                .Eval(triggerContext)
                .FlatMap(
                    card => Property switch
                    {
                        CardIntegerValueType.CardPower => GameFormula.CardPower(triggerContext, card),
                        CardIntegerValueType.CardCost => GameFormula.CardCost(triggerContext, card),
                        CardIntegerValueType.CardBasePower => card.OriginPower.Some(),
                        CardIntegerValueType.CardBaseCost => card.OriginCost.Some(),
                        _ => Option.None<int>()
                    });
        }
    }

    [Serializable]
    public class PlayerIntegerProperty : IIntegerValue
    {
        public enum PlayerIntegerValueType
        {
            MaxEnergy,
            CurrentEnergy,
            CurrentDisposition
        }

        [HorizontalGroup("1")]
        public ITargetPlayerValue Player;
        public PlayerIntegerValueType Property;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return Player
                .Eval(triggerContext)
                .FlatMap(player =>
                    Property switch
                    {
                        PlayerIntegerValueType.MaxEnergy => player.MaxEnergy.Some(),
                        PlayerIntegerValueType.CurrentEnergy => player.CurrentEnergy.Some(),
                        PlayerIntegerValueType.CurrentDisposition =>
                            player is IPlayerDispositionEntity dispositionPlayer
                                ? dispositionPlayer.DispositionManager.CurrentDisposition.Some()
                                : Option.None<int>(),
                        _ => Option.None<int>()
                    });
        }
    }

    [Serializable]
    public class CharacterIntegerProperty : IIntegerValue
    {
        public enum CharacterIntegerValueType
        {
            CurrentHealth,
            MaxHealth,
            CurrentShield
        }

        [HorizontalGroup("1")]
        public ITargetCharacterValue Character;
        public CharacterIntegerValueType Property;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return Character
                .Eval(triggerContext)
                .FlatMap(character =>
                    Property switch
                    {
                        CharacterIntegerValueType.CurrentHealth => character.CurrentHealth.Some(),
                        CharacterIntegerValueType.MaxHealth => character.MaxHealth.Some(),
                        CharacterIntegerValueType.CurrentShield => character.CurrentShield.Some(),
                        _ => Option.None<int>()
                    });
        }
    }

    [Serializable]
    public class CardBuffIntegerProperty : IIntegerValue
    {
        public enum CardBuffIntegerValueType
        {
            Level
        }

        [HorizontalGroup("1")]
        public ITargetCardBuffValue CardBuff;
        public CardBuffIntegerValueType Property;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return CardBuff
                .Eval(triggerContext)
                .FlatMap(
                    cardBuff => Property switch
                    {
                        CardBuffIntegerValueType.Level => cardBuff.Level.Some(),
                        _ => Option.None<int>()
                    });
        }
    }

    [Serializable]
    public class PlayerBuffIntegerProperty : IIntegerValue
    {
        public enum PlayerBuffIntegerValueType
        {
            Level,
        }

        [HorizontalGroup("1")]
        public ITargetPlayerBuffValue PlayerBuff;
        public PlayerBuffIntegerValueType Property;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return PlayerBuff
                .Eval(triggerContext)
                .FlatMap(
                    playerBuff => Property switch
                    {
                        PlayerBuffIntegerValueType.Level => playerBuff.Level.Some(),
                        _ => Option.None<int>()
                    });
        }
    }

    [Serializable]
    public class PlayerBuffSessionInteger : IIntegerValue
    {
        [HorizontalGroup("1")]
        public ITargetPlayerBuffValue PlayerBuff;
        public string SessionIntegerId;

        public Option<int> Eval(TriggerContext triggerContext)
        {
            return PlayerBuff
                .Eval(triggerContext)
                .FlatMap(playerBuff => playerBuff.GetSessionInteger(SessionIntegerId));
        }
    }

    [Serializable]
    public class ConditionalValue : IIntegerValue
    {
        [Serializable]
        public class ConditionPair
        {
            [ShowInInspector]
            [HorizontalGroup("1")]
            public List<IPlayerBuffCondition> Conditions = new();

            [HorizontalGroup("2")]
            public IIntegerValue Value;
        }

        [ShowInInspector]
        [HorizontalGroup("1")]
        public List<ConditionPair> Pairs = new();

        public Option<int> Eval(TriggerContext triggerContext)
        {
            foreach (var pair in Pairs)
            {
                if (pair.Conditions.All(condition => condition.Eval(triggerContext)))
                {
                    return pair.Value.Eval(triggerContext);
                }
            }
            return Option.None<int>();
        }
    }

}
