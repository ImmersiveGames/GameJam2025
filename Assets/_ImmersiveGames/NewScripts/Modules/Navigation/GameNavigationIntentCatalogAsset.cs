using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

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
        private static readonly NavigationIntentId DefeatAliasId = NavigationIntentId.FromName("defeat");
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

            [FormerlySerializedAs("criticalRequired")]
            [Tooltip("Campo legado sem efeito de fail-fast. A criticidade canônica é fixa em to-menu/to-gameplay.")]
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
        public NavigationIntentId Defeat => TryGetCoreIntentId(DefeatAliasId);
        public NavigationIntentId Victory => TryGetCoreIntentId(VictoryCanonicalId);
        public NavigationIntentId Restart => TryGetCoreIntentId(RestartCanonicalId);
        public NavigationIntentId ExitToMenu => TryGetCoreIntentId(ExitToMenuCanonicalId);

        public void EnsureCoreIntentsForProductionOrFail()
        {
            EnsureCanonicalCoreEntryOrFail(MenuCanonicalId);
            EnsureCanonicalCoreEntryOrFail(GameplayCanonicalId);
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

            EnsureCoreIntentsForProductionOrFail();
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
                    if (IsMandatoryCoreIntent(entry.intentId) && entry.routeRef == null)
                    {
                        FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: Core exige routeRef. asset='{name}', intentId='{entry.intentId}'.");
                    }

                    if (IsMandatoryCoreIntent(entry.intentId) && !entry.styleId.IsValid)
                    {
                        FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog inválido: Core exige styleId válido. asset='{name}', intentId='{entry.intentId}'.");
                    }

                    if (!IsMandatoryCoreIntent(entry.intentId) && (entry.routeRef == null || !entry.styleId.IsValid))
                    {
                        DebugUtility.Log(typeof(GameNavigationIntentCatalogAsset),
                            $"[OBS][Config] Core extra opcional com configuração parcial/ausente (permitido). asset='{name}', intentId='{entry.intentId}'.",
                            DebugUtility.Colors.Info);
                    }
                }
            }
        }

        private static bool IsMandatoryCoreIntent(NavigationIntentId intentId)
        {
            return intentId == MenuCanonicalId || intentId == GameplayCanonicalId;
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
