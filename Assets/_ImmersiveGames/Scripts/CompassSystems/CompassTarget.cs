using _ImmersiveGames.Scripts.CompassSystems.Compass;
using _ImmersiveGames.Scripts.UI.Compass;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
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

        private ICompassRuntimeService _runtimeService;

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

            if (CompassRuntimeService.TryGet(out var runtimeService))
            {
                _runtimeService = runtimeService;
            }
            else
            {
                DebugUtility.LogError<CompassTarget>("CompassRuntimeService não encontrado para registrar target.");
            }
        }

        Transform ICompassTrackable.Transform => transform;

        CompassTargetType ICompassTrackable.TargetType => targetType;

        bool ICompassTrackable.IsActive => true;
    }
}
