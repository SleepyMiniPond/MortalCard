using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _nameText;

    [SerializeField]
    private HealthBarView _healthBarView;

    [SerializeField]
    private EnergyBarView _energyBarView;

    [SerializeField]
    private PlayerBuffCollectionView _buffCollectionView;
    
    
    private IGameplayModel _statusWatcher;
    private IGameViewModel _gameViewModel;
    private LocalizeLibrary _localizeLibrary;

    public void Init(
        IGameplayModel statusWatcher,
        IGameViewModel gameInfoModel,
        SimpleTitleInfoHintView simpleHintView,
        LocalizeLibrary localizeLibrary)
    {
        _statusWatcher = statusWatcher;
        _gameViewModel = gameInfoModel;
        _localizeLibrary = localizeLibrary;
        _buffCollectionView.Init(_gameViewModel, _localizeLibrary, simpleHintView);
    }

    public void SetPlayerInfo(EnemyEntity enemy)
    {
        _nameText.text = _localizeLibrary.Get(LocalizeTitleInfoType.Player, enemy.MainCharacter.NameKey).Title;
        _healthBarView.SetHealth(enemy.MainCharacter.CurrentHealth, enemy.MainCharacter.MaxHealth);
        _healthBarView.SetShield(enemy.MainCharacter.CurrentArmor);
        _energyBarView.SetEnergy(enemy.CurrentEnergy, enemy.MaxEnergy);
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
