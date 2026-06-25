using System.Collections.Generic;

public static class GameContextTestBuilder
{
    public static CardLibrary CreateCardLibrary(Dictionary<string, CardData> cards = null)
    {
        return new CardLibrary(cards ?? new Dictionary<string, CardData>());
    }

    public static CardBuffLibrary CreateCardBuffLibrary(Dictionary<string, CardBuffData> buffs = null)
    {
        return new CardBuffLibrary(buffs ?? new Dictionary<string, CardBuffData>());
    }

    public static PlayerBuffLibrary CreatePlayerBuffLibrary(Dictionary<string, PlayerBuffData> buffs = null)
    {
        return new PlayerBuffLibrary(buffs ?? new Dictionary<string, PlayerBuffData>());
    }

    public static CharacterBuffLibrary CreateCharacterBuffLibrary(Dictionary<string, CharacterBuffData> buffs = null)
    {
        return new CharacterBuffLibrary(buffs ?? new Dictionary<string, CharacterBuffData>());
    }

    public static DispositionLibrary CreateDispositionLibrary()
    {
        return new DispositionLibrary(new[]
        {
            new DispositionData("test-disposition", 1, 0, 0)
        });
    }

    public static LocalizeLibrary CreateLocalizeLibrary()
    {
        return new LocalizeLibrary(
            new Dictionary<LocalizeTitleInfoType, IReadOnlyDictionary<string, LocalizeTitleInfoData>>(),
            new Dictionary<LocalizeInfoType, IReadOnlyDictionary<string, LocalizeInfoData>>());
    }

    public static GameContextManager CreateContextManager(
        CardLibrary cardLibrary = null,
        CardBuffLibrary cardBuffLibrary = null,
        PlayerBuffLibrary playerBuffLibrary = null,
        CharacterBuffLibrary characterBuffLibrary = null,
        DispositionLibrary dispositionLibrary = null,
        LocalizeLibrary localizeLibrary = null)
    {
        return new GameContextManager(
            cardLibrary ?? CreateCardLibrary(),
            cardBuffLibrary ?? CreateCardBuffLibrary(),
            playerBuffLibrary ?? CreatePlayerBuffLibrary(),
            characterBuffLibrary ?? CreateCharacterBuffLibrary(),
            dispositionLibrary ?? CreateDispositionLibrary(),
            localizeLibrary ?? CreateLocalizeLibrary());
    }
}
