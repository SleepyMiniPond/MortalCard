using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;

namespace MortalGame.Editor.Exceptions
{
    public sealed class EmptyCatalogAssetPathException : ArgumentException
    {
        public EmptyCatalogAssetPathException(string paramName)
            : base("Catalog 資產路徑不可為空。", paramName)
        {
        }
    }

    public sealed class EmptyContentSearchFoldersException : ArgumentException
    {
        public EmptyContentSearchFoldersException(string paramName)
            : base("內容搜尋目錄不可為空。", paramName)
        {
        }
    }

    public sealed class CatalogAssetTypeConflictException : InvalidOperationException
    {
        public string AssetPath { get; }
        public Type ActualType { get; }

        public CatalogAssetTypeConflictException(string assetPath, Type actualType)
            : base($"Catalog 輸出路徑已存在其他類型資產：{assetPath}，型別：{actualType.FullName}。")
        {
            AssetPath = assetPath;
            ActualType = actualType;
        }
    }

    public sealed class MissingCatalogSerializedFieldException : InvalidOperationException
    {
        public string PropertyName { get; }

        public MissingCatalogSerializedFieldException(string propertyName)
            : base($"GameContentCatalog 缺少序列化欄位：{propertyName}。")
        {
            PropertyName = propertyName;
        }
    }

    public sealed class MissingEffectMenuSettingsException : InvalidOperationException
    {
        public string AssetPath { get; }

        public MissingEffectMenuSettingsException(string assetPath)
            : base($"找不到效果選單設定：{assetPath}。")
        {
            AssetPath = assetPath;
        }
    }

    public sealed class GameDataValidationBuildFailedException : BuildFailedException
    {
        public IReadOnlyList<ValidationIssue> ValidationErrors { get; }

        public GameDataValidationBuildFailedException(IEnumerable<ValidationIssue> errors)
            : this(errors.ToArray())
        {
        }

        private GameDataValidationBuildFailedException(ValidationIssue[] errors)
            : base($"遊戲資料驗證失敗，共 {errors.Length} 個錯誤：" +
                Environment.NewLine + string.Join(Environment.NewLine, errors.Select(error => $"- {error?.Message}")))
        {
            ValidationErrors = Array.AsReadOnly(errors);
        }
    }
}
