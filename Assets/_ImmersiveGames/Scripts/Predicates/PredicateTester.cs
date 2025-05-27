using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.Predicates
{
    public class PredicateTester : MonoBehaviour
    {
        [Tooltip("Referência para um PredicateSO que você deseja testar")]
        [SerializeReference]
        public PredicateSo predicate;

        [Tooltip("Opcional: ativar/desativar a avaliação")]
        public bool isEnabled = true;

        private void Start()
        {
            var input = GetComponent<PlayerInput>();
            if (input == null)
            {
                Debug.LogWarning("[PredicateTester] Nenhum PlayerInput encontrado no GameObject.");
                return;
            }

            // Bind recursivo para o predicado e seus filhos
            BindAllPredicates(predicate, input.actions);
        }

        private void Update()
        {
            if (!isEnabled || predicate == null) return;

            bool result = predicate.Evaluate();

            if (result)
            {
                Debug.Log($"[PredicateTester] 🎯 Predicate '{predicate.name}' retornou TRUE em frame {Time.frameCount}", this);
            }
        }

        private void BindAllPredicates(PredicateSo pred, InputActionAsset inputAsset)
        {
            if (!pred || !inputAsset) return;

            switch (pred)
            {
                case IBindableInputPredicate bindable:
                    bindable.Bind(inputAsset);
                    Debug.Log($"[PredicateTester] 🔗 Bind realizado para: {pred.name}");
                    break;
                case AndPredicateSo and: {
                    foreach (var p in and.conditions)
                        BindAllPredicates(p, inputAsset);
                    break;
                }
                case OrPredicateSo or: {
                    foreach (var p in or.conditions)
                        BindAllPredicates(p, inputAsset);
                    break;
                }
                case NotPredicateSo not when not.condition != null:
                    BindAllPredicates(not.condition, inputAsset);
                    break;
            }

        }
    }
}
