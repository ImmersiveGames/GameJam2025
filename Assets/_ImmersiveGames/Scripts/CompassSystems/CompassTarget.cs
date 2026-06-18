using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.Scripts.UISystems.Compass;
using UnityEngine;
namespace _ImmersiveGames.Scripts.CompassSystems
{
    /// <summary>
    /// Componente padrão para marcar um GameObject como rastreável pela bússola.
    /// </summary>
    public class CompassTarget : MonoBehaviour, ICompassTrackable
    {
        [Header("Compass Target")]
        [Tooltip("Tipo de alvo exibido na bússola.")]
        public CompassTargetType targetType = CompassTargetType.PointOfInterest;

        private IICompassRuntimeService _runtimeService;

        private void Awake()
        {
            ResolveService();
        }

        private void OnEnable()
        {
            ResolveService();
            _runtimeService?.RegisterTarget(this);
        }

        private void OnDisable()
        {
            _runtimeService?.UnregisterTarget(this);
        }

        private void ResolveService()
        {
            if (_runtimeService != null)
            {
                return;
            }

            if (TryResolveRuntimeService(out var runtimeService))
            {
                _runtimeService = runtimeService;
            }
            else
            {
                DebugUtility.LogError<CompassTarget>("ICompassRuntimeService não encontrado para registrar target.");
            }
        }

        Transform ICompassTrackable.Transform => transform;

        CompassTargetType ICompassTrackable.TargetType => targetType;

        bool ICompassTrackable.IsActive => true;

        private static bool TryResolveRuntimeService(out IICompassRuntimeService runtimeService)
        {
            runtimeService = null;
            if (DependencyManager.Provider == null)
            {
                return false;
            }

            return DependencyManager.Provider.TryGetGlobal(out runtimeService);
        }
    }
}
