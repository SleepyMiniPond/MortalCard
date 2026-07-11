using System;
using MortalGame.GameData;
using Optional;
using UnityEngine;
using MortalGame.GameModel;

namespace MortalGame.Presentation.Abstractions
{

    public interface IGameCommand { }
    public record UseCardCommand(
        Guid CardIndentity,
        Option<ISelectionTarget> SelectionTarget = default) : IGameCommand;

    public record TurnSubmitCommand(
        Faction Faction) : IGameCommand;

}
