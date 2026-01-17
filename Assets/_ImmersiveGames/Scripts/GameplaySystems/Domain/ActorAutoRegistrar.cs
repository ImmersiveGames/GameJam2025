using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-150)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ActorAutoRegistrar : MonoBehaviour
    {
        private IActor _actor;
        private IActorRegistry _registry;

        private bool _registered;
        private bool _pendingRegister;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<ActorAutoRegistrar>(
                    $"Nenhum IActor encontrado em '{name}'. ActorAutoRegistrar será ignorado.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (_actor == null)
                return;

            TryResolveRegistry();

            if (_registry == null)
                return;

            // Se o ActorId ainda não está pronto, adia para Start.
            if (string.IsNullOrWhiteSpace(_actor.ActorId))
            {
                _pendingRegister = true;
                return;
            }

            _registered = _registry.Register(_actor);
            _pendingRegister = false;
        }

        private void Start()
        {
            if (_actor == null || _registered == true || _pendingRegister == false)
                return;

            TryResolveRegistry();

            if (_registry == null)
                return;

            if (string.IsNullOrWhiteSpace(_actor.ActorId))
            {
                DebugUtility.LogWarning<ActorAutoRegistrar>(
                    $"ActorId ainda vazio em Start para '{_actor.ActorName}'. " +
                    $"Verifique se o ActorMaster está gerando ActorId no Awake e se o OldUniqueIdFactory está disponível.",
                    this);
                return;
            }

            _registered = _registry.Register(_actor);
            _pendingRegister = false;
        }

        private void OnDisable()
        {
            if (!_registered || _registry == null || _actor == null)
                return;

            _registry.Unregister(_actor);
            _registered = false;
        }

        private void TryResolveRegistry()
        {
            if (_registry != null)
                return;

            var sceneName = gameObject.scene.name;

            if (!DependencyManager.Provider.TryGetForScene<IActorRegistry>(sceneName, out _registry) || _registry == null)
            {
                DebugUtility.LogWarning<ActorAutoRegistrar>(
                    $"IActorRegistry não encontrado para a cena '{sceneName}'. " +
                    $"Garanta que existe um GameplayDomainBootstrapper nessa cena.",
                    this);
                _registry = null;
            }
        }
    }
}
