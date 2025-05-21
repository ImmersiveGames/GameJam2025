using UnityEngine;

namespace _ImmersiveGames.Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NovaPlanetaConfig", menuName = "ImmersiveGames/PlanetaConfig", order = 1)]
    public class DestructibleObjectSo : ScriptableObject
    {
        [SerializeField] public float maxHealth = 100f;
        [SerializeField] public float defense = 0f;
        [SerializeField] public bool destroyOnDeath = true;
        [SerializeField] public float destroyDelay = 2f;
    }
}
