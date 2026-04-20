using System.Collections.Generic;
using UnityEngine;

public enum LocalizeTitleInfoType
{
    Player,
    Card,
    CardBuff,
    PlayerBuff,
    KeyWord
}
public enum LocalizeInfoType
{
    UI
}

public enum GameKeyWord
{
    None = 0,
    // CardType
    Attack,
    Defense,
    Speech,
    Sneak,
    Special,
    Item,
    // CardTheme
    TangSect,
    Emei,
    Songshan,
    BeggarClan,
    DianCang,
    BattleBegin,
    // GameTiming
    TurnStart,
    OnDrawCard,
    OnPlayCard,
    TurnEnd,
    // CardProperty
    EffectTimes,
    RecycleTimes,
    PowerAdjust,
    CostAdjust,
    InitialPriority,
    Preserved,
    Sealed,
    Consumable,
    Dispose,
    AutoDispose,
    AppendEffect,
    // Disposition
    Coward,
    Cautious,
    Moderate,
    Brave,
    Reckless,
}

public record LocalizeTitleInfoData(string Title, string Info);
public record LocalizeInfoData(string Info);

public class LocalizeLibrary
{
    private IReadOnlyDictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>> _localizeTitleInfoDatas;
    private IReadOnlyDictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>> _localizeInfoDatas;

    public LocalizeLibrary(
        IReadOnlyDictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>> localizeTitleInfoDatas,
        IReadOnlyDictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>> localizeInfoDatas)
    {
        _localizeTitleInfoDatas = localizeTitleInfoDatas;
        _localizeInfoDatas = localizeInfoDatas;
    }

    public LocalizeTitleInfoData Get(LocalizeTitleInfoType localizeType, string key)
    {
        if (_localizeTitleInfoDatas.TryGetValue(localizeType, out var localizeData))
        {
            if (localizeData.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return new LocalizeTitleInfoData(
            $"{localizeType}|{key}_Title",
            $"{localizeType}|{key}_Info");
    }

    public LocalizeInfoData Get(LocalizeInfoType localizeType, string key)
    {
        if (_localizeInfoDatas.TryGetValue(localizeType, out var localizeData))
        {
            if (localizeData.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return new LocalizeInfoData(
            $"{localizeType}|{key}_Info");
    }
}
