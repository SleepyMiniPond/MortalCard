using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Optional;
using Unity.VisualScripting;
using MortalGame.GameData;
namespace MortalGame.GameModel
{

    public interface ICardEntity
    {
        Guid Identity { get; }
        Option<Guid> OriginCardInstanceGuid { get; }
        string BaseCardDataId { get; }
        string CardDataId { get; }
        Option<CardFormState> SelfFormState { get; }

        CardType Type { get; }
        CardRarity Rarity { get; }
        IEnumerable<CardTheme> Themes { get; }

        MainTargetSelectLogic MainSelect { get; }
        IEnumerable<ISubSelectionGroup> SubSelects { get; }

        IEnumerable<ICardEffect> Effects { get; }
        IReadOnlyDictionary<CardTriggeredTiming, IEnumerable<ICardEffect>> TriggeredEffects { get; }
        IEnumerable<ICardPropertyEntity> Properties { get; }
        ICardBuffManager BuffManager { get; }

        int OriginCost { get; }
        int OriginPower { get; }

        CardFormOperationResult TryApplySelfForm(
            string transformKey,
            string targetCardDataId,
            CardFormPersistence persistence);
        CardFormOperationResult TryRevertSelfForm(string transformKey);
        ICardEntity Clone();
    }

    public class CardEntity : ICardEntity
    {
        // Card static data
        private readonly Guid _indentity;
        private readonly Option<Guid> _originCardInstanceGuid;
        private readonly string _baseCardDataId;

        // Card runtime data
        private Option<CardFormState> _selfFormState;
        private IReadOnlyList<ICardPropertyEntity> _cardDataProperties;
        private readonly IReadOnlyList<ICardPropertyEntity> _instanceProperties;

        // Card components
        private readonly ICardBuffManager _buffManager;
        private readonly CardLibrary _cardLibrary;
        private readonly ICardPropertyEntityFactory _cardPropertyEntityFactory;

        private string _effectiveCardDataId => _selfFormState.Map(state => state.CardDataId).ValueOr(_baseCardDataId);
        private CardData _effectiveCardData => _cardLibrary.GetCardData(_effectiveCardDataId);
        public string BaseCardDataId => _baseCardDataId;
        public string CardDataId => _effectiveCardDataId;
        public Option<CardFormState> SelfFormState => _selfFormState;
        public CardType Type => _effectiveCardData.Type;
        public CardRarity Rarity => _effectiveCardData.Rarity;
        public int OriginCost => _effectiveCardData.Cost;
        public int OriginPower => _effectiveCardData.Power;
        public IEnumerable<CardTheme> Themes => _effectiveCardData.Themes;
        public MainTargetSelectLogic MainSelect => _effectiveCardData.MainSelect;
        public IEnumerable<ISubSelectionGroup> SubSelects => _effectiveCardData.SubSelects;
        public IEnumerable<ICardEffect> Effects => _effectiveCardData.Effects;
        public IReadOnlyDictionary<CardTriggeredTiming, IEnumerable<ICardEffect>> TriggeredEffects
            => _effectiveCardData.TriggeredEffects.ToDictionary(
                pair => pair.Timing,
                pair => (IEnumerable<ICardEffect>)pair.Effects);

        public Guid Identity => _indentity;
        public Option<Guid> OriginCardInstanceGuid => _originCardInstanceGuid;
        public IEnumerable<ICardPropertyEntity> Properties => _cardDataProperties.Concat(_instanceProperties);
        public ICardBuffManager BuffManager => _buffManager;
        public bool IsDummy => this == DummyCard;

        public static ICardEntity DummyCard = new CardEntity(
            indentity: Guid.Empty,
            originCardInstanceGuid: Option.None<Guid>(),
            baseCardDataId: string.Empty,
            cardDataProperties: new List<ICardPropertyEntity>(),
            instanceProperties: new List<ICardPropertyEntity>(),
            buffs: new List<ICardBuffEntity>(),
            cardLibrary: null,
            cardPropertyEntityFactory: null
        );

