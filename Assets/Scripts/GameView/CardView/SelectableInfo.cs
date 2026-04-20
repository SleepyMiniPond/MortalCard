using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public record MainSelectionInfo(
    SelectType SelectType,
    TargetLogicTag LogicTag);

public record SubSelectionInfo(
    IReadOnlyDictionary<string, ISubSelectionGroupInfo> SelectionInfos);

public interface ISubSelectionGroupInfo { }
public record ExistCardSelectionInfo(
    IReadOnlyList<CardInfo> CardInfos,
    int Count,
    bool IsMustSelect) : ISubSelectionGroupInfo;
public record NewCardSelectionInfo() : ISubSelectionGroupInfo;
public record NewPartialCardSelectionInfo() : ISubSelectionGroupInfo;
public record NewEffectSelectionInfo() : ISubSelectionGroupInfo;

public static class SelectionInfoUtility
{
    public static MainSelectionInfo ToInfo(this MainTargetSelectLogic mainTargetLogic)
    {
        return new MainSelectionInfo(
            mainTargetLogic.MainSelectable.SelectType, mainTargetLogic.LogicTag);
    }

    public static SubSelectionInfo ToInfo(this IEnumerable<ISubSelectionGroup> subSelectionGroups, IGameplayModel model, ICardEntity cardEntity)
    {
        var selectionInfos = new Dictionary<string, ISubSelectionGroupInfo>();
        foreach (var group in subSelectionGroups)
        {
            switch (group)
            {
                case ExistCardSelectionGroup existCardGroup:                
                    var cardLookTriggerContext = new TriggerContext(
                        model,
                        new CardTrigger(cardEntity),
                        new CardLookIntentAction(cardEntity));

                    selectionInfos[group.Id] =
                        new ExistCardSelectionInfo(
                            existCardGroup.CardCandidates.Eval(cardLookTriggerContext).Select(c => c.ToInfo(model)).ToList(),
                            existCardGroup.SelectCount.Eval(cardLookTriggerContext),
                            existCardGroup.IsMustSelect.Eval(cardLookTriggerContext));
                    break;
                case NewCardSelectionGroup:
                    selectionInfos[group.Id] = new NewCardSelectionInfo();
                    break;
                case NewPartialCardSelectionGroup:
                    selectionInfos[group.Id] = new NewPartialCardSelectionInfo();
                    break;
                case NewEffectSelectionGroup:
                    selectionInfos[group.Id] = new NewEffectSelectionInfo();
                    break;
            }
        }

        return new SubSelectionInfo(selectionInfos);
    }

    public static bool IsSelectable(this SelectType selectType, TargetType targetType)
    {
        switch (selectType)
        {
            case SelectType.Character:
                return targetType == TargetType.AllyCharacter ||
                       targetType == TargetType.EnemyCharacter;
            case SelectType.AllyCharacter:
                return targetType == TargetType.AllyCharacter;
            case SelectType.EnemyCharacter:
                return targetType == TargetType.EnemyCharacter;
            case SelectType.Card:
                return targetType == TargetType.AllyCard ||
                       targetType == TargetType.EnemyCard;
            case SelectType.AllyCard:
                return targetType == TargetType.AllyCard;
            case SelectType.EnemyCard:
                return targetType == TargetType.EnemyCard;
            case SelectType.None:
            default:
                return false;
        }
    }
}