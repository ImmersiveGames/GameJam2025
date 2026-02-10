using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings
{
    /// <summary>
    /// Catálogo explícito (asset) para mapear <see cref="SceneFlowProfileId"/> → <see cref="SceneTransitionProfile"/>.
    ///
    /// Motivação:
    /// - Evitar resolver profiles via <see cref="Resources.Load"/> em cada transição.
    /// - Centralizar pontos de configuração para transições (por profile) via referência direta.
    ///
    /// Observações:
    /// - O catálogo é opcional: se não existir (ou não tiver uma entrada), o resolver pode usar
    ///   fallback legado via Resources, conforme <see cref="AllowLegacyResourcesFallback"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneTransitionProfileCatalog",
        menuName = "ImmersiveGames/SceneFlow/SceneTransitionProfileCatalog",
        order = 10)]
    public sealed class SceneTransitionProfileCatalogAsset : ScriptableObject
    {
        /// <summary>
        /// Caminho sugerido para Resources.Load do catálogo.
        /// (Ex.: Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset)
        /// </summary>
        public const string DefaultResourcesPath = "SceneFlow/SceneTransitionProfileCatalog";

        [Header("Legacy fallback")]
        [Tooltip("Se true, o resolver pode cair no comportamento legado (Resources.Load) quando não há entry no catálogo.")]
        [SerializeField] private bool _allowLegacyResourcesFallback = true;

        [Tooltip("Base path usado pelo fallback legado (Resources). Ex.: SceneFlow/Profiles")]
        [SerializeField] private string _legacyResourcesBasePath = SceneFlowProfilePaths.ProfilesRoot;

        [Header("Entries")]
        [SerializeField] private List<Entry> _entries = new();

        public bool AllowLegacyResourcesFallback => _allowLegacyResourcesFallback;

        public string LegacyResourcesBasePath => string.IsNullOrWhiteSpace(_legacyResourcesBasePath)
            ? SceneFlowProfilePaths.ProfilesRoot
            : _legacyResourcesBasePath.Trim();

        public IReadOnlyList<Entry> Entries => _entries;

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private SceneFlowProfileId _profileId;
            [SerializeField] private SceneTransitionProfile _profile;

            public SceneFlowProfileId ProfileId => _profileId;
            public SceneTransitionProfile Profile => _profile;
        }

        public bool TryGetProfile(SceneFlowProfileId id, out SceneTransitionProfile profile)
        {
            profile = null;
            if (!id.IsValid)
                return false;

            if (_entries == null || _entries.Count == 0)
                return false;

            // Linear scan: o catálogo tende a ser pequeno (startup/frontend/gameplay + variações).
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null)
                    continue;

                if (e.ProfileId == id)
                {
                    profile = e.Profile;
                    return profile != null;
                }
            }

            return false;
        }
    }
}
