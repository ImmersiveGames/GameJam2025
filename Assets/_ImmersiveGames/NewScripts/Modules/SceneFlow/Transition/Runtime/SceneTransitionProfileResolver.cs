using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Resolve SceneTransitionProfile por ID, via Resources.
    /// Padrão de paths:
    /// - "SceneFlow/Profiles/<profileId.Value/>"
    /// - "<profileId.Value/>"
    ///
    /// Observação importante:
    /// - Se existir um asset com esse nome, mas de tipo legado (ex.: SceneTransitionProfile),
    ///   Resources.Load<SceneTransitionProfile/> retornará null. Este resolver detecta e loga isso.
    /// </summary>
    public sealed class SceneTransitionProfileResolver
    {
        private readonly Dictionary<string, SceneTransitionProfile> _cache = new();

        public SceneTransitionProfile Resolve(SceneFlowProfileId profileId)
        {
            return Resolve(profileId, out _);
        }

        public SceneTransitionProfile Resolve(SceneFlowProfileId profileId, out string resolvedPath)
        {
            resolvedPath = string.Empty;

            if (!profileId.IsValid)
            {
                return null;
            }

            // O value do ID já é normalizado (trim + lower) em SceneFlowProfileId.
            string key = profileId.Value;
            if (_cache.TryGetValue(key, out var cached) && cached != null)
            {
                resolvedPath = "<cache>";
                return cached;
            }

            string pathA = SceneFlowProfilePaths.For(profileId);
            string pathB = key;

            // 1) Tentativa principal (tipo correto).
            var resolved = !string.IsNullOrEmpty(pathA)
                ? Resources.Load<SceneTransitionProfile>(pathA)
                : null;

            if (resolved != null)
            {
                resolvedPath = pathA;
            }
            else
            {
                resolved = Resources.Load<SceneTransitionProfile>(pathB);
                if (resolved != null)
                {
                    resolvedPath = pathB;
                }
            }

            // 2) Sem fallback de case aqui: o ID já é normalizado (lower). Se existir um asset com casing
            // diferente no path, ele deve ser corrigido no projeto/Resources.

            if (resolved != null)
            {
                _cache[key] = resolved;

                DebugUtility.LogVerbose<SceneTransitionProfileResolver>(
                    $"[SceneFlow] Profile resolvido: name='{key}', path='{resolvedPath}', type='{resolved.GetType().FullName}'.");
                return resolved;
            }

            // 3) Diagnóstico de tipo incorreto (sem fallback funcional).
            if (!string.IsNullOrEmpty(pathA))
            {
                var anyA = Resources.Load(pathA);
                if (anyA != null)
                {
                    DebugUtility.LogError<SceneTransitionProfileResolver>(
                        $"[SceneFlow] Asset encontrado em Resources no path '{pathA}', porém com TIPO incorreto: '{anyA.GetType().FullName}'. " +
                        $"Esperado: '{typeof(SceneTransitionProfile).FullName}'. " +
                        "Ação: recrie/migre o asset como SceneTransitionProfile (CreateAssetMenu NewScripts).");
                    return null;
                }
            }

            var anyB = Resources.Load(pathB);
            if (anyB != null)
            {
                DebugUtility.LogError<SceneTransitionProfileResolver>(
                    $"[SceneFlow] Asset encontrado em Resources no path '{pathB}', porém com TIPO incorreto: '{anyB.GetType().FullName}'. " +
                    $"Esperado: '{typeof(SceneTransitionProfile).FullName}'. " +
                    "Ação: recrie/migre o asset como SceneTransitionProfile (CreateAssetMenu NewScripts).");
                return null;
            }

            DebugUtility.LogError<SceneTransitionProfileResolver>(
                $"[SceneFlow] SceneTransitionProfile '{key}' NÃO encontrado em Resources. " +
                $"Paths tentados: '{pathA}' e '{pathB}'. Confirme que o asset está em Resources/{SceneFlowProfilePaths.ProfilesRoot} e é do tipo SceneTransitionProfile.");
            return null;
        }
    }
}
