using System;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    /// <summary>
    /// Gerencia solicitações de congelamento de órbita utilizando <see cref="PlanetMotion"/>.
    /// Permite registrar e liberar o congelamento preservando logs centralizados.
    /// </summary>
    internal sealed class PlanetOrbitFreezeHandle
    {
        private readonly object _requester;
        private PlanetMotion _planetMotion;
        private bool _loggedMissingMotion;

        public PlanetOrbitFreezeHandle(object requester)
        {
            _requester = requester ?? throw new ArgumentNullException(nameof(requester));
        }

        public void Request(Transform planetTransform, bool shouldLog, object context, object instance)
        {
            PlanetMotion planetMotion = ResolvePlanetMotion(planetTransform);
            if (planetMotion == null)
            {
                if (shouldLog && !_loggedMissingMotion)
                {
                    DebugUtility.LogWarning(
                        "Planeta marcado não possui PlanetMotion para congelar órbita.",
                        context,
                        instance);
                    _loggedMissingMotion = true;
                }

                return;
            }

            _loggedMissingMotion = false;

            if (!ReferenceEquals(_planetMotion, planetMotion))
            {
                Release();
                _planetMotion = planetMotion;
            }

            _planetMotion.RequestOrbitFreeze(_requester);
        }

        public void Release()
        {
            if (_planetMotion == null)
            {
                return;
            }

            _planetMotion.ReleaseOrbitFreeze(_requester);
            _planetMotion = null;
        }

        private static PlanetMotion ResolvePlanetMotion(Transform planetTransform)
        {
            if (planetTransform == null)
            {
                return null;
            }

            if (planetTransform.TryGetComponent(out PlanetMotion directMotion))
            {
                return directMotion;
            }

            return planetTransform.GetComponentInParent<PlanetMotion>();
        }
    }
}
