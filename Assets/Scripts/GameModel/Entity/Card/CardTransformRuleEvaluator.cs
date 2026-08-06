using System.Linq;
using MortalGame.GameData;
using Optional;
using Optional.Collections;

namespace MortalGame.GameModel
{
    public sealed record CardFormRuleContext(
        IGameplayModel Model,
        ICardEntity Owner,
        IActionUnit Action,
        bool IsSelfTransformSuppressed = false)
    {
        public TriggerContext TriggerContext => new(
            Model,
            new CardTrigger(Owner),
            Action);
    }

    public abstract record CardFormOperation(
        string RuleId,
        string TransformKey);

    public sealed record ApplyCardFormOperation(
        string RuleId,
        string TransformKey,
        string TargetCardDataId,
        CardFormPersistence Persistence) : CardFormOperation(RuleId, TransformKey);

    public sealed record RevertCardFormOperation(
        string RuleId,
        string TransformKey) : CardFormOperation(RuleId, TransformKey);

    public static class CardTransformRuleEvaluator
    {
        public static Option<CardFormOperation> Evaluate(
            GameTiming timing,
            CardFormRuleContext context)
        {
            if (context.IsSelfTransformSuppressed)
                return Option.None<CardFormOperation>();

            var baseCardData = context.Model.ContextManager.CardLibrary
                .GetStandardCardData(context.Owner.BaseCardDataId);
            var orderedRules = baseCardData.TransformRules
                .Select((rule, index) => (Rule: rule, Index: index))
                .Where(pair => pair.Rule.Timing == timing)
                .Where(pair => pair.Rule.Conditions.All(
                    condition => condition.Eval(context.TriggerContext)))
                .OrderByDescending(pair => pair.Rule.Priority)
                .ThenBy(pair => pair.Index)
                .Select(pair => pair.Rule);

            return OptionCollectionExtensions
                .FirstOrNone(orderedRules)
                .Map(rule => rule.Operation.CreateOperation(
                    rule.RuleId,
                    rule.TransformKey));
        }
    }
}
