using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-140)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PlayerAutoRegistrar : MonoBehaviour
    {
        private IActor _actor;
        private IPlayerDomain _playerDomain;

        private bool _registered;
        private bool _waitingForActorId;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<PlayerAutoRegistrar>(
                    $"Nenhum IActor encontrado em '{name}'. PlayerAutoRegistrar ser� ignorado.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (_actor == null)
                return;

            _registered = false;
            _waitingForActorId = false;

            TryRegisterOrStartWaiting();
        }

        private void Update()
        {
            if (_actor == null || _registered == true || _waitingForActorId == false)
                return;

            // Continua tentando at� o ActorId existir.
            if (string.IsNullOrWhiteSpace(_actor.ActorId))
                return;

            // ActorId ficou pronto � tenta registrar.
            TryRegisterNow();
        }

        private void TryRegisterOrStartWaiting()
        {
            if (_registered || _actor == null)
                return;

            if (string.IsNullOrWhiteSpace(_actor.ActorId))
            {
                // ActorId ainda n�o foi gerado. Vamos aguardar via Update.
                _waitingForActorId = true;
                return;
            }

            TryRegisterNow();
        }

        private void TryRegisterNow()
        {
            if (_registered || _actor == null)
                return;

            var sceneName = gameObject.scene.name;

            if (!DependencyManager.Provider.TryGetForScene<IPlayerDomain>(sceneName, out _playerDomain) || _playerDomain == null)
            {
                DebugUtility.LogWarning<PlayerAutoRegistrar>(
                    $"IPlayerDomain n�o encontrado para a cena '{sceneName}'. " +
                    $"Garanta GameplayDomainBootstrapper nessa cena e maxSceneServices adequado.",
                    this);
                _playerDomain = null;
                return;
            }

            // Agora o ActorId j� existe (garantido antes de chamar).
            _registered = _playerDomain.RegisterPlayer(_actor);
            _waitingForActorId = !_registered;
        }

        private void OnDisable()
        {
            if (!_registered || _playerDomain == null || _actor == null)
                return;

            _playerDomain.UnregisterPlayer(_actor);
            _registered = false;
            _waitingForActorId = false;
        }
    }
}

