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
                return;
            }

            int invalidCount = 0;
            foreach (var entry in styles)
            {
                if (entry == null || !entry.styleId.IsValid)
                {
                    invalidCount++;
                    continue;
                }

                if (_cache.ContainsKey(entry.styleId))
                {
                    invalidCount++;
                    if (warnOnInvalidStyles)
                    {
                        DebugUtility.LogWarning<TransitionStyleCatalogAsset>(
                            $"[SceneFlow] Estilo duplicado no TransitionStyleCatalog. styleId='{entry.styleId}'. Apenas o primeiro será usado.");
                    }
                    continue;
                }

                if (entry.transitionProfile == null)
                {
                    invalidCount++;
                    if (warnOnInvalidStyles)
                    {
                        DebugUtility.LogWarning<TransitionStyleCatalogAsset>(
                            $"[FATAL][Config] TransitionStyle sem SceneTransitionProfile. styleId='{entry.styleId}'.");
                    }

                    continue;
                }

                _cache.Add(entry.styleId, new TransitionStyleDefinition(entry.transitionProfile, entry.profileId, entry.useFade));
            }

            if (warnOnInvalidStyles && invalidCount > 0)
            {
                DebugUtility.LogWarning<TransitionStyleCatalogAsset>(
                    $"[SceneFlow] TransitionStyleCatalog possui entradas inválidas/duplicadas. invalidCount={invalidCount}.");
            }
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

                DebugUtility.LogError<TransitionStyleCatalogAsset>(
                    $"[FATAL][Config] TransitionStyleCatalog com referência nula de SceneTransitionProfile. asset='{assetPath}', index={i}, styleId='{entry.styleId}'.");
            }
        }
#else
        private void ValidateTransitionProfileReferences() { }
#endif
    }
}
