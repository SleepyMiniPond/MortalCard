using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameHistory
{
    private List<TurnRecord> _turnRecords = new();
    private IGameEventWatcher _gameEventWatcher;

    public GameHistory(IGameEventWatcher gameEventWatcher)
    {
        _gameEventWatcher = gameEventWatcher;
    }
}

public class TurnRecord
{
    private List<ActionRecord> _actionRecords = new();
}

public class ActionRecord
{

}