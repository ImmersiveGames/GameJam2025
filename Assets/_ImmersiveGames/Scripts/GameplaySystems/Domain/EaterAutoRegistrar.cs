using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-140)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterAutoRegistrar : MonoBehaviour
    {
        private IActor _actor;
        private IEaterDomain _eaterDomain;

        private bool _registered;
        private bool _shouldRetryInStart;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<EaterAutoRegistrar>(
                    $"Nenhum IActor encontrado em '{name}'. EaterAutoRegistrar ser� ignorado.",
                    this);
            }
        }

        private void OnEnable()
        {
            if (_actor == null)
            {
                return;
            }

            _shouldRetryInStart = !TryRegisterNow();
        }

        private void Start()
        {
            if (_actor == null || _registered || !_shouldRetryInStart)
            {
                return;
            }

            _shouldRetryInStart = false;
            TryRegisterNow();
        }

        private bool TryRegisterNow()
        {
            if (_registered || _actor == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(_actor.ActorId))
            {
                return false;
            }

            string sceneName = gameObject.scene.name;

            if (!DependencyManager.Provider.TryGetForScene(sceneName, out _eaterDomain) || _eaterDomain == null)
            {
                DebugUtility.LogWarning<EaterAutoRegistrar>(
                    $"IEaterDomain n�o encontrado para a cena '{sceneName}'. " +
                    $"Garanta GameplayDomainBootstrapper nessa cena e maxSceneServices adequado.",
                    this);
                _eaterDomain = null;
                return false;
            }

            _registered = _eaterDomain.RegisterEater(_actor);
            return _registered;
        }

        private void OnDisable()
        {
            if (!_registered || _eaterDomain == null || _actor == null)
            {
                return;
            }

            _eaterDomain.UnregisterEater(_actor);
            _registered = false;
        }
    }
}

