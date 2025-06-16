using UnityEngine;
namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/Logic/NOT")]
    public class NotPredicateSo : PredicateSo
    {
        [SerializeReference]
        public PredicateSo condition;

        public override bool Evaluate()
        {
            return isActive && condition != null && !condition.Evaluate();
        }

        public override void Reset()
        {
            condition?.Reset();
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            condition?.SetActive(active);
        }
    }
}