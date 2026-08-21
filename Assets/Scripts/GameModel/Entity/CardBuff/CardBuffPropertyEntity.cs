using System.Collections.Generic;
using MortalGame.GameData;
using System.Linq;
using Optional;
using UnityEngine;

namespace MortalGame.GameModel
{

    public interface ICardBuffPropertyEntity
    {
        CardProperty Property { get; }
        IEnumerable<string> Keywords { get; }

        Option<int> Eval(TriggerContext triggerContext);

        ICardBuffPropertyEntity Clone();
    }

    public class SealedCardBuffPropertyEntity : ICardBuffPropertyEntity
    {
        public CardProperty Property => CardProperty.Sealed;
        public IEnumerable<string> Keywords => Property.ToString().WrapAsEnumerable();

        public SealedCardBuffPropertyEntity() { }
        public Option<int> Eval(TriggerContext triggerContext) => 0.Some();

        public ICardBuffPropertyEntity Clone() => new SealedCardBuffPropertyEntity();
    }

    public class PowerCardBuffPropertyEntity : ICardBuffPropertyEntity
    {
        public CardProperty Property => CardProperty.PowerAddition;
        public IEnumerable<string> Keywords => Enumerable.Empty<string>();

        private readonly IIntegerValue _value;

        public PowerCardBuffPropertyEntity(IIntegerValue value)
        {
            _value = value;
        }
        public Option<int> Eval(TriggerContext triggerContext)
        {
            var cardBuffPropertyContext = triggerContext with { Action = new CardBuffPropertyLookAction(this) };
            return _value.Eval(cardBuffPropertyContext);
        }

        public ICardBuffPropertyEntity Clone() => new PowerCardBuffPropertyEntity(_value);
    }

}
