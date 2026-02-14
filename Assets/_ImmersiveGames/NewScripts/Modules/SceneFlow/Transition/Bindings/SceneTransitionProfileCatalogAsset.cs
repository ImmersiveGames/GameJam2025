using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings
{
    /// <summary>
    /// Catálogo explícito (asset) para mapear <see cref="SceneFlowProfileId"/> → <see cref="SceneTransitionProfile"/>.
    /// Fonte única para resolução de profiles no SceneFlow.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneTransitionProfileCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Catalogs/SceneTransitionProfileCatalogAsset",
        order = 30)]
    public sealed class SceneTransitionProfileCatalogAsset : ScriptableObject
    {
        /// <summary>
        /// Caminho esperado para Resources.Load do catálogo.
        /// (Ex.: Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset)
        /// </summary>
        public const string DefaultResourcesPath = "SceneFlow/SceneTransitionProfileCatalog";

        [Header("Entries")]
        [SerializeField] private List<Entry> _entries = new();

        public IReadOnlyList<Entry> Entries => _entries;

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private SceneFlowProfileId _profileId;
            [SerializeField] private SceneTransitionProfile _profile;

            public SceneFlowProfileId ProfileId => _profileId;
            public SceneTransitionProfile Profile => _profile;

            internal void Set(SceneFlowProfileId profileId, SceneTransitionProfile profile)
            {
                _profileId = profileId;
                _profile = profile;
            }

            internal static Entry Create(SceneFlowProfileId profileId, SceneTransitionProfile profile)
            {
                var entry = new Entry();
                entry.Set(profileId, profile);
                return entry;
            }
        }

        public bool SetOrAddProfile(SceneFlowProfileId id, SceneTransitionProfile profile)
        {
            if (!id.IsValid || profile == null)
                return false;

            _entries ??= new List<Entry>();

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e == null)
                    continue;

                if (e.ProfileId == id)
                {
                    e.Set(id, profile);
                    return true;
                }
            }

            _entries.Add(Entry.Create(id, profile));
            return true;
        }

        public bool TryGetProfile(SceneFlowProfileId id, out SceneTransitionProfile profile)
        {
            profile = null;
            if (!id.IsValid)
                return false;

            if (_entries == null || _entries.Count == 0)
                return false;

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
