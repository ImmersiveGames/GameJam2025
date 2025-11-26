using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Detectable;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Utilidades compartilhadas entre os sistemas de defesa planetária.
    /// Centraliza formatação e resolução de papéis para evitar duplicação
    /// de lógica em controladores e serviços.
    /// </summary>
    public static class DefenseUtils
    {
        public static string FormatDetector(IDetector detector, DefenseRole role)
        {
            string detectorName = GetDetectorName(detector);

            return role switch
            {
                DefenseRole.Player => $"o Player ({detectorName})",
                DefenseRole.Eater => $"o Eater ({detectorName})",
                _ => detectorName
            };
        }

        public static string GetDetectorName(IDetector detector)
        {
            return detector?.Owner?.ActorName ?? detector?.ToString() ?? "Unknown";
        }

        public static DefenseRole ResolveDefenseRole(IDetector detector)
        {
            if (detector is IDefenseRoleProvider provider)
            {
                return provider.DefenseRole;
            }

            string actorName = detector?.Owner?.ActorName;
            if (string.IsNullOrEmpty(actorName))
            {
                return DefenseRole.Unknown;
            }

            if (actorName.Contains("Player"))
            {
                return DefenseRole.Player;
            }

            if (actorName.Contains("Eater"))
            {
                return DefenseRole.Eater;
            }

            return DefenseRole.Unknown;
        }

        public static void LogMissingPlanetMaster(object context, string gameObjectName)
        {
            DebugUtility.LogError(context?.GetType(),
                $"PlanetsMaster não encontrado para o controle de defesa em {gameObjectName}.",
                context as Object);
        }

        public static void LogMissingDefenseController(object context, string gameObjectName)
        {
            DebugUtility.LogError(context?.GetType(),
                $"PlanetDefenseController não encontrado em {gameObjectName}.",
                context as Object);
        }

        public static void LogDuplicateDetector(IDetector detector, DetectionType detectionType)
        {
            DebugUtility.LogVerbose<DefenseUtils>(
                $"Detector {GetDetectorName(detector)} já estava registrado para {detectionType?.TypeName ?? "Unknown"}.");
        }

        public static void LogIgnoredNullDetector(object context)
        {
            DebugUtility.LogVerbose(context?.GetType(),
                "Detector nulo ignorado na defesa planetária.");
        }

        public static bool TryAddToLookup<T>(IDictionary<T, HashSet<IDetector>> lookup, T key, IDetector detector)
        {
            if (!lookup.TryGetValue(key, out var set))
            {
                set = new HashSet<IDetector>();
                lookup[key] = set;
            }

            return set.Add(detector);
        }

        public static bool TryRemoveFromLookup<T>(IDictionary<T, HashSet<IDetector>> lookup, T key, IDetector detector)
        {
            if (!lookup.TryGetValue(key, out var set))
            {
                return false;
            }

            bool removed = set.Remove(detector);
            if (set.Count == 0)
            {
                lookup.Remove(key);
            }

            return removed;
        }
    }
}
