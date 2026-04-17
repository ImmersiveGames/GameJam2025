using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring
{
    [CreateAssetMenu(
        fileName = "PhaseDefinitionAsset",
        menuName = "ImmersiveGames/NewScripts/Orchestration/PhaseDefinition/Definitions/PhaseDefinitionAsset",
        order = 30)]
    public sealed class PhaseDefinitionAsset : ScriptableObject
    {
        public enum PhaseSceneRole
        {
            Unknown = 0,
            Main = 1,
            Additive = 2,
            Overlay = 3,
        }

        public enum PhasePlayerRole
        {
            Unknown = 0,
            Local = 1,
            Remote = 2,
            Bot = 3,
            Shared = 4,
        }

        [Serializable]
        public sealed class PhaseIdentityBlock
        {
            public PhaseDefinitionId phaseId;
            public string displayName = string.Empty;
            public string description = string.Empty;
        }

        [Serializable]
        public sealed class PhaseContentBlock
        {
            public List<PhaseContentEntry> entries = new();
        }

        [Serializable]
        public sealed class PhaseContentEntry
        {
            public string localId = string.Empty;
            public SceneKeyAsset sceneRef;
            public PhaseSceneRole role;
        }

        [Serializable]
        public sealed class PhasePlayersBlock
        {
            public List<PhasePlayerEntry> entries = new();
        }

        [Serializable]
        public sealed class PhasePlayerEntry
        {
            public string localId = string.Empty;
            public PhasePlayerRole role;
        }

        [Header("Identity")]
        [SerializeField] private PhaseIdentityBlock identity = new();

        [Header("Content")]
        [SerializeField] private PhaseContentBlock content = new();

        [Header("Players")]
        [SerializeField] private PhasePlayersBlock players = new();

        public PhaseIdentityBlock Identity => identity;
        public PhaseContentBlock Content => content;
        public PhasePlayersBlock Players => players;

        public PhaseDefinitionId PhaseId => identity != null ? identity.phaseId : PhaseDefinitionId.None;

        public void ValidateOrFail(string owner = null)
        {
            string assetOwner = string.IsNullOrWhiteSpace(owner) ? name : owner;

            if (identity == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing identity block. asset='{assetOwner}'.");
            }

            identity.displayName = Normalize(identity.displayName);
            identity.description = Normalize(identity.description);
            identity.phaseId = PhaseDefinitionId.FromName(identity.phaseId.Value);

            if (!identity.phaseId.IsValid)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Invalid phaseId. asset='{assetOwner}'.");
            }

            if (string.IsNullOrWhiteSpace(identity.displayName))
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing displayName. asset='{assetOwner}', phaseId='{identity.phaseId}'.");
            }

            ValidateContentBlock(assetOwner);
            ValidatePlayersBlock(assetOwner);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateOrFail();
        }
#endif

        private void ValidateContentBlock(string assetOwner)
        {
            if (content == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing content block. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            ValidateEntries(
                content.entries,
                assetOwner,
                "content",
                entry =>
                {
                    entry.localId = Normalize(entry.localId);
                    if (entry.sceneRef == null)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Content entry missing sceneRef. asset='{assetOwner}', phaseId='{PhaseId}'.");
                    }

                    if (entry.role == PhaseSceneRole.Unknown)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Content entry missing role. asset='{assetOwner}', phaseId='{PhaseId}', localId='{entry.localId}'.");
                    }
                });
        }

        private void ValidatePlayersBlock(string assetOwner)
        {
            if (players == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing players block. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            ValidateEntries(
                players.entries,
                assetOwner,
                "players",
                entry =>
                {
                    entry.localId = Normalize(entry.localId);
                    if (entry.role == PhasePlayerRole.Unknown)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Player entry missing role. asset='{assetOwner}', phaseId='{PhaseId}', localId='{entry.localId}'.");
                    }
                });
        }

        private static void ValidateEntries<T>(
            List<T> entries,
            string assetOwner,
            string blockName,
            Action<T> validateEntry)
            where T : class
        {
            if (entries == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing {blockName} entries list. asset='{assetOwner}'.");
            }

            for (int i = 0; i < entries.Count; i++)
            {
                T entry = entries[i];
                if (entry == null)
                {
                    throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Null entry in {blockName}. asset='{assetOwner}', index={i}.");
                }

                validateEntry(entry);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

    }
}

