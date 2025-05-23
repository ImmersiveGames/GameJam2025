using _ImmersiveGames.Scripts.EnemySystem;
using UnityEngine;
using _ImmersiveGames.Scripts.ScriptableObjects;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ImmersiveGames/PlayerHealthData")]
    public class PlayerHealthData : DestructibleObjectSo
    {
        // Herda maxHealth e IsDestructible de DestructibleObjectSo
        // Podemos adicionar mais configurações específicas do player aqui
    }
}