using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public interface IAllCardDetailPanel
{
    public record Property(
        IEnumerable<ICardView.CardClickableProperty> Items);    

    void Init(IGameViewModel gameInfoModel, LocalizeLibrary localizeLibrary);
    void Open();
    void Render(Property property);
    void Close();

    Button DeckButton { get; }
    Button HandCardButton { get; }
    Button GraveyardButton { get; }
    Button[] CloseButtons { get; }
}

public class AllCardDetailPanel : MonoBehaviour, IAllCardDetailPanel
{
    [SerializeField]
    private Button[] _closeButtons;
    [SerializeField]
    private Button _deckButton;
    [SerializeField]
    private Button _handCardButton;
    [SerializeField]
    private Button _graveyardButton;
    [SerializeField]
    private GameObject _panel;
    [SerializeField]
    private Transform _cardViewParent;
    [SerializeField]
    private CardViewFactory _cardViewFactory;

    public Button DeckButton => _deckButton;
    public Button HandCardButton => _handCardButton;
    public Button GraveyardButton => _graveyardButton;
    public Button[] CloseButtons => _closeButtons;

    private IGameViewModel _gameViewModel;
    private LocalizeLibrary _localizeLibrary;
    private List<CardView> _cardViews = new();

    public void Init(
        IGameViewModel gameInfoModel,
        LocalizeLibrary localizeLibrary)
    {
        _gameViewModel = gameInfoModel;
        _localizeLibrary = localizeLibrary;
    }

    public void Render(IAllCardDetailPanel.Property property)
    {
        _Clear();

        foreach (var cardItemProperty in property.Items)
        {
            var cardView = _cardViewFactory.CreatePrefab();
            cardView.transform.SetParent(_cardViewParent, false);
            cardView.Initialize(_gameViewModel, _localizeLibrary);
            cardView.Render(cardItemProperty);
            _cardViews.Add(cardView);
        }
    }

    public void Open()
    {
        _panel.SetActive(true);
    }
    public void Close()
    {
        _Clear();
        _panel.SetActive(false);
    }

    private void _Clear()
    {        
        foreach (var cardView in _cardViews)
        {
            _cardViewFactory.RecyclePrefab(cardView);
        }
        _cardViews.Clear();
    }
}
