using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime
{
    /// <summary>
    /// Registro global que agrupa detectáveis por DetectionType, permitindo que os sensores
    /// consultem atores próximos sem depender exclusivamente de colisores físicos.
    /// </summary>
    public static class DetectableRegistry
    {
        private static readonly Dictionary<DetectionType, HashSet<IDetectable>> _byType = new();
        private static readonly Dictionary<IDetectable, DetectionType> _reverseLookup = new();

        public static void Register(IDetectable detectable, DetectionType detectionType)
        {
            if (detectable == null || detectionType == null)
            {
                DebugUtility.LogWarning(typeof(DetectableRegistry),
                    "Tentativa de registrar detectável inválido no DetectableRegistry.");
                return;
            }

            if (!_byType.TryGetValue(detectionType, out HashSet<IDetectable> set))
            {
                set = new HashSet<IDetectable>();
                _byType[detectionType] = set;
            }

            set.Add(detectable);
            _reverseLookup[detectable] = detectionType;
        }

        public static void Unregister(IDetectable detectable)
        {
            if (detectable == null)
            {
                return;
            }

            if (!_reverseLookup.TryGetValue(detectable, out var detectionType))
            {
                return;
            }

            if (_byType.TryGetValue(detectionType, out HashSet<IDetectable> set))
            {
                set.Remove(detectable);
                if (set.Count == 0)
                {
                    _byType.Remove(detectionType);
                }
            }

            _reverseLookup.Remove(detectable);
        }

        public static IReadOnlyCollection<IDetectable> GetByType(DetectionType detectionType)
        {
            if (detectionType != null && _byType.TryGetValue(detectionType, out HashSet<IDetectable> set))
            {
                return set;
            }

            return System.Array.Empty<IDetectable>();
        }
    }
}

