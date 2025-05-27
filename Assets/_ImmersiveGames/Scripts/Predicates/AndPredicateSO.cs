using UnityEngine;
namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/And")]
    public class AndPredicateSo : PredicateSo
    {
        [SerializeReference] public PredicateSo[] conditions;

        public override bool Evaluate()
        {
            if (!isActive || conditions == null) return false;

            foreach (var condition in conditions)
            {
                bool result = condition != null && condition.Evaluate();
                if (!result) return false;
            }

            return true;
        }

        public override void Reset()
        {
            foreach (var condition in conditions)
            {
                condition?.Reset();
            }
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);
            foreach (var condition in conditions)
            {
                condition?.SetActive(active);
            }
        }
    }
}