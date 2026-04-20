using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MenuScene : MonoBehaviour
{
    // Get the reference to the CanvasScaler component attached to the Canvas
    [SerializeField] private CanvasScaler _canvasScaler;

    public async UniTask Run()
    {
        await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0));
    }
}
