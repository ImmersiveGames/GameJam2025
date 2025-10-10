using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    [Serializable]
    public class MaterialSlot
    {
        [Header("Target References")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private int materialIndex = 0;

        [Header("Group Assignment")]
        [SerializeField] private MaterialGroupConfig materialGroup;

        // Cache dos materiais originais (um por renderer)
        private Material[][] _originalMaterials;
        private Material _currentAppliedMaterial;
        private bool _isInitialized = false;

        public Renderer[] TargetRenderers => targetRenderers;
        public int MaterialIndex => materialIndex;
        public MaterialGroupConfig MaterialGroup => materialGroup;
        public Material CurrentAppliedMaterial => _currentAppliedMaterial;
        
        public bool IsValid => targetRenderers != null && 
                              targetRenderers.Length > 0 && 
                              materialGroup != null;

        public void Initialize()
        {
            if (_isInitialized || targetRenderers == null) return;

            _originalMaterials = new Material[targetRenderers.Length][];

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                if (renderer != null)
                {
                    var materials = renderer.sharedMaterials;
                    _originalMaterials[i] = new Material[materials.Length];
                    
                    for (int j = 0; j < materials.Length; j++)
                    {
                        _originalMaterials[i][j] = materials[j];
                    }
                }
            }
            
            _isInitialized = true;
        }

        /// <summary>
        /// Aplica um material específico a este slot em todos os renderers
        /// </summary>
        public void ApplyMaterial(Material material)
        {
            if (!IsValid) return;

            _currentAppliedMaterial = material;

            foreach (var renderer in targetRenderers)
            {
                if (renderer == null) continue;

                var materials = renderer.materials;
                if (materialIndex >= 0 && materialIndex < materials.Length)
                {
                    materials[materialIndex] = material;
                    renderer.materials = materials;
                }
            }
        }

        /// <summary>
        /// Aplica um material aleatório do grupo - CADA SLOT SORTEIA INDEPENDENTEMENTE
        /// </summary>
        public void ApplyRandomMaterial()
        {
            if (!IsValid) return;
            
            // Cada slot sorteia seu próprio material aleatório
            var randomMaterial = materialGroup.GetRandomMaterial();
            if (randomMaterial != null)
            {
                ApplyMaterial(randomMaterial);
            }
        }

        /// <summary>
        /// Reseta para os materiais originais
        /// </summary>
        public void ResetToOriginal()
        {
            if (!IsValid || !_isInitialized) return;

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                var renderer = targetRenderers[i];
                if (renderer == null) continue;

                if (_originalMaterials[i] != null)
                {
                    renderer.materials = _originalMaterials[i];
                }
            }
            
            _currentAppliedMaterial = null;
        }

        /// <summary>
        /// Obtém o material atual do slot (do primeiro renderer)
        /// </summary>
        public Material GetCurrentMaterial()
        {
            if (!IsValid || targetRenderers.Length == 0) return null;

            var renderer = targetRenderers[0];
            if (renderer == null) return null;

            var materials = renderer.materials;
            if (materialIndex >= 0 && materialIndex < materials.Length)
            {
                return materials[materialIndex];
            }
            
            return null;
        }

        /// <summary>
        /// Verifica se este slot pode ser aplicado
        /// </summary>
        public bool CanApply()
        {
            if (!IsValid) return false;
            
            foreach (var renderer in targetRenderers)
            {
                if (renderer != null && materialIndex < renderer.sharedMaterials.Length)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Obtém informações sobre este slot
        /// </summary>
        public SlotInfo GetSlotInfo()
        {
            return new SlotInfo
            {
                RendererCount = targetRenderers?.Length ?? 0,
                MaterialIndex = materialIndex,
                GroupName = materialGroup?.GroupName ?? "None",
                CurrentMaterial = _currentAppliedMaterial?.name ?? "None",
                IsValid = IsValid,
                CanApply = CanApply()
            };
        }
    }

    [System.Serializable]
    public struct SlotInfo
    {
        public int RendererCount;
        public int MaterialIndex;
        public string GroupName;
        public string CurrentMaterial;
        public bool IsValid;
        public bool CanApply;

        public override string ToString()
        {
            return $"{GroupName} (Index: {MaterialIndex}, Renderers: {RendererCount}, Material: {CurrentMaterial})";
        }
    }
}