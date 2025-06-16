using System.Linq;
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

            return conditions.Any(condition => condition && condition.Evaluate());

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