        private CardEntity(
            Guid indentity,
            Option<Guid> originCardInstanceGuid,
            string baseCardDataId,
            IEnumerable<ICardPropertyEntity> cardDataProperties,
            IEnumerable<ICardPropertyEntity> instanceProperties,
            IEnumerable<ICardBuffEntity> buffs,
            CardLibrary cardLibrary,
            ICardPropertyEntityFactory cardPropertyEntityFactory
        )
        {
            _indentity = indentity;
            _originCardInstanceGuid = originCardInstanceGuid;
            _baseCardDataId = baseCardDataId;
            _selfFormState = Option.None<CardFormState>();
            _cardDataProperties = cardDataProperties.ToList();
            _instanceProperties = instanceProperties.ToList();
            _buffManager = new CardBuffManager(buffs);
            _cardLibrary = cardLibrary;
            _cardPropertyEntityFactory = cardPropertyEntityFactory;
        }

        public static ICardEntity CreateFromInstance(
            CardInstance cardInstance,
            CardLibrary cardLibrary,
            ICardPropertyEntityFactory cardPropertyEntityFactory)
        {
            return new CardEntity(
                indentity: Guid.NewGuid(),
                originCardInstanceGuid: cardInstance.InstanceGuid.Some(),
                baseCardDataId: cardInstance.CardDataId,
                cardDataProperties: cardLibrary.GetCardData(cardInstance.CardDataId).PropertyDatas
                    .Select(cardPropertyEntityFactory.Create),
                instanceProperties: cardInstance.AdditionPropertyDatas.Select(cardPropertyEntityFactory.Create),
                buffs: Array.Empty<ICardBuffEntity>(),
                cardLibrary: cardLibrary,
                cardPropertyEntityFactory: cardPropertyEntityFactory
            );
        }

        public static ICardEntity RuntimeCreateFromId(
            string cardDataId,
            CardLibrary cardLibrary,
            ICardPropertyEntityFactory cardPropertyEntityFactory)
        {
            return new CardEntity(
                indentity: Guid.NewGuid(),
                originCardInstanceGuid: Option.None<Guid>(),
                baseCardDataId: cardDataId,
                cardDataProperties: cardLibrary.GetCardData(cardDataId).PropertyDatas.Select(cardPropertyEntityFactory.Create),
                instanceProperties: Array.Empty<ICardPropertyEntity>(),
                buffs: Array.Empty<ICardBuffEntity>(),
                cardLibrary: cardLibrary,
                cardPropertyEntityFactory: cardPropertyEntityFactory
            );
        }

        public CardFormOperationResult TryApplySelfForm(
            string transformKey,
            string targetCardDataId,
            CardFormPersistence persistence)
        {
            var beforeCardDataId = CardDataId;
            if (string.IsNullOrWhiteSpace(transformKey) || string.IsNullOrWhiteSpace(targetCardDataId))
            {
                return new CardFormOperationResult(
                    CardFormOperationStatus.Rejected,
                    beforeCardDataId,
                    beforeCardDataId,
                    transformKey,
                    "TransformKey 與目標 CardDataId 不可為空白。");
            }

            if (targetCardDataId == beforeCardDataId)
            {
                return new CardFormOperationResult(
                    CardFormOperationStatus.NoOp,
                    beforeCardDataId,
                    beforeCardDataId,
                    transformKey);
            }

            var targetCardData = _cardLibrary.GetCardData(targetCardDataId);
            if (targetCardData == null)
            {
                return new CardFormOperationResult(
                    CardFormOperationStatus.Rejected,
                    beforeCardDataId,
                    beforeCardDataId,
                    transformKey,
                    $"找不到目標 CardData：{targetCardDataId}。");
            }

            _selfFormState = new CardFormState(transformKey, targetCardDataId, persistence).Some();
            _RebuildCardDataProperties(targetCardData);

            return new CardFormOperationResult(
                CardFormOperationStatus.Applied,
                beforeCardDataId,
                CardDataId,
                transformKey);
        }

