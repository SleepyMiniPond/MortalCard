using System;
using MortalGame.GameData;
using System.Collections.Generic;
using Optional;

namespace MortalGame.GameModel
{

    public interface IGameAction { }

    public record TurnSubmitAction(Faction Faction) : IGameAction;

    public record UseCardAction(
        Guid CardIndentity,
        MainSelectionAction MainSelectionAction,
        IReadOnlyDictionary<string, ISubSelectionAction> SubSelectionActions) : IGameAction;

    public record MainSelectionAction(
        TargetType TargetType,
        Option<Guid> SelectedTarget)
    {
        public static MainSelectionAction Empty = new MainSelectionAction(TargetType.None, Option.None<Guid>());
        public static MainSelectionAction Create(ISelectionTarget selectionTarget)
            => new MainSelectionAction(selectionTarget.TargetType, selectionTarget.TargetIdentity.Some());
    }

    public interface ISubSelectionAction { }
    public record ExistCardSubSelectionAction(
        IReadOnlyList<Guid> CardIdentity) : ISubSelectionAction;
    public record NewCardSubSelectionAction() : ISubSelectionAction;
    public record NewPartialCardSubSelectionAction() : ISubSelectionAction;
    public record NewEffectSubSelectionAction() : ISubSelectionAction;

}
