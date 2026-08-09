using System;

namespace MortalGame.GameModel
{
    /// <summary>
    /// CardBuff Layer 的不透明身分。只有 Layer Manager 能建立 Handle；
    /// 外部流程只能保存並交還原本取得的實例。
    /// </summary>
    public sealed class CardBuffLayerHandle
    {
        private readonly Guid _identity = Guid.NewGuid();

        internal CardBuffLayerHandle()
        {
        }

        public override string ToString()
        {
            return _identity.ToString("N");
        }
    }
}
