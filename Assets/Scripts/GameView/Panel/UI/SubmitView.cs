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

        public void SetInteractable(bool interactable)
        {
            // 場景關閉時，Presenter 的清理可能晚於按鈕銷毀。
            if (_submitButton != null)
                _submitButton.interactable = interactable;
        }

        public void Init(IGameplayActionReciever reciever)
        {
            _submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                    reciever.RecieveEvent(new TurnSubmitCommand(Faction.Ally)))
                .AddTo(this);
        }
    }
}
