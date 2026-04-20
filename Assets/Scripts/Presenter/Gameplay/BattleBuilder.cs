using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.TextCore.Text;

public class BattleBuidler
{
    private Context _context;

    public BattleBuidler(Context context)
    {
        _context = context;
    }

    public GameContextManager ConstructGameContextManager()
    {
        var cardLibrary = new CardLibrary(_context.CardTable);
        var cardBuffLibrary = new CardBuffLibrary(_context.CardBuffTable);
        var playerBuffLibrary = new PlayerBuffLibrary(_context.PlayerBuffTable);
        var characterBuffLibrary = new CharacterBuffLibrary(_context.CharacterBuffTable);
        var dispositionLibrary = new DispositionLibrary(_context.DispositionSettings);
        var localizeLibrary = new LocalizeLibrary(_context.LocalizeTitleInfoSetting, _context.LocalizeInfoSetting);

        return new GameContextManager(
            cardLibrary, 
            cardBuffLibrary,
            playerBuffLibrary,
            characterBuffLibrary,
            dispositionLibrary,
            localizeLibrary);
    }

    public GameStageSetting ConstructBattle()
    { 
        return new GameStageSetting(
            StageID: "StageTest",
            RandomSeed: Environment.TickCount,
            Ally: _context.Ally,
            Enemy: _context.AllEnemies[0]);
    }
}

