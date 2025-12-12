using System;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Utilitário que controla solicitações de congelamento de órbita de um planeta marcado.
    /// Mantém o controle de referência do <see cref="PlanetMotion"/> associado para garantir liberação correta.
    /// </summary>
    internal sealed class PlanetOrbitFreezeController
    {
        private readonly object _requester;
        private PlanetMotion _currentMotion;
        private bool _loggedMissingMotion;

        public PlanetOrbitFreezeController(object requester)
        {
            _requester = requester ?? throw new ArgumentNullException(nameof(requester));
        }

        public bool TryFreeze(Behavior.EaterBehavior behavior, Transform target)
        {
            if (target == null)
            {
                Release();
                return false;
            }

            var motion = ResolvePlanetMotion(target);
            if (motion == null)
            {
                LogMissingPlanetMotion(behavior);
                return false;
            }

            _loggedMissingMotion = false;

            if (!ReferenceEquals(_currentMotion, motion))
            {
                Release();
                _currentMotion = motion;
            }

            _currentMotion.RequestOrbitFreeze(_requester);
            return true;
        }

        public void Release()
        {
            if (_currentMotion == null)
            {
                return;
            }

            _currentMotion.ReleaseOrbitFreeze(_requester);
            _currentMotion = null;
        }

        private static PlanetMotion ResolvePlanetMotion(Transform planetTransform)
        {
            if (planetTransform == null)
            {
                return null;
            }

            return planetTransform.TryGetComponent(out PlanetMotion directMotion) ? directMotion : planetTransform.GetComponentInParent<PlanetMotion>();

        }

        private void LogMissingPlanetMotion(Behavior.EaterBehavior behavior)
        {
            if (behavior == null || !behavior.ShouldLogStateTransitions || _loggedMissingMotion)
            {
                return;
            }

            DebugUtility.LogWarning(
                "Planeta marcado não possui PlanetMotion para congelar órbita durante a interação.",
                behavior,
                _requester);

            _loggedMissingMotion = true;
        }
    }
}
