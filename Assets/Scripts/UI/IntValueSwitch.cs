using Sirenix.OdinInspector;
using UnityEngine;

namespace MortalGame.UI
{

public abstract class IntValueSwitch : SerializedMonoBehaviour
{
    public abstract int Value { set; }
}
}
