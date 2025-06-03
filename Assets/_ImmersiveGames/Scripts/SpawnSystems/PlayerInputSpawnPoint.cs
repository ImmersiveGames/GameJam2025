using _ImmersiveGames.Scripts.Predicates;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class PlayerInputSpawnPoint : SpawnPoint
    {
        [field: SerializeField] public PlayerInput PlayerInput { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (PlayerInput && spawnData?.TriggerStrategy is PredicateTriggerSo predicateTrigger)
            {
                BindAllPredicates(predicateTrigger.predicate, PlayerInput.actions);
            }
        }
        public void BindAllPredicates(PredicateSo predicate, InputActionAsset inputAsset)
        {
            while (true)
            {
                switch (predicate)
                {
                    case IBindableInputPredicate bindable:
                        bindable.Bind(inputAsset);
                        break;
                    case AndPredicateSo and: {
                        foreach (var inner in and.conditions) BindAllPredicates(inner, inputAsset);
                        break;
                    }
                    case OrPredicateSo or: {
                        foreach (var inner in or.conditions) BindAllPredicates(inner, inputAsset);
                        break;
                    }
                    case NotPredicateSo not when not.condition:
                        predicate = not.condition;
                        continue;
                }

                break;
            }
        }

    }
}