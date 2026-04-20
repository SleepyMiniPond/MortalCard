using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyScriptable", menuName = "Scriptable Objects/EnemyScriptable")]
public class EnemyScriptable : SerializedScriptableObject
{
    public EnemyData Enemy = new();
}