        public CardFormOperationResult TryRevertSelfForm(string transformKey)
        {
            var beforeCardDataId = CardDataId;
            if (!_selfFormState.TryGetValue(out var currentForm) || currentForm.TransformKey != transformKey)
            {
                return new CardFormOperationResult(
                    CardFormOperationStatus.NoOp,
                    beforeCardDataId,
                    beforeCardDataId,
                    transformKey);
            }

            _selfFormState = Option.None<CardFormState>();
            _RebuildCardDataProperties(_cardLibrary.GetCardData(_baseCardDataId));

            return new CardFormOperationResult(
                CardFormOperationStatus.Reverted,
                beforeCardDataId,
                CardDataId,
                transformKey);
        }

        public ICardEntity Clone()
        {
            return new CardEntity(
                indentity: Guid.NewGuid(),
                originCardInstanceGuid: Option.None<Guid>(),
                baseCardDataId: CardDataId,
                cardDataProperties: _effectiveCardData.PropertyDatas.Select(_cardPropertyEntityFactory.Create),
                instanceProperties: Array.Empty<ICardPropertyEntity>(),
                buffs: Array.Empty<ICardBuffEntity>(),
                cardLibrary: _cardLibrary,
                cardPropertyEntityFactory: _cardPropertyEntityFactory);
        }

        private void _RebuildCardDataProperties(CardData cardData)
        {
            _cardDataProperties = cardData.PropertyDatas
                .Select(_cardPropertyEntityFactory.Create)
                .ToList();
        }
    }

    public static class CardEntityExtensions
    {
        public static Option<ICardEntity> GetCard(this IGameplayModel model, Guid identity)
        {
            var allyCardOpt = model.GameStatus.Ally.CardManager.GetCardOrNone(card => card.Identity == identity);
            if (allyCardOpt.HasValue)
                return allyCardOpt;
            var enemyCardOpt = model.GameStatus.Enemy.CardManager.GetCardOrNone(card => card.Identity == identity);
            if (enemyCardOpt.HasValue)
                return enemyCardOpt;
            return Option.None<ICardEntity>();
        }

        public static Option<IPlayerEntity> Owner(this ICardEntity card, IGameplayModel model)
        {
            var gameStatus = model.GameStatus;
            var allyCardOpt = gameStatus.Ally.CardManager.GetCardOrNone(card => card.Identity == card.Identity);
            if (allyCardOpt.HasValue)
                return (gameStatus.Ally as IPlayerEntity).Some();
            var enemyCardOpt = gameStatus.Enemy.CardManager.GetCardOrNone(card => card.Identity == card.Identity);
            if (enemyCardOpt.HasValue)
                return (gameStatus.Enemy as IPlayerEntity).Some();
            return Option.None<IPlayerEntity>();
        }
        public static Faction Faction(this ICardEntity card, IGameplayModel model)
        {
            return card.Owner(model).ValueOr(DummyPlayer.Instance).Faction;
        }

        public static bool IsConsumable(this ICardEntity card)
        {
            return card.HasProperty(CardProperty.Consumable);
        }
        public static bool IsDisposable(this ICardEntity card)
        {
            return card.HasProperty(CardProperty.Dispose) || card.HasProperty(CardProperty.AutoDispose);
        }

        public static bool HasProperty(this ICardEntity card, CardProperty property)
        {
            return
                card.Properties.Any(p => p.Property == property) ||
                card.BuffManager.Buffs.Any(b => b.Properties.Any(p => p.Property == property));
        }

        public static int GetCardProperty(
            this ICardEntity card, TriggerContext triggerContext, CardProperty targetProperty)
        {
            var value = 0;

            var cardTrigger = new CardTrigger(card);
            var propertyContext = triggerContext with { Triggered = cardTrigger };
            foreach (var property in card.Properties.Where(p => p.Property == targetProperty))
            {
                value += property.Eval(propertyContext);
            }

            foreach (var buff in card.BuffManager.Buffs)
            {
                var cardBuffTrigger = new CardBuffTrigger(buff);
                var cardBuffContext = triggerContext with { Triggered = cardBuffTrigger };
                foreach (var property in buff.Properties.Where(p => p.Property == targetProperty))
                {
                    value += property.Eval(cardBuffContext);
                }
            }

            return value;
        }
    }

}
