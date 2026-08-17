using System;
using System.Collections.Generic;

namespace MortalGame.Editor
{
    /// <summary>
    /// 集中管理 Editor 工具使用的 Unity 專案相對路徑。
    /// Runtime 不應依賴此類別，正式內容應透過序列化資產引用取得。
    /// </summary>
    public static class ProjectAssetPaths
    {
        public const string ScriptableObjectsRoot = "Assets/ScriptableObjects";

        public static class GameContent
        {
            public const string Catalog =
                ScriptableObjectsRoot + "/GameContentCatalog.asset";
            public const string PlayerBuffFolder =
                ScriptableObjectsRoot + "/PlayerBuff";
            public const string CardBuffFolder =
                ScriptableObjectsRoot + "/CardBuff";

            public static IReadOnlyList<string> SearchFolders { get; } =
                Array.AsReadOnly(new[] { ScriptableObjectsRoot });
        }

        public static class Tests
        {
            public const string EditModeRoot = "Assets/Tests/EditMode";
            public const string GameContentCatalogRoot =
                EditModeRoot + "/GameContentCatalog";
        }
    }
}
