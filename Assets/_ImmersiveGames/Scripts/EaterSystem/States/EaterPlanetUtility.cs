using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Core;

namespace _ImmersiveGames.Scripts.EaterSystem.States
{
    /// <summary>
    /// Utilitários para resolver informações de planetas marcados a partir de eventos de detecção.
    /// </summary>
    internal static class EaterPlanetUtility
    {
        public static bool TryResolveMarkedPlanet(IDetectable detectable, out IPlanetActor planetActor, out MarkPlanet markedPlanet)
        {
            markedPlanet = null;

            if (!TryResolvePlanetActor(detectable, out planetActor))
            {
                return false;
            }

            return TryResolveMarkPlanet(planetActor, out markedPlanet);
        }

        public static bool TryResolvePlanetActor(IDetectable detectable, out IPlanetActor planetActor)
        {
            planetActor = null;

            if (detectable == null)
            {
                return false;
            }

            if (detectable.Owner is IPlanetActor ownerPlanetActor)
            {
                planetActor = ownerPlanetActor;
                return true;
            }

            if (detectable is UnityEngine.Component component)
            {
                if (component.TryGetComponent(out ownerPlanetActor))
                {
                    planetActor = ownerPlanetActor;
                    return true;
                }

                ownerPlanetActor = component.GetComponentInParent<IPlanetActor>();
                if (ownerPlanetActor != null)
                {
                    planetActor = ownerPlanetActor;
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveMarkPlanet(IPlanetActor planetActor, out MarkPlanet markedPlanet)
        {
            markedPlanet = null;

            UnityEngine.Transform planetTransform = planetActor?.PlanetActor?.Transform;
            if (planetTransform == null)
            {
                return false;
            }

            if (!planetTransform.TryGetComponent(out markedPlanet))
            {
                markedPlanet = planetTransform.GetComponentInParent<MarkPlanet>();
            }

            if (markedPlanet == null)
            {
                return false;
            }

            return markedPlanet.IsMarked;
        }

        public static string GetPlanetDisplayName(IPlanetActor planetActor)
        {
            if (planetActor?.PlanetActor == null)
            {
                return "desconhecido";
            }

            string actorName = planetActor.PlanetActor.ActorName;
            if (!string.IsNullOrWhiteSpace(actorName))
            {
                return actorName;
            }

            UnityEngine.Transform transform = planetActor.PlanetActor.Transform;
            return transform != null ? transform.name : "desconhecido";
        }
    }
}
