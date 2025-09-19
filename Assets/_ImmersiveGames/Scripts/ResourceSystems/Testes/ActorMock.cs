using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Testes
{
    public class ActorMock : MonoBehaviour, IActor
    {
        public Transform Transform => transform;
        public bool IsActive { get; set; }

        public string Name
        {
            get
            {
                string name = gameObject.name;
                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogWarning($"ActorMock: Nome do GameObject está vazio em {gameObject.name}. Definindo como 'UnnamedActor'.", this);
                    return "UnnamedActor";
                }
                return name;
            }
        }
    }
}