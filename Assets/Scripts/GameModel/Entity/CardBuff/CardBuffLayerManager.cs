using System;
using System.Collections.Generic;
using Optional;

namespace MortalGame.GameModel
{
    /// <summary>
    /// 對外提供穩定的 CardBuff 管理介面，並將操作路由至目前生效的 Buff Layer。
    /// Base Layer 在 Override Layer 生效期間會凍結，直到 Override Layer 被移除。
    /// </summary>
    public sealed class CardBuffLayerManager : ICardBuffManager
    {
        private sealed record LayerEntry(
            CardBuffLayerHandle Handle,
            CardBuffLayer Layer);

        private readonly LayerEntry _baseLayer;
        private Option<LayerEntry> _overrideLayer;

        private LayerEntry ActiveLayerEntry => _overrideLayer.ValueOr(_baseLayer);
        private CardBuffLayer ActiveLayer => ActiveLayerEntry.Layer;

        public CardBuffLayerHandle ActiveLayerHandle => ActiveLayerEntry.Handle;
        public IReadOnlyCollection<ICardBuffEntity> Buffs => ActiveLayer.Buffs;

        public CardBuffLayerManager(IEnumerable<ICardBuffEntity> baseBuffs)
        {
            _baseLayer = new LayerEntry(
                new CardBuffLayerHandle(),
                new CardBuffLayer(baseBuffs));
            _overrideLayer = Option.None<LayerEntry>();
        }

        public CardBuffLayerHandle ReplaceOverrideLayer()
        {
            var overrideLayer = new LayerEntry(
                new CardBuffLayerHandle(),
                new CardBuffLayer(Array.Empty<ICardBuffEntity>()));
            _overrideLayer = overrideLayer.Some();
            return overrideLayer.Handle;
        }

        public bool TryRemoveOverrideLayer(CardBuffLayerHandle layerHandle)
        {
            if (!_overrideLayer.TryGetValue(out var overrideLayer) ||
                !ReferenceEquals(overrideLayer.Handle, layerHandle))
            {
                return false;
            }

            _overrideLayer = Option.None<LayerEntry>();
            return true;
        }

        public Option<ModifyCardBuffLevelResult> TryModifyBuffLevel(
            CardBuffLayerHandle layerHandle,
            string buffId,
            int level)
        {
            if (!_IsActiveLayer(layerHandle))
            {
                return Option.None<ModifyCardBuffLevelResult>();
            }

            return ActiveLayer.ModifyBuffLevel(buffId, level).Some();
        }

        public Option<AddCardBuffResult> TryAddBuff(
            CardBuffLayerHandle layerHandle,
            ICardBuffEntity newBuff)
        {
            if (!_IsActiveLayer(layerHandle))
            {
                return Option.None<AddCardBuffResult>();
            }

            return ActiveLayer.AddBuff(newBuff).Some();
        }

        public Option<RemoveCardBuffResult> TryRemoveBuff(
            CardBuffLayerHandle layerHandle,
            ICardBuffEntity existBuff)
        {
            if (!_IsActiveLayer(layerHandle))
            {
                return Option.None<RemoveCardBuffResult>();
            }

            return ActiveLayer.RemoveBuff(existBuff).Some();
        }

        public ModifyCardBuffLevelResult ModifyBuffLevel(string buffId, int level)
        {
            return ActiveLayer.ModifyBuffLevel(buffId, level);
        }

        public AddCardBuffResult AddBuff(ICardBuffEntity newBuff)
        {
            return ActiveLayer.AddBuff(newBuff);
        }

        public RemoveCardBuffResult RemoveBuff(ICardBuffEntity existBuff)
        {
            return ActiveLayer.RemoveBuff(existBuff);
        }

        public bool Update(TriggerContext triggerContext, ICardEntity card)
        {
            return ActiveLayer.Update(triggerContext, card);
        }

        private bool _IsActiveLayer(CardBuffLayerHandle layerHandle)
        {
            return ReferenceEquals(ActiveLayerHandle, layerHandle);
        }
    }
}
