using UnityEngine;
namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/Logic/OR")]
    public class OrPredicateSo : PredicateSo
    {
        [SerializeReference]
        public PredicateSo[] conditions;

        public override bool Evaluate()
        {
            if (!isActive || conditions == null) return false;

            foreach (var condition in conditions)
            {
                if (condition != null && condition.Evaluate())
                    return true;
            }

            return false;
        }

        public override void Reset()
        {
            foreach (var condition in conditions)
                condition?.Reset();
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            foreach (var condition in conditions)
                condition?.SetActive(active);
        }
    }
}