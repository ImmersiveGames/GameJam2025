using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.CameraSystems
{
    /// <summary>
    /// Respons�vel por registrar a c�mera de gameplay no CameraResolver.
    /// Deve ser colocado na c�mera principal da GameplayScene.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public class OldGameplayCameraBinder : MonoBehaviour
    {
        [SerializeField] private int playerId;

        private IOldCameraResolver _resolver;
        private Camera _camera;

        private bool _registered;
        private bool _triedResolve;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_camera == null)
            {
                DebugUtility.LogError<OldGameplayCameraBinder>("OldGameplayCameraBinder exige um componente Camera no mesmo GameObject.");
            }

            // N�o registra aqui. Apenas tenta resolver (pode falhar dependendo da ordem de bootstrap).
            TryResolve();
        }

        private void Start()
        {
            // Caso o resolver global ainda n�o existisse no Awake, tenta novamente no Start.
            if (_resolver == null)
            {
                TryResolve();
            }

            // Se o objeto j� est� enabled (normal), OnEnable j� ter� tentado registrar.
            // Mas se a resolu��o s� ficou pronta no Start, garantimos o registro aqui.
            TryRegister();
        }

        private void OnEnable()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            TryUnregister();
        }

        private void TryResolve()
        {
            if (_triedResolve && _resolver != null)
            {
                return;
            }

            _triedResolve = true;

            if (!DependencyManager.Provider.TryGetGlobal(out _resolver) || _resolver == null)
            {
                // N�o � erro fatal; depende da ordem de bootstrap global.
                DebugUtility.LogWarning<OldGameplayCameraBinder>(
                    "OldCameraResolverService n�o encontrado no DI global (ainda). Vou tentar novamente no Start.",
                    this);

                _resolver = null;
            }
        }

        private void TryRegister()
        {
            if (_registered)
            {
                return;
            }

            if (_camera == null)
            {
                return;
            }

            if (_resolver == null)
            {
                // Tenta resolver caso ainda n�o tenha sido poss�vel.
                TryResolve();
                if (_resolver == null)
                {
                    return;
                }
            }

            _resolver.RegisterCamera(playerId, _camera);
            _registered = true;

            DebugUtility.Log<OldGameplayCameraBinder>(
                $"C�mera registrada como Player {playerId}: {_camera.name}",
                DebugUtility.Colors.Info);
        }

        private void TryUnregister()
        {
            if (!_registered)
            {
                return;
            }

            if (_resolver == null || _camera == null)
            {
                _registered = false;
                return;
            }

            _resolver.UnregisterCamera(playerId, _camera);
            _registered = false;
        }
    }
}


