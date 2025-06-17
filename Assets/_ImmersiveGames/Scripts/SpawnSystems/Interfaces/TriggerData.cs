using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Spawn/TriggerData")]
    public class TriggerData : ScriptableObject
    {
        public TriggerType triggerType;
        public TriggerProperties properties = new TriggerProperties();
    }
}