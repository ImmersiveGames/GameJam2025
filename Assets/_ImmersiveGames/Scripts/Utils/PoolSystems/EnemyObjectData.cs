using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "EnemyObjectData", menuName = "ImmersiveGames/EnemyObjectData")]
    public class EnemyObjectData : PoolableObjectData
    {
        [SerializeField] private string initialAIState = "Idle";

        public string InitialAIState => initialAIState;

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!string.IsNullOrEmpty(initialAIState)) return;
            initialAIState = "Idle";
        }
    #endif
    }
}