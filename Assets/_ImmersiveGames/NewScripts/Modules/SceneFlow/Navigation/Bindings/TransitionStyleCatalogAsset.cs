using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEngine;
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

            [Tooltip("Profile canônico do SceneFlow (id semântico para regras de runtime).")]
            public SceneFlowProfileId profileId = SceneFlowProfileId.Frontend;

            [Tooltip("Referência direta ao SceneTransitionProfile usado pela transição.")]
            public SceneTransitionProfile transitionProfile;

            [Tooltip("Quando true, aplica fade (se o SceneFlow suportar).")]
            public bool useFade = true;

            public override string ToString()
                => $"styleId='{styleId}', profile='{profileId}', useFade={useFade}";
        }

        [Header("Styles")]
        [SerializeField] private List<StyleEntry> styles = new();

        [Header("Profile Catalog (Consistency Validation)")]
        [Tooltip("Catálogo canônico usado para validar consistência entre profileId (legado) e transitionProfile (AssetRef).")]
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
            EnsureCache();
            ValidateTransitionProfileReferences();
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
                FailFast("TransitionStyleCatalog sem estilos configurados.");
            }

            int degradedStyles = 0;

            for (int i = 0; i < styles.Count; i++)
            {
                var entry = styles[i];
                if (entry == null)
                {
                    FailFast($"StyleEntry nulo em styles[{i}].");
                }

                if (!entry.styleId.IsValid)
                {
                    FailFast($"StyleEntry inválido em styles[{i}] (styleId vazio/inválido).");
                }

                if (_cache.ContainsKey(entry.styleId))
                {
                    FailFast($"Estilo duplicado no TransitionStyleCatalog. styleId='{entry.styleId}', index={i}.");
                }

                if (entry.transitionProfile == null)
                {
                    degradedStyles++;

                    DebugUtility.LogWarning<TransitionStyleCatalogAsset>(
                        $"[WARN][Degraded] TransitionStyle sem SceneTransitionProfile. styleId='{entry.styleId}', profileId='{entry.profileId}'. Fallback=no-fade (dur=0).");

                    _cache.Add(entry.styleId, new TransitionStyleDefinition(null, entry.profileId, false));
                    continue;
                }

                ValidateProfileIdConsistency(entry);

                _cache.Add(entry.styleId, new TransitionStyleDefinition(entry.transitionProfile, entry.profileId, entry.useFade));

                DebugUtility.LogVerbose<TransitionStyleCatalogAsset>(
                    $"[OBS][Config] StyleResolvedVia=AssetRef styleId='{entry.styleId}' profileId='{entry.profileId}' asset='{entry.transitionProfile.name}' useFade={entry.useFade}.",
                    DebugUtility.Colors.Info);
            }

            if (warnOnInvalidStyles)
            {
                DebugUtility.LogVerbose<TransitionStyleCatalogAsset>(
                    $"[OBS][Config] TransitionStyleCatalogBuild stylesResolved={_cache.Count} invalidStyles=0 degradedStyles={degradedStyles}",
                    DebugUtility.Colors.Info);
            }
        }

        private void ValidateProfileIdConsistency(StyleEntry entry)
        {
            if (!entry.profileId.IsValid)
            {
                return;
            }

            if (transitionProfileCatalog == null)
            {
                FailFast($"TransitionStyleCatalog sem referência ao SceneTransitionProfileCatalogAsset para validar consistência id/ref. styleId='{entry.styleId}', profileId='{entry.profileId}'.");
            }

            if (!transitionProfileCatalog.TryGetProfile(entry.profileId, out var profileFromCatalog) || profileFromCatalog == null)
            {
                FailFast($"profileId não encontrado no SceneTransitionProfileCatalogAsset. styleId='{entry.styleId}', profileId='{entry.profileId}'.");
            }

            if (profileFromCatalog != entry.transitionProfile)
            {
                FailFast(
                    $"Inconsistência entre profileId e transitionProfile. styleId='{entry.styleId}', profileId='{entry.profileId}', " +
                    $"catalogProfile='{profileFromCatalog.name}', directProfile='{entry.transitionProfile.name}'.");
            }
        }

        private static void FailFast(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError<TransitionStyleCatalogAsset>(message);
            throw new InvalidOperationException(message);
        }

#if UNITY_EDITOR
        [ContextMenu("Validate Transition Profiles")]
        private void ValidateTransitionProfileReferences()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (styles == null)
            {
                return;
            }

            for (int i = 0; i < styles.Count; i++)
            {
                var entry = styles[i];
                if (entry == null || entry.transitionProfile != null)
                {
                    continue;
                }

                DebugUtility.LogWarning<TransitionStyleCatalogAsset>(
                    $"[WARN][Degraded] TransitionStyleCatalog com referência nula de SceneTransitionProfile. asset='{assetPath}', index={i}, styleId='{entry.styleId}'. Fallback=no-fade (dur=0).");
            }
        }
#else
        private void ValidateTransitionProfileReferences() { }
#endif
    }
}
