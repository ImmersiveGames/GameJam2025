using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Spawn/PredicateData")]
    public class PredicateData : ScriptableObject, IPredicate
    {
        [SerializeField] private bool alwaysTrue; // Exemplo simples, substitua pela lógica real

        public bool Evaluate()
        {
            return alwaysTrue; // Implemente a lógica real do predicado
        }
    }
}