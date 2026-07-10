using System;
using Optional;
using UnityEngine;
using MortalGame.GameModel;

public interface IGameCommand { }
public record UseCardCommand(
    Guid CardIndentity,
    Option<ISelectionTarget> SelectionTarget = default) : IGameCommand;

public record TurnSubmitCommand(
    Faction Faction) : IGameCommand;
