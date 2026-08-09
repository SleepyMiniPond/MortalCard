using System.Collections.Generic;
using Optional;

namespace MortalGame.GameModel
{
    public interface ICardBuffManager
    {
        CardBuffLayerHandle ActiveLayerHandle { get; }
        IReadOnlyCollection<ICardBuffEntity> Buffs { get; }
        CardBuffLayerHandle ReplaceOverrideLayer();
        bool TryRemoveOverrideLayer(CardBuffLayerHandle layerHandle);
        Option<ModifyCardBuffLevelResult> TryModifyBuffLevel(
            CardBuffLayerHandle layerHandle,
            string buffId,
            int level);
        Option<AddCardBuffResult> TryAddBuff(
            CardBuffLayerHandle layerHandle,
            ICardBuffEntity newBuff);
        Option<RemoveCardBuffResult> TryRemoveBuff(
            CardBuffLayerHandle layerHandle,
            ICardBuffEntity existBuff);
        ModifyCardBuffLevelResult ModifyBuffLevel(string buffId, int level);
        AddCardBuffResult AddBuff(ICardBuffEntity newBuff);
        RemoveCardBuffResult RemoveBuff(ICardBuffEntity existBuff);
        bool Update(TriggerContext triggerContext, ICardEntity card);
    }
}
