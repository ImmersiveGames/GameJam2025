using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems.Tests
{
    public class MockActor : MonoBehaviour, IActor
    {
        // Implementação mínima de IActor, ajuste conforme sua interface real
        public Transform Transform { get; }
        public bool IsActive { get; set; }
        public string Name { get; }
    }
}