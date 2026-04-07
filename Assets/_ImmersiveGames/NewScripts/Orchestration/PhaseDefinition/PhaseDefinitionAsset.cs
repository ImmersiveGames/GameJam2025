using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition
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

        public enum PhaseRuleKind
        {
            Unknown = 0,
            Constraint = 1,
            Gate = 2,
            Modifier = 3,
        }

        public enum PhaseObjectiveKind
        {
            Unknown = 0,
            Primary = 1,
            Secondary = 2,
            Optional = 3,
        }

        public enum PhaseInitialStateKind
        {
            Unknown = 0,
            Flag = 1,
            Counter = 2,
            Value = 3,
            Reference = 4,
        }

        public enum PhaseClosureResultKind
        {
            Unknown = 0,
            Completed = 1,
            Failed = 2,
            Aborted = 3,
            Branched = 4,
        }

        public enum PhaseContinuityPolicyKind
        {
            Unknown = 0,
            None = 1,
            Stay = 2,
            Menu = 3,
            Restart = 4,
            NextPhase = 5,
            Custom = 6,
        }

        [Serializable]
        public sealed class PhaseParameterEntry
        {
            public string key = string.Empty;
            public string value = string.Empty;
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
            public List<string> tags = new();
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

        [Serializable]
        public sealed class PhaseRulesObjectivesBlock
        {
            public List<PhaseRuleEntry> rules = new();
            public List<PhaseObjectiveEntry> objectives = new();
        }

        [Serializable]
        public sealed class PhaseRuleEntry
        {
            public string localId = string.Empty;
            public PhaseRuleKind ruleKind;
            public List<PhaseParameterEntry> parameters = new();
        }

        [Serializable]
        public sealed class PhaseObjectiveEntry
        {
            public string localId = string.Empty;
            public PhaseObjectiveKind objectiveKind;
            public List<PhaseParameterEntry> parameters = new();
        }

        [Serializable]
        public sealed class PhaseInitialStateBlock
        {
            public List<PhaseInitialStateEntry> entries = new();
        }

        [Serializable]
        public sealed class PhaseRunResultStageBlock
        {
            public bool hasRunResultStage = true;
            public List<PhaseParameterEntry> parameters = new();
        }

        [Serializable]
        public sealed class PhaseInitialStateEntry
        {
            public string localId = string.Empty;
            public PhaseInitialStateKind stateKind;
            public List<PhaseParameterEntry> parameters = new();
        }

        [Serializable]
        public sealed class PhaseClosureBlock
        {
            public PhaseClosureResultKind resultKind;
            public PhaseContinuityPolicyKind continuityPolicyKind;
            public List<PhaseParameterEntry> parameters = new();
        }

        [Header("Identity")]
        [SerializeField] private PhaseIdentityBlock identity = new();

        [Header("Content")]
        [SerializeField] private PhaseContentBlock content = new();

        [Header("Players")]
        [SerializeField] private PhasePlayersBlock players = new();

        [Header("Rules/Objectives")]
        [SerializeField] private PhaseRulesObjectivesBlock rulesObjectives = new();

        [Header("Initial State")]
        [SerializeField] private PhaseInitialStateBlock initialState = new();

        [Header("Run Result Stage")]
        [SerializeField] private PhaseRunResultStageBlock runResultStage = new();

        [Header("Closure")]
        [SerializeField] private PhaseClosureBlock closure = new();

        public PhaseIdentityBlock Identity => identity;
        public PhaseContentBlock Content => content;
        public PhasePlayersBlock Players => players;
        public PhaseRulesObjectivesBlock RulesObjectives => rulesObjectives;
        public PhaseInitialStateBlock InitialState => initialState;
        public PhaseRunResultStageBlock RunResultStage => runResultStage;
        public PhaseClosureBlock Closure => closure;

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
            ValidateRulesObjectivesBlock(assetOwner);
            ValidateInitialStateBlock(assetOwner);
            ValidateRunResultStageBlock(assetOwner);
            ValidateClosureBlock(assetOwner);
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

        private void ValidateRulesObjectivesBlock(string assetOwner)
        {
            if (rulesObjectives == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing rulesObjectives block. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            ValidateEntries(
                rulesObjectives.rules,
                assetOwner,
                "rulesObjectives.rules",
                entry =>
                {
                    entry.localId = Normalize(entry.localId);
                    if (entry.ruleKind == PhaseRuleKind.Unknown)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Rule entry missing ruleKind. asset='{assetOwner}', phaseId='{PhaseId}', localId='{entry.localId}'.");
                    }

                    ValidateParameters(entry.parameters, assetOwner, "rule", entry.localId);
                });

            ValidateEntries(
                rulesObjectives.objectives,
                assetOwner,
                "rulesObjectives.objectives",
                entry =>
                {
                    entry.localId = Normalize(entry.localId);
                    if (entry.objectiveKind == PhaseObjectiveKind.Unknown)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Objective entry missing objectiveKind. asset='{assetOwner}', phaseId='{PhaseId}', localId='{entry.localId}'.");
                    }

                    ValidateParameters(entry.parameters, assetOwner, "objective", entry.localId);
                });
        }

        private void ValidateInitialStateBlock(string assetOwner)
        {
            if (initialState == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing initialState block. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            ValidateEntries(
                initialState.entries,
                assetOwner,
                "initialState",
                entry =>
                {
                    entry.localId = Normalize(entry.localId);
                    if (entry.stateKind == PhaseInitialStateKind.Unknown)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] InitialState entry missing stateKind. asset='{assetOwner}', phaseId='{PhaseId}', localId='{entry.localId}'.");
                    }

                    ValidateParameters(entry.parameters, assetOwner, "initialState", entry.localId);
                });
        }

        private void ValidateRunResultStageBlock(string assetOwner)
        {
            if (runResultStage == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing runResultStage block. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            ValidateParameters(runResultStage.parameters, assetOwner, "runResultStage", PhaseId.Value);
        }

        private void ValidateClosureBlock(string assetOwner)
        {
            if (closure == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing closure block. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            if (closure.resultKind == PhaseClosureResultKind.Unknown)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing closure.resultKind. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            if (closure.continuityPolicyKind == PhaseContinuityPolicyKind.Unknown)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing closure.continuityPolicyKind. asset='{assetOwner}', phaseId='{PhaseId}'.");
            }

            ValidateParameters(closure.parameters, assetOwner, "closure", PhaseId.Value);
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

        private static void ValidateParameters(
            List<PhaseParameterEntry> parameters,
            string assetOwner,
            string blockName,
            string entryId)
        {
            if (parameters == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Missing parameter list for {blockName}. asset='{assetOwner}', entryId='{entryId}'.");
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                PhaseParameterEntry parameter = parameters[i];
                if (parameter == null)
                {
                    throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Null parameter in {blockName}. asset='{assetOwner}', entryId='{entryId}', index={i}.");
                }

                parameter.key = Normalize(parameter.key);
                parameter.value = Normalize(parameter.value);

                if (string.IsNullOrWhiteSpace(parameter.key))
                {
                    throw new InvalidOperationException($"[FATAL][Config][PhaseDefinition] Empty parameter key in {blockName}. asset='{assetOwner}', entryId='{entryId}', index={i}.");
                }
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
