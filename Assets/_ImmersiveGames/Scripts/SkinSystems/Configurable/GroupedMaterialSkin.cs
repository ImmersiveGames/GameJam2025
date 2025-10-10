using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.SkinSystems.Data;
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
        [SerializeField] private bool showDebugLogs = false;

        private Dictionary<MaterialGroupConfig, List<MaterialSlot>> _groupedSlots;
        private bool _isInitialized = false;

        #region Unity Lifecycle
        protected override void Start()
        {
            base.Start();
            InitializeGroups();
            
            if (applyOnStart)
            {
                ApplyGroupedMaterials();
            }
            
            _isInitialized = true;
            if (showDebugLogs) Debug.Log($"[GroupedMaterialSkin] Initialized with {materialSlots.Length} slots");
        }
        #endregion

        #region SkinConfigurable Implementation
        public override void ConfigureSkin(ISkinConfig skinConfig)
        {
            if (!applyOnSkinChange) return;
            
            ApplyGroupedMaterials();
            if (showDebugLogs) Debug.Log($"[GroupedMaterialSkin] Configured from skin");
        }

        public override void ApplyDynamicModifications()
        {
            ApplyGroupedMaterials();
            if (showDebugLogs) Debug.Log("[GroupedMaterialSkin] Applied dynamic modifications");
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
                Debug.Log($"[GroupedMaterialSkin] Organized into {_groupedSlots.Count} groups:");
                foreach (var group in _groupedSlots)
                {
                    Debug.Log($"  - {group.Key.GroupName}: {group.Value.Count} slots");
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
                if (slot != null && slot.IsValid && slot.CanApply())
                {
                    slot.ApplyRandomMaterial();
                }
            }
            
            if (showDebugLogs) Debug.Log($"[GroupedMaterialSkin] Applied random materials to {materialSlots.Length} slots");
        }

        /// <summary>
        /// Aplica um material específico a um grupo (todos os slots do grupo recebem o MESMO material)
        /// </summary>
        public void ApplyMaterialToGroup(MaterialGroupConfig group, Material material)
        {
            if (_groupedSlots == null || !_groupedSlots.ContainsKey(group)) return;

            foreach (var slot in _groupedSlots[group])
            {
                if (slot != null && slot.CanApply())
                {
                    slot.ApplyMaterial(material);
                }
            }

            if (showDebugLogs) Debug.Log($"[GroupedMaterialSkin] Applied specific material '{material.name}' to group '{group.GroupName}'");
        }

        /// <summary>
        /// Randomiza os materiais de um grupo específico - CADA SLOT SORTEIA INDEPENDENTEMENTE
        /// </summary>
        public void RandomizeGroup(MaterialGroupConfig group)
        {
            if (_groupedSlots == null || !_groupedSlots.ContainsKey(group)) return;

            foreach (var slot in _groupedSlots[group])
            {
                if (slot != null && slot.CanApply())
                {
                    slot.ApplyRandomMaterial();
                }
            }

            if (showDebugLogs) 
            {
                var materialsUsed = _groupedSlots[group]
                    .Select(s => s.CurrentAppliedMaterial?.name ?? "None")
                    .Distinct()
                    .ToArray();
                
                Debug.Log($"[GroupedMaterialSkin] Randomized group '{group.GroupName}' - {materialsUsed.Length} different materials used: {string.Join(", ", materialsUsed)}");
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
                var totalMaterialsUsed = materialSlots
                    .Where(s => s != null && s.CurrentAppliedMaterial != null)
                    .Select(s => s.CurrentAppliedMaterial.name)
                    .Distinct()
                    .Count();
                
                Debug.Log($"[GroupedMaterialSkin] Randomized all {_groupedSlots.Count} groups - {totalMaterialsUsed} different materials used across all slots");
            }
        }

        /// <summary>
        /// Aplica uma progressão de materiais baseada em índice - TODOS OS SLOTS DO GRUPO RECEBEM O MESMO MATERIAL
        /// </summary>
        public void ApplyMaterialProgression(int materialIndex)
        {
            if (_groupedSlots == null) return;

            foreach (var group in _groupedSlots)
            {
                var material = group.Key.GetMaterialByIndex(materialIndex);
                if (material != null)
                {
                    ApplyMaterialToGroup(group.Key, material);
                }
            }

            if (showDebugLogs) Debug.Log($"[GroupedMaterialSkin] Applied material progression index {materialIndex}");
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

            if (showDebugLogs) Debug.Log("[GroupedMaterialSkin] Reset all materials to original");
        }

        /// <summary>
        /// Reseta um grupo específico para os materiais originais
        /// </summary>
        public void ResetGroup(MaterialGroupConfig group)
        {
            if (_groupedSlots == null || !_groupedSlots.ContainsKey(group)) return;

            foreach (var slot in _groupedSlots[group])
            {
                if (slot != null)
                {
                    slot.ResetToOriginal();
                }
            }

            if (showDebugLogs) Debug.Log($"[GroupedMaterialSkin] Reset group '{group.GroupName}' to original");
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Obtém todos os grupos únicos
        /// </summary>
        public MaterialGroupConfig[] GetUniqueGroups()
        {
            if (_groupedSlots == null) InitializeGroups();
            return _groupedSlots?.Keys.ToArray() ?? new MaterialGroupConfig[0];
        }

        /// <summary>
        /// Obtém os slots de um grupo específico
        /// </summary>
        public MaterialSlot[] GetSlotsForGroup(MaterialGroupConfig group)
        {
            if (_groupedSlots == null || !_groupedSlots.ContainsKey(group)) 
                return new MaterialSlot[0];
            
            return _groupedSlots[group].ToArray();
        }

        /// <summary>
        /// Obtém o estado atual dos grupos
        /// </summary>
        public GroupedMaterialState GetGroupedMaterialState()
        {
            var groups = GetUniqueGroups();
            var groupStates = new List<GroupState>();

            foreach (var group in groups)
            {
                var slots = GetSlotsForGroup(group);
                var uniqueMaterials = slots
                    .Where(s => s.CurrentAppliedMaterial != null)
                    .Select(s => s.CurrentAppliedMaterial.name)
                    .Distinct()
                    .ToArray();
                
                groupStates.Add(new GroupState
                {
                    Group = group,
                    SlotCount = slots.Length,
                    UniqueMaterialsCount = uniqueMaterials.Length,
                    UniqueMaterials = uniqueMaterials
                });
            }

            return new GroupedMaterialState
            {
                TotalGroups = groups.Length,
                TotalSlots = materialSlots.Length,
                ValidSlots = materialSlots.Count(s => s != null && s.IsValid),
                GroupStates = groupStates.ToArray()
            };
        }

        /// <summary>
        /// Obtém informações detalhadas sobre todos os slots
        /// </summary>
        public SlotInfo[] GetAllSlotInfos()
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
                if (slot != null && slot.IsValid)
                {
                    validSlots++;
                }
                else
                {
                    invalidSlots++;
                }
            }

            Debug.Log($"[GroupedMaterialSkin] Validation: {validSlots} valid, {invalidSlots} invalid slots");
        }

        [ContextMenu("Log Detailed Slot Info")]
        private void EditorLogDetailedInfo()
        {
            var slotInfos = GetAllSlotInfos();
            Debug.Log($"[GroupedMaterialSkin] Detailed Slot Info ({slotInfos.Length} slots):");
            
            foreach (var info in slotInfos)
            {
                Debug.Log($"  - {info}");
            }
        }

        [ContextMenu("Test Randomization - Show Material Diversity")]
        private void EditorTestRandomization()
        {
            RandomizeAllGroups();
            var state = GetGroupedMaterialState();
            
            Debug.Log($"[GroupedMaterialSkin] Randomization Test:");
            Debug.Log($"Total slots: {state.TotalSlots}, Valid: {state.ValidSlots}");
            
            foreach (var groupState in state.GroupStates)
            {
                Debug.Log($"  - {groupState.Group.GroupName}: {groupState.SlotCount} slots, {groupState.UniqueMaterialsCount} unique materials");
                foreach (var material in groupState.UniqueMaterials)
                {
                    Debug.Log($"    * {material}");
                }
            }
        }
        #endif
        #endregion
    }

    [System.Serializable]
    public struct GroupedMaterialState
    {
        public int TotalGroups;
        public int TotalSlots;
        public int ValidSlots;
        public GroupState[] GroupStates;

        public override string ToString()
        {
            var uniqueMaterials = GroupStates.Sum(gs => gs.UniqueMaterialsCount);
            return $"Groups: {TotalGroups}, Slots: {ValidSlots}/{TotalSlots}, Unique Materials: {uniqueMaterials}";
        }
    }

    [System.Serializable]
    public struct GroupState
    {
        public MaterialGroupConfig Group;
        public int SlotCount;
        public int UniqueMaterialsCount;
        public string[] UniqueMaterials;

        public override string ToString()
        {
            return $"{Group.GroupName}: {SlotCount} slots, {UniqueMaterialsCount} unique materials";
        }
    }
}