namespace MortalGame.Editor
{
    public abstract partial record ValidationIssue
    {
        public sealed record MissingCatalogForNestedContent : ValidationIssue
        {
            public override string Message => "GameContentCatalog 為空，無法驗證巢狀內容";
        }

        public sealed record MissingCatalogForLocalization : ValidationIssue
        {
            public override string Message => "GameContentCatalog 為空，無法驗證 Localization";
        }

        public sealed record MissingLocalizationData : ValidationIssue
        {
            public override string Message => "ExcelDatas 為空，無法驗證 Localization";
        }

        public sealed record MissingCatalogForCoverage : ValidationIssue
        {
            public override string Message => "GameContentCatalog 為空，無法驗證內容覆蓋";
        }

        public sealed record DuplicateCardDataId(string DuplicateId) : ValidationIssue
        {
            public override string Message => $"CardData ID 在 Standard／Override 資產間重複：{DuplicateId}";
        }

        public sealed record NullDeckCardReference(string Context) : ValidationIssue
        {
            public override string Message => $"{Context} 的 DeckScriptable.Cards 含有空卡牌引用";
        }

        public sealed record MissingCommandHandler(System.Type ActualType) : ValidationIssue
        {
            public override string Message => $"{ActualType.Name} 缺少 IEffectCommandHandler 註冊";
        }

        public sealed record MissingCatalogForResolvers : ValidationIssue
        {
            public override string Message => "GameContentCatalog 為空，無法驗證 Effect Resolver";
        }

        public sealed record MissingCatalogForReferenceIds : ValidationIssue
        {
            public override string Message => "GameContentCatalog 為空，無法驗證參照 ID";
        }

        public sealed record MissingPlayerData(string AssetPath) : ValidationIssue
        {
            public override string Message => $"{AssetPath} 的 PlayerData 為空";
        }

        public sealed record MissingPlayerDeck(string AssetPath, string ID) : ValidationIssue
        {
            public override string Message => $"{AssetPath} / PlayerData[{ID}] 缺少 Deck 引用";
        }

        public sealed record MissingContentId(string Context, string DataType) : ValidationIssue
        {
            public override string Message => $"{Context} 的 {DataType}.ID 為空";
        }

        public sealed record DuplicateContentId(string DataType, string DuplicateId) : ValidationIssue
        {
            public override string Message => $"{DataType}.ID 重複：{DuplicateId}";
        }

        public sealed record MissingLocalizationTable(string TableName) : ValidationIssue
        {
            public override string Message => $"ExcelDatas.{TableName} 為空";
        }

        public sealed record NullLocalizationRow(string TableName, int Index) : ValidationIssue
        {
            public override string Message => $"ExcelDatas.{TableName}[{Index}] 為空";
        }

        public sealed record EmptyLocalizationId(string TableName, int Index) : ValidationIssue
        {
            public override string Message => $"ExcelDatas.{TableName}[{Index}].Id 為空";
        }

        public sealed record EmptyLocalizationTitle(string TableName, int Index) : ValidationIssue
        {
            public override string Message => $"ExcelDatas.{TableName}[{Index}].Title 為空";
        }

        public sealed record EmptyLocalizationInfo(string TableName, int Index) : ValidationIssue
        {
            public override string Message => $"ExcelDatas.{TableName}[{Index}].Info 為空";
        }

        public sealed record DuplicateLocalizationId(string TableName, string DuplicateId) : ValidationIssue
        {
            public override string Message => $"ExcelDatas.{TableName} 的 Id 重複：{DuplicateId}";
        }

        public sealed record MissingLocalizationKey(string Context, string TableName) : ValidationIssue
        {
            public override string Message => $"{Context} 在 ExcelDatas.{TableName} 找不到 Localization Key";
        }

        public sealed record NullCatalogAsset(string CollectionName, int Index) : ValidationIssue
        {
            public override string Message => $"GameContentCatalog.{CollectionName}[{Index}] 是空資產引用";
        }

        public sealed record UnsavedCatalogAsset(string CollectionName, int Index, string Name) : ValidationIssue
        {
            public override string Message => $"GameContentCatalog.{CollectionName}[{Index}] 不是已儲存的專案資產：{Name}";
        }

        public sealed record DuplicateCatalogAsset(string CollectionName, string DuplicatePath) : ValidationIssue
        {
            public override string Message => $"GameContentCatalog.{CollectionName} 重複收錄資產：{DuplicatePath}";
        }

        public sealed record MissingCatalogAsset(string CollectionName, string MissingPath) : ValidationIssue
        {
            public override string Message => $"GameContentCatalog.{CollectionName} 未收錄掃描到的資產：{MissingPath}";
        }

        public sealed record OutOfScopeCatalogAsset(string CollectionName, string ExtraPath) : ValidationIssue
        {
            public override string Message => $"GameContentCatalog.{CollectionName} 收錄了掃描範圍外的資產：{ExtraPath}";
        }

        public sealed record UnsortedCatalogAssets(string CollectionName) : ValidationIssue
        {
            public override string Message => $"GameContentCatalog.{CollectionName} 的順序與資產路徑排序不一致，請重新產生內容目錄";
        }

        public sealed record MissingDefaultCatalog(string Catalog) : ValidationIssue
        {
            public override string Message => $"找不到遊戲內容目錄：{Catalog}，" +
                "請執行 MortalGame/遊戲內容/重新產生內容目錄";
        }
    }
}
