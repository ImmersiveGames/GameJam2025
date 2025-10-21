using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Services
{
    
    
    public class PlanetInteractService
    {
        private readonly PlanetMarkingManager _markingManager = PlanetMarkingManager.Instance;

        public bool TryInteractWithPlanet(Transform origin, float interactionDistance, LayerMask planetLayerMask, Vector3 raycastOffset)
        {
            var rayOrigin = origin.position + origin.TransformDirection(raycastOffset);
            var direction = origin.forward;

            if (!Physics.Raycast(rayOrigin, direction, out var hit, interactionDistance, planetLayerMask))
            {
                DebugUtility.LogVerbose<PlanetInteractService>("Raycast não acertou nenhum objeto");
                return false;
            }

            var hitObject = hit.collider.gameObject;
            DebugUtility.LogVerbose<PlanetInteractService>($"Raycast acertou: {hitObject.name}");

            return _markingManager.TryMarkPlanet(hitObject);
        }

        public bool IsPlanetInSight(Transform origin, float interactionDistance, LayerMask planetLayerMask, Vector3 raycastOffset, out MarkPlanet markPlanet, out float distance)
        {
            markPlanet = null;
            distance = 0f;

            var rayOrigin = origin.position + origin.TransformDirection(raycastOffset);
            var direction = origin.forward;

            if (Physics.Raycast(rayOrigin, direction, out var hit, interactionDistance, planetLayerMask))
            {
                markPlanet = hit.collider.gameObject.GetComponentInParent<MarkPlanet>();
                distance = hit.distance;
                return markPlanet != null;
            }

            return false;
        }
    }
}