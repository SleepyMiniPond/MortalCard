using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.GameData
{
    /// <summary>
    /// Runtime 與 Editor 共用的遊戲內容目錄建置產物。
    /// Editor 負責產生資產引用；Runtime 僅讀取已通過驗證的內容。
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameContentCatalog",
        menuName = "Scriptable Objects/Game Content Catalog")]
    public sealed class GameContentCatalog : SerializedScriptableObject
    {
        [SerializeField]
        [BoxGroup("Cards")]
        private CardDataScriptableBase[] _cardAssets = Array.Empty<CardDataScriptableBase>();

        [SerializeField]
        [BoxGroup("Buffs")]
        private CardBuffScriptable[] _cardBuffAssets = Array.Empty<CardBuffScriptable>();

        [SerializeField]
        [BoxGroup("Buffs")]
        private PlayerBuffDataScriptable[] _playerBuffAssets = Array.Empty<PlayerBuffDataScriptable>();

        [SerializeField]
        [BoxGroup("Buffs")]
        private CharacterBuffDataScriptable[] _characterBuffAssets = Array.Empty<CharacterBuffDataScriptable>();

        public IReadOnlyList<CardDataScriptableBase> CardAssets => _cardAssets;
        public IReadOnlyList<CardBuffScriptable> CardBuffAssets => _cardBuffAssets;
        public IReadOnlyList<PlayerBuffDataScriptable> PlayerBuffAssets => _playerBuffAssets;
        public IReadOnlyList<CharacterBuffDataScriptable> CharacterBuffAssets => _characterBuffAssets;
    }
}
