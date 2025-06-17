using _ImmersiveGames.Scripts.Predicates;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    // Implementa um trigger baseado em inputs do jogador
    public class PlayerInputTrigger : ISpawnTrigger
    {
        private SpawnPoint _spawnPoint;
        private bool _isActive = true;
        private readonly IPredicate _predicate;
        private readonly InputActionAsset _inputAsset;

        // Construtor recebe o predicado e os inputs
        public PlayerInputTrigger(IPredicate predicate, InputActionAsset inputAsset)
        {
            _predicate = predicate ?? throw new System.ArgumentNullException(nameof(predicate));
            _inputAsset = inputAsset ?? throw new System.ArgumentNullException(nameof(inputAsset));
            ConfigureBindings();
        }

        // Configura os bindings dos predicados
        private void ConfigureBindings()
        {
            ConfigurePredicateBindings(_predicate, _inputAsset);
        }

        // Configura bindings recursivamente para predicados compostos
        private void ConfigurePredicateBindings(IPredicate predicate, InputActionAsset inputAsset)
        {
            switch (predicate)
            {
                case IBindableInputPredicate bindable:
                    bindable.Bind(inputAsset);
                    break;
                case And and:
                    foreach (var inner in and.GetRules())
                        ConfigurePredicateBindings(inner, inputAsset);
                    break;
                case Or or:
                    foreach (var inner in or.GetRules())
                        ConfigurePredicateBindings(inner, inputAsset);
                    break;
                case Not not:
                    ConfigurePredicateBindings(not.GetRule(), inputAsset);
                    break;
            }
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || !_predicate.Evaluate()) return false;
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void Reset()
        {
            _isActive = true;
        }

        public void BindPlayerInput(InputActionAsset inputActions)
        {
            // Não necessário, pois a vinculação já é feita no construtor
        }
    }
}