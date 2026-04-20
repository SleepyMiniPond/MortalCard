using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class AllyInfoView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _nameText;

    [SerializeField]
    private HealthBarView _healthBarView;

    [SerializeField]
    private EnergyBarView _energyBarView;

    [SerializeField]
    private DispositionView _dispositionView;

    [SerializeField]
    private PlayerBuffCollectionView _buffCollectionView;
    
    private IGameViewModel _gameViewModel;
    private TopBarInfoView _topBarInfoView;
    private LocalizeLibrary _localizeLibrary;

    public void Init(
        IGameViewModel gameViewModel,
        TopBarInfoView topBarInfoView, 
        SimpleTitleInfoHintView simpleHintView,
        LocalizeLibrary localizeLibrary, 
        DispositionLibrary dispositionLibrary)
    {
        _gameViewModel = gameViewModel;
        _topBarInfoView = topBarInfoView;
        _localizeLibrary = localizeLibrary;
        _dispositionView.Init(localizeLibrary, dispositionLibrary, gameViewModel, simpleHintView);
        _buffCollectionView.Init(gameViewModel, localizeLibrary, simpleHintView);
    }

    public void SetPlayerInfo(int round, AllyEntity ally)
    {
        _topBarInfoView.UpdateTurnInfo(round);
        _nameText.text = _localizeLibrary.Get(LocalizeTitleInfoType.Player, ally.MainCharacter.NameKey).Title;
        _healthBarView.SetHealth(ally.MainCharacter.CurrentHealth, ally.MainCharacter.MaxHealth);
        _healthBarView.SetShield(ally.MainCharacter.CurrentArmor);    
        _energyBarView.SetEnergy(ally.CurrentEnergy, ally.MaxEnergy);
    }
    
    public void UpdateEnergy(LoseEnergyEvent loseEnergyEvent)
    {
        _energyBarView.SetEnergy(loseEnergyEvent.Info.CurrentEnergy, loseEnergyEvent.Info.MaxEnergy);
    }
    public void UpdateEnergy(GainEnergyEvent gainEnergyEvent)
    {
        _energyBarView.SetEnergy(gainEnergyEvent.Info.CurrentEnergy, gainEnergyEvent.Info.MaxEnergy);
    }

    public void UpdateHealth(HealthEvent healthEvent)
    {
        _healthBarView.SetHealth(healthEvent.Hp, healthEvent.MaxHp);
        _healthBarView.SetShield(healthEvent.Dp);    
    }

    public void AddBuff(AddPlayerBuffEvent addBuffEvent)
    {
        _buffCollectionView.AddBuff(addBuffEvent.Buff);
    }
    public void RemoveBuff(RemovePlayerBuffEvent removeBuffEvent)
    {
        _buffCollectionView.RemoveBuff(removeBuffEvent.Buff);
    }
}
