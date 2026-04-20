using UnityEngine;
using UnityEngine.UI;

public class AutoCanvasScaler : MonoBehaviour
{
    [SerializeField]
    private CanvasScaler _canvasScaler;

    private void Awake()
    {
        float screenRatio = (float)Screen.width / Screen.height;
        float referenceRatio = _canvasScaler.referenceResolution.x / _canvasScaler.referenceResolution.y;
        _canvasScaler.matchWidthOrHeight = screenRatio > referenceRatio ? 1 : 0;
    }
}
