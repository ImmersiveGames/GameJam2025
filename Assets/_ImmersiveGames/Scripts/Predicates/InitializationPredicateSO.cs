using UnityEngine;
namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/Initialization")]
    public class InitializationPredicateSo : PredicateSo
    {
        private bool _firstCallDone;

        public override bool Evaluate()
        {
            if (!isActive) return false;

            // Se ainda não foi chamado, disparamos e marcamos como usado
            if (!_firstCallDone)
            {
                _firstCallDone = true;
                return true;
            }

            return false;
        }

        public override void SetActive(bool active)
        {
            base.SetActive(active);

            // Sempre que o trigger for reativado, resetamos o predicado
            if (active)
            {
                Reset(); // <- aqui acontece a mágica automática
            }
        }

        public override void Reset()
        {
            _firstCallDone = false;
        }
    }
}