using UniRx;
using MortalGame.Presentation.Abstractions;
using MortalGame.GameData;
using UnityEngine;
using UnityEngine.UI;

namespace MortalGame.GameView
{

public class SubmitView : MonoBehaviour
{
    [SerializeField]
    private Button _submitButton;

    public void Init(IGameplayActionReciever reciever)
    {
        _submitButton.OnClickAsObservable()
            .Subscribe(_ => 
                reciever.RecieveEvent(new TurnSubmitCommand(Faction.Ally)))
            .AddTo(this);
    }
}
}
