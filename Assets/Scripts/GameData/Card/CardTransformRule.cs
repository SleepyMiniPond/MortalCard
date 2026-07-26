using System;
using System.Collections.Generic;
using MortalGame.GameModel;
using Sirenix.OdinInspector;

namespace MortalGame.GameData
{
    public interface ICardTransformOperationData
    {
        CardFormOperation CreateOperation(string ruleId, string transformKey);
    }

    [Serializable]
    public sealed class ApplyCardTransformOperationData : ICardTransformOperationData
    {
        public string TargetCardDataId;
        public CardFormPersistence Persistence;

        public CardFormOperation CreateOperation(string ruleId, string transformKey)
        {
            return new ApplyCardFormOperation(
                ruleId,
                transformKey,
                TargetCardDataId,
                Persistence);
        }
    }

    [Serializable]
    public sealed class RevertCardTransformOperationData : ICardTransformOperationData
    {
        public CardFormOperation CreateOperation(string ruleId, string transformKey)
        {
            return new RevertCardFormOperation(ruleId, transformKey);
        }
    }

    [Serializable]
    public sealed class CardTransformRule
    {
        [BoxGroup("Identification")]
        public string RuleId;

        [BoxGroup("Identification")]
        public string TransformKey;

        [BoxGroup("Evaluation")]
        public int Priority;

        [BoxGroup("Evaluation")]
        public GameTiming Timing;

        [ShowInInspector]
        [BoxGroup("Evaluation")]
        public List<ICondition> Conditions = new();

        [ShowInInspector]
        [BoxGroup("Operation")]
        public ICardTransformOperationData Operation;
    }
}
