using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Configurable
{
    public class GroupedMaterialSkin : SkinConfigurable
    {
        [Header("Material Groups")]
        [SerializeField] private MaterialSlot[] materialSlots;

        [Header("Application Settings")]
        [SerializeField] private bool applyOnSkinChange = true;
        [SerializeField] private bool applyOnStart = true;
        [SerializeField] private bool randomizeOnNewInstances = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs;

        private Dictionary<MaterialGroupConfig, List<MaterialSlot>> _groupedSlots;

        #region Unity Lifecycle
        protected override void Start()
        {
            base.Start();
            InitializeGroups();
            
            if (applyOnStart)
            {
                ApplyGroupedMaterials();
            }
            
            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Initialized with {materialSlots.Length} slots");
        }
        #endregion

        #region SkinConfigurable Implementation

        protected override void ConfigureSkin(ISkinConfig skinConfig)
        {
            if (!applyOnSkinChange) return;
            
            ApplyGroupedMaterials();
            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Configured from skin");
        }

        protected override void ApplyDynamicModifications()
        {
            ApplyGroupedMaterials();
            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Applied dynamic modifications");
        }

        protected override void OnSkinInstancesCreated(ModelType modelType, List<GameObject> instances)
        {
            if (modelType != targetModelType) return;
            
            if (randomizeOnNewInstances)
            {
                ApplyGroupedMaterials();
            }
        }
        #endregion

        #region Group Management
        private void InitializeGroups()
        {
            _groupedSlots = new Dictionary<MaterialGroupConfig, List<MaterialSlot>>();

            foreach (var slot in materialSlots)
            {
                if (slot == null || !slot.IsValid) continue;

                slot.Initialize();

                var group = slot.MaterialGroup;
                if (!_groupedSlots.ContainsKey(group))
                {
                    _groupedSlots[group] = new List<MaterialSlot>();
                }
                _groupedSlots[group].Add(slot);
            }

            if (showDebugLogs)
            {
                DebugUtility.LogVerbose<GroupedMaterialSkin>($"Organized into {_groupedSlots.Count} groups:");
                foreach (KeyValuePair<MaterialGroupConfig, List<MaterialSlot>> group in _groupedSlots)
                {
                    DebugUtility.LogVerbose<GroupedMaterialSkin>($"  - {group.Key.GroupName}: {group.Value.Count} slots");
                }
            }
        }

        [ContextMenu("Apply Grouped Materials")]
        public void ApplyGroupedMaterials()
        {
            if (_groupedSlots == null) InitializeGroups();

            // CADA SLOT SORTEIA SEU PRÓPRIO MATERIAL ALEATÓRIO
            foreach (var slot in materialSlots)
            {
                if (slot is { IsValid: true } && slot.CanApply())
                {
                    slot.ApplyRandomMaterial();
                }
            }
            
            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Applied random materials to {materialSlots.Length} slots");
        }

        /// <summary>
        /// Aplica um material específico a um grupo (todos os slots do grupo recebem o MESMO material)
        /// </summary>
        private void ApplyMaterialToGroup(MaterialGroupConfig group, Material material)
        {
            if (_groupedSlots == null || !_groupedSlots.TryGetValue(group, out List<MaterialSlot> groupedSlot)) return;

            foreach (var slot in groupedSlot.Where(slot => slot != null && slot.CanApply()))
            {
                slot.ApplyMaterial(material);
            }

            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Applied specific material '{material.name}' to group '{group.GroupName}'");
        }

        /// <summary>
        /// Randomiza os materiais de um grupo específico - CADA SLOT SORTEIA INDEPENDENTEMENTE
        /// </summary>
        private void RandomizeGroup(MaterialGroupConfig group)
        {
            if (_groupedSlots == null || !_groupedSlots.TryGetValue(group, out List<MaterialSlot> groupedSlot)) return;

            foreach (var slot in groupedSlot.Where(slot => slot != null && slot.CanApply()))
            {
                slot.ApplyRandomMaterial();
            }

            if (showDebugLogs) 
            {
                string[] materialsUsed = _groupedSlots[group]
                    .Select(s => s.CurrentAppliedMaterial?.name ?? "None")
                    .Distinct()
                    .ToArray();
                
                DebugUtility.LogVerbose<GroupedMaterialSkin>($"Randomized group '{group.GroupName}' - {materialsUsed.Length} different materials used: {string.Join(", ", materialsUsed)}");
            }
        }

        /// <summary>
        /// Randomiza todos os grupos - CADA SLOT SORTEIA INDEPENDENTEMENTE
        /// </summary>
        [ContextMenu("Randomize All Groups")]
        public void RandomizeAllGroups()
        {
            if (_groupedSlots == null) return;

            foreach (var group in _groupedSlots.Keys)
            {
                RandomizeGroup(group);
            }

            if (showDebugLogs) 
            {
                int totalMaterialsUsed = materialSlots
                    .Where(s => s != null && s.CurrentAppliedMaterial != null)
                    .Select(s => s.CurrentAppliedMaterial.name)
                    .Distinct()
                    .Count();
                
                DebugUtility.LogVerbose<GroupedMaterialSkin>($"Randomized all {_groupedSlots.Count} groups - {totalMaterialsUsed} different materials used across all slots");
            }
        }

        /// <summary>
        /// Aplica uma progressão de materiais baseada em índice - TODOS OS SLOTS DO GRUPO RECEBEM O MESMO MATERIAL
        /// </summary>
        public void ApplyMaterialProgression(int materialIndex)
        {
            if (_groupedSlots == null) return;

            foreach (KeyValuePair<MaterialGroupConfig, List<MaterialSlot>> group in _groupedSlots)
            {
                var material = group.Key.GetMaterialByIndex(materialIndex);
                if (material != null)
                {
                    ApplyMaterialToGroup(group.Key, material);
                }
            }

            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Applied material progression index {materialIndex}");
        }

        /// <summary>
        /// Reseta todos os slots para seus materiais originais
        /// </summary>
        [ContextMenu("Reset All Materials")]
        public void ResetAllMaterials()
        {
            foreach (var slot in materialSlots)
            {
                if (slot != null)
                {
                    slot.ResetToOriginal();
                }
            }

            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Reset all materials to original");
        }

        /// <summary>
        /// Reseta um grupo específico para os materiais originais
        /// </summary>
        public void ResetGroup(MaterialGroupConfig group)
        {
            if (_groupedSlots == null || !_groupedSlots.TryGetValue(group, out List<MaterialSlot> groupedSlot)) return;

            foreach (var slot in groupedSlot.Where(slot => slot != null))
            {
                slot.ResetToOriginal();
            }

            if (showDebugLogs) DebugUtility.LogVerbose<GroupedMaterialSkin>($"Reset group '{group.GroupName}' to original");
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Obtém todos os grupos únicos
        /// </summary>
        private MaterialGroupConfig[] GetUniqueGroups()
        {
            if (_groupedSlots == null) InitializeGroups();
            return _groupedSlots?.Keys.ToArray() ?? Array.Empty<MaterialGroupConfig>();
        }

        /// <summary>
        /// Obtém os slots de um grupo específico
        /// </summary>
        private MaterialSlot[] GetSlotsForGroup(MaterialGroupConfig group)
        {
            if (_groupedSlots == null || !_groupedSlots.TryGetValue(group, out List<MaterialSlot> groupedSlot)) 
                return Array.Empty<MaterialSlot>();
            
            return groupedSlot.ToArray();
        }

        /// <summary>
        /// Obtém o estado atual dos grupos
        /// </summary>
        public GroupedMaterialState GetGroupedMaterialState()
        {
            MaterialGroupConfig[] groups = GetUniqueGroups();
            var groupStates = new List<GroupState>();

            foreach (var group in groups)
            {
                MaterialSlot[] slots = GetSlotsForGroup(group);
                string[] uniqueMaterials = slots
                    .Where(s => s.CurrentAppliedMaterial != null)
                    .Select(s => s.CurrentAppliedMaterial.name)
                    .Distinct()
                    .ToArray();
                
                groupStates.Add(new GroupState
                {
                    group = group,
                    slotCount = slots.Length,
                    uniqueMaterialsCount = uniqueMaterials.Length,
                    uniqueMaterials = uniqueMaterials
                });
            }

            return new GroupedMaterialState
            {
                totalGroups = groups.Length,
                totalSlots = materialSlots.Length,
                validSlots = materialSlots.Count(s => s is { IsValid: true }),
                groupStates = groupStates.ToArray()
            };
        }

        /// <summary>
        /// Obtém informações detalhadas sobre todos os slots
        /// </summary>
        private SlotInfo[] GetAllSlotInfos()
        {
            return materialSlots.Select(slot => slot?.GetSlotInfo() ?? new SlotInfo()).ToArray();
        }
        #endregion

        #region Editor Helpers
        #if UNITY_EDITOR
        [ContextMenu("Validate Material Slots")]
        private void EditorValidateSlots()
        {
            int validSlots = 0;
            int invalidSlots = 0;

            foreach (var slot in materialSlots)
            {
                if (slot is { IsValid: true })
                {
                    validSlots++;
                }
                else
                {
                    invalidSlots++;
                }
            }

            DebugUtility.LogVerbose<GroupedMaterialSkin>($"Validation: {validSlots} valid, {invalidSlots} invalid slots");
        }

        [ContextMenu("Log Detailed Slot Info")]
        private void EditorLogDetailedInfo()
        {
            SlotInfo[] slotInfos = GetAllSlotInfos();
            DebugUtility.LogVerbose<GroupedMaterialSkin>($"Detailed Slot Info ({slotInfos.Length} slots):");
            
            foreach (var info in slotInfos)
            {
                DebugUtility.LogVerbose<GroupedMaterialSkin>($"  - {info}");
            }
        }

        [ContextMenu("Test Randomization - Show Material Diversity")]
        private void EditorTestRandomization()
        {
            RandomizeAllGroups();
            var state = GetGroupedMaterialState();
            
            DebugUtility.LogVerbose<GroupedMaterialSkin>($"Randomization Test:");
            DebugUtility.LogVerbose<GroupedMaterialSkin>($"Total slots: {state.totalSlots}, Valid: {state.validSlots}");
            
            foreach (var groupState in state.groupStates)
            {
                DebugUtility.LogVerbose<GroupedMaterialSkin>($"  - {groupState.group.GroupName}: {groupState.slotCount} slots, {groupState.uniqueMaterialsCount} unique materials");
                foreach (string material in groupState.uniqueMaterials)
                {
                    DebugUtility.LogVerbose<GroupedMaterialSkin>($"    * {material}");
                }
            }
        }
        #endif
        #endregion
    }

    [Serializable]
    public struct GroupedMaterialState
    {
        public int totalGroups;
        public int totalSlots;
        public int validSlots;
        public GroupState[] groupStates;

        public override string ToString()
        {
            int uniqueMaterials = groupStates.Sum(gs => gs.uniqueMaterialsCount);
            return $"Groups: {totalGroups}, Slots: {validSlots}/{totalSlots}, Unique Materials: {uniqueMaterials}";
        }
    }

    [Serializable]
    public struct GroupState
    {
        public MaterialGroupConfig group;
        public int slotCount;
        public int uniqueMaterialsCount;
        public string[] uniqueMaterials;

        public override string ToString()
        {
            return $"{group.GroupName}: {slotCount} slots, {uniqueMaterialsCount} unique materials";
        }
    }
}