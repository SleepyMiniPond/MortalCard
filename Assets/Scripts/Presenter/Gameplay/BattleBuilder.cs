using MortalGame.Presentation.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.TextCore.Text;
using MortalGame.GameData;
using MortalGame.GameModel;

namespace MortalGame.Presenter
{

public class BattleBuidler
{
    private Context _context;

    public BattleBuidler(Context context)
    {
        _context = context;
    }

    public GameContextManager ConstructGameContextManager(int randomSeed)
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
            localizeLibrary,
            new GameRandom(randomSeed),
            CardPropertyEntityFactory.CreateDefault(),
            CardBuffPropertyEntityFactory.CreateDefault(),
            CardBuffLifeTimeEntityFactory.CreateDefault(),
            ReactionSessionEntityFactory.CreateDefault(),
            PlayerBuffPropertyEntityFactory.CreateDefault(),
            PlayerBuffLifeTimeEntityFactory.CreateDefault(),
            CharacterBuffPropertyEntityFactory.CreateDefault(),
            CharacterBuffLifeTimeEntityFactory.CreateDefault());
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


}
