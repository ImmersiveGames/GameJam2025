using _ImmersiveGames.Scripts.UI.Compass;
using UnityEngine;

namespace _ImmersiveGames.Scripts.World.Compass
{
    /// <summary>
    /// Componente padrão para marcar um GameObject como rastreável pela bússola.
    /// </summary>
    public class CompassTarget : MonoBehaviour, ICompassTrackable
    {
        [Header("Compass Target")]
        [Tooltip("Tipo de alvo exibido na bússola.")]
        public CompassTargetType targetType = CompassTargetType.PointOfInterest;

        private void OnEnable()
        {
            CompassRuntimeService.RegisterTarget(this);
        }

        private void OnDisable()
        {
            CompassRuntimeService.UnregisterTarget(this);
        }

        Transform ICompassTrackable.Transform => transform;

        CompassTargetType ICompassTrackable.TargetType => targetType;

        bool ICompassTrackable.IsActive => true;
    }
}
