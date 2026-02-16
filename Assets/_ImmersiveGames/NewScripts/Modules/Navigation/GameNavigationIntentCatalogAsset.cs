using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo canônico de intents de navegação.
    ///
    /// Estrutura:
    /// - Core: intents canônicos do domínio (pode conter aliases oficiais).
    /// - Custom: intents não-canônicos/expansões de projeto.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameNavigationIntentCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/Navigation/Catalogs/GameNavigationIntentCatalogAsset",
        order = 31)]
    public sealed class GameNavigationIntentCatalogAsset : ScriptableObject
    {
        private static readonly NavigationIntentId MenuCanonicalId = NavigationIntentId.FromName("to-menu");
        private static readonly NavigationIntentId GameplayCanonicalId = NavigationIntentId.FromName("to-gameplay");
        private static readonly NavigationIntentId GameOverCanonicalId = NavigationIntentId.FromName("gameover");
        private static readonly NavigationIntentId VictoryCanonicalId = NavigationIntentId.FromName("victory");
        private static readonly NavigationIntentId RestartCanonicalId = NavigationIntentId.FromName("restart");
        private static readonly NavigationIntentId ExitToMenuCanonicalId = NavigationIntentId.FromName("exit-to-menu");

        [Serializable]
        public sealed class IntentEntry
        {
            [Tooltip("Id canônico da intent.")]
            public NavigationIntentId intentId;

            [Tooltip("Referência direta da rota para a intent.")]
            public SceneRouteDefinitionAsset routeRef;

            [Tooltip("Style de transição associado à intent.")]
            public TransitionStyleId styleId;

            [Tooltip("Quando true, a intent é crítica e obrigatória para validação de bootstrap.")]
            public bool criticalRequired;

            public void Normalize()
            {
                intentId = NavigationIntentId.FromName(intentId.Value);
            }
        }

        [Header("Core (canônico + aliases oficiais)")]
        [SerializeField] private List<IntentEntry> core = new();

        [Header("Custom (extensões não-canônicas)")]
        [SerializeField] private List<IntentEntry> custom = new();

        public NavigationIntentId Menu => TryGetCoreIntentId(MenuCanonicalId);
        public NavigationIntentId Gameplay => TryGetCoreIntentId(GameplayCanonicalId);
        public NavigationIntentId GameOver => TryGetCoreIntentId(GameOverCanonicalId);
        public NavigationIntentId Victory => TryGetCoreIntentId(VictoryCanonicalId);
        public NavigationIntentId Restart => TryGetCoreIntentId(RestartCanonicalId);
        public NavigationIntentId ExitToMenu => TryGetCoreIntentId(ExitToMenuCanonicalId);

        /// <summary>
        /// Retorna intents críticas obrigatórias configuradas no bloco Core.
        /// </summary>
        public IEnumerable<NavigationIntentId> EnumerateCriticalIntents()
        {
            if (core == null)
            {
                yield break;
            }

            for (int i = 0; i < core.Count; i++)
            {
                IntentEntry entry = core[i];
                if (entry == null || !entry.criticalRequired || !entry.intentId.IsValid)
                {
                    continue;
                }

                yield return entry.intentId;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeEntries();
            ValidateOrFail();
        }
#endif

        private NavigationIntentId TryGetCoreIntentId(NavigationIntentId canonical)
        {
            if (!canonical.IsValid || core == null)
            {
                return NavigationIntentId.None;
            }

            for (int i = 0; i < core.Count; i++)
            {
                IntentEntry entry = core[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.intentId == canonical)
                {
                    return entry.intentId;
                }
            }

            return NavigationIntentId.None;
        }

        private void NormalizeEntries()
        {
            NormalizeList(core);
            NormalizeList(custom);
        }

        private static void NormalizeList(List<IntentEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                entries[i]?.Normalize();
            }
        }

        private void ValidateOrFail()
        {
            if (core == null || core.Count == 0)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: bloco Core vazio. asset='{name}'.");
            }

            var seen = new HashSet<NavigationIntentId>();
            ValidateListOrFail(core, seen, isCore: true);
            ValidateListOrFail(custom, seen, isCore: false);

            EnsureCanonicalCoreEntryOrFail(MenuCanonicalId);
            EnsureCanonicalCoreEntryOrFail(GameplayCanonicalId);
        }

        private void ValidateListOrFail(List<IntentEntry> entries, ISet<NavigationIntentId> seen, bool isCore)
        {
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                IntentEntry entry = entries[i];
                if (entry == null)
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: entry nula. asset='{name}', block='{(isCore ? "Core" : "Custom")}', index={i}.");
                }

                if (!entry.intentId.IsValid)
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: intentId vazio. asset='{name}', block='{(isCore ? "Core" : "Custom")}', index={i}.");
                }

                if (!seen.Add(entry.intentId))
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: intent duplicada. asset='{name}', intentId='{entry.intentId}'.");
                }

                if (isCore)
                {
                    if (entry.routeRef == null)
                    {
                        FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: Core exige routeRef. asset='{name}', intentId='{entry.intentId}'.");
                    }

                    if (!entry.styleId.IsValid)
                    {
                        FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: Core exige styleId válido. asset='{name}', intentId='{entry.intentId}'.");
                    }
                }
            }
        }

        private void EnsureCanonicalCoreEntryOrFail(NavigationIntentId canonicalId)
        {
            NavigationIntentId resolved = TryGetCoreIntentId(canonicalId);
            if (!resolved.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: intent canônica ausente no Core. asset='{name}', intentId='{canonicalId}'.");
            }
        }

        private static void FailFastConfig(string message)
        {
            DebugUtility.LogError(typeof(GameNavigationIntentCatalogAsset), message);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            throw new InvalidOperationException(message);
        }
    }
}
