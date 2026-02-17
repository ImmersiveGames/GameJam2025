using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Catálogo configurável de estilos de transição (TransitionStyleId -> profile/fade).
    /// </summary>
    [CreateAssetMenu(
        fileName = "TransitionStyleCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Catalogs/TransitionStyleCatalogAsset",
        order = 30)]
    public sealed class TransitionStyleCatalogAsset : ScriptableObject, ITransitionStyleCatalog
    {
        [Serializable]
        public sealed class StyleEntry
        {
            [Tooltip("Id canônico do estilo (TransitionStyleId).")]
            public TransitionStyleId styleId;

            [Tooltip("Profile canônico do SceneFlow (id semântico para regras de validação/editor).")]
            public SceneFlowProfileId profileId = SceneFlowProfileId.Frontend;

            [FormerlySerializedAs("transitionProfile")]
            [Tooltip("Referência direta ao SceneTransitionProfile usado pela transição (obrigatório).")]
            public SceneTransitionProfile profileRef;

            [Tooltip("Quando true, aplica fade (se o SceneFlow suportar).")]
            public bool useFade = true;

            public override string ToString()
                => $"styleId='{styleId}', profile='{profileId}', useFade={useFade}";
        }

        [Header("Styles")]
        [SerializeField] private List<StyleEntry> styles = new();

        [Header("Profile Catalog (Legacy / Validation-only)")]
        [Tooltip("Campo legado mantido para compatibilidade YAML/editor. Nunca é usado em runtime para resolver profile.")]
        [SerializeField] private SceneTransitionProfileCatalogAsset transitionProfileCatalog;

        [Header("Validation")]
        [Tooltip("Quando true, registra warning se houver estilos inválidos/duplicados.")]
        [SerializeField] private bool warnOnInvalidStyles = true;

        private readonly Dictionary<TransitionStyleId, TransitionStyleDefinition> _cache = new();
        private bool _cacheBuilt;

        public bool TryGet(TransitionStyleId styleId, out TransitionStyleDefinition style)
        {
            style = default;

            if (!styleId.IsValid)
            {
                return false;
            }

            EnsureCache();
            return _cache.TryGetValue(styleId, out style);
        }

        private void OnEnable()
        {
            _cacheBuilt = false;
        }

        private void OnValidate()
        {
            _cacheBuilt = false;
            ValidateTransitionProfileReferences();
            EnsureCache();
        }

        private void EnsureCache()
        {
            if (_cacheBuilt)
            {
                return;
            }

            _cacheBuilt = true;
            _cache.Clear();

            if (styles == null || styles.Count == 0)
            {
                FailFastConfig("TransitionStyleCatalog has no configured styles.");
            }

            for (int i = 0; i < styles.Count; i++)
            {
                StyleEntry entry = styles[i];
                if (entry == null)
                {
                    FailFastConfig($"TransitionStyleCatalog contains null StyleEntry at styles[{i}].");
                }

                if (!entry.styleId.IsValid)
                {
                    FailFastConfig($"TransitionStyleCatalog contains invalid styleId at styles[{i}].");
                }

                if (_cache.ContainsKey(entry.styleId))
                {
                    FailFastConfig($"TransitionStyleCatalog has duplicated styleId='{entry.styleId}' at index={i}.");
                }

                if (!entry.profileId.IsValid)
                {
                    FailFastConfig($"TransitionStyleCatalog: invalid profileId for styleId='{entry.styleId}'.");
                }

                if (entry.profileRef == null)
                {
                    FailFastConfig($"TransitionStyleCatalog: missing profileRef for styleId='{entry.styleId}' profileId='{entry.profileId}'.");
                }

                _cache.Add(entry.styleId, new TransitionStyleDefinition(entry.profileRef, entry.profileId, entry.useFade));

                DebugUtility.LogVerbose<TransitionStyleCatalogAsset>(
                    $"[OBS][Config] StyleResolvedVia=AssetRef styleId='{entry.styleId}' profileId='{entry.profileId}' asset='{entry.profileRef.name}' useFade={entry.useFade}.",
                    DebugUtility.Colors.Info);
            }

            if (warnOnInvalidStyles)
            {
                DebugUtility.LogVerbose<TransitionStyleCatalogAsset>(
                    $"[OBS][Config] TransitionStyleCatalogBuild stylesResolved={_cache.Count} invalidStyles=0",
                    DebugUtility.Colors.Info);
            }
        }

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<TransitionStyleCatalogAsset>(message);
            throw new InvalidOperationException(message);
        }

#if UNITY_EDITOR
        [ContextMenu("Validate Transition Profiles")]
        private void ValidateTransitionProfileReferences()
        {
            if (styles == null)
            {
                return;
            }

            int invalidEntriesCount = 0;
            int noFadeEntriesCount = 0;

            for (int i = 0; i < styles.Count; i++)
            {
                StyleEntry entry = styles[i];
                if (entry == null)
                {
                    invalidEntriesCount++;
                    continue;
                }

                if (!entry.profileId.IsValid || entry.profileRef == null)
                {
                    invalidEntriesCount++;
                }

                if (!entry.useFade)
                {
                    noFadeEntriesCount++;
                }
            }

            if (invalidEntriesCount > 0 || noFadeEntriesCount > 0)
            {
                DebugUtility.Log(typeof(TransitionStyleCatalogAsset),
                    $"[OBS][Config] TransitionStyleCatalog OnValidate summary: invalidEntries={invalidEntriesCount}, noFadeEntries={noFadeEntriesCount}, asset='{name}'.",
                    DebugUtility.Colors.Info);
            }
        }
#else
        private void ValidateTransitionProfileReferences() { }
#endif
    }
}
