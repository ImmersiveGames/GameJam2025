using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "ImmersiveGames/ResourceConfig")]
    public class ResourceConfigSo : ScriptableObject
    {
        [SerializeField] private string uniqueId; // Identificador único do recurso (ex.: Health)
        [SerializeField] private string resourceName = "Recurso"; // Nome de exibição (ex.: Vida)
        [SerializeField] private ResourceType resourceType = ResourceType.Custom; // Tipo do recurso
        [SerializeField] private float maxValue = 100f; // Valor máximo do recurso
        [SerializeField] private float initialValue = 100f; // Valor inicial do recurso
        [SerializeField] private List<float> thresholds = new() { 0.75f, 0.5f, 0.25f }; // Limiares de porcentagem
        [SerializeField] private Sprite resourceIcon; // Ícone do recurso
        [SerializeField] private bool autoFillEnabled; // Habilita auto-preenchimento
        [SerializeField] private bool autoDrainEnabled; // Habilita auto-drenagem
        [SerializeField] private float autoFillRate; // Taxa de preenchimento (unidades/s)
        [SerializeField] private float autoDrainRate; // Taxa de drenagem (unidades/s)
        [SerializeField] private float autoChangeDelay = 1f; // Atraso antes de iniciar mudanças automáticas

        public string UniqueId => uniqueId;
        public string ResourceName => resourceName;
        public ResourceType ResourceType => resourceType;
        public float MaxValue => maxValue;
        public float InitialValue => initialValue;
        public List<float> Thresholds => thresholds;
        public Sprite ResourceIcon => resourceIcon;
        public bool AutoFillEnabled => autoFillEnabled;
        public bool AutoDrainEnabled
        {
            get => autoDrainEnabled;
            set => autoDrainEnabled = value;
        }
        public float AutoFillRate => autoFillRate;
        public float AutoDrainRate
        {
            get => autoDrainRate;
            set => autoDrainRate = value;
        }
        public float AutoChangeDelay => autoChangeDelay;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                Debug.LogError($"ResourceConfigSo '{name}': uniqueId está vazio! Defina um identificador único (ex.: 'Health').", this);
            }
            else if (uniqueId.Contains("_"))
            {
                Debug.LogError($"ResourceConfigSo '{name}': uniqueId='{uniqueId}' contém '_', o que pode indicar um prefixo inválido. Use apenas o ID base (ex.: 'Health', não 'Player1_Health').", this);
            }
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError($"ResourceConfigSo '{name}': resourceName está vazio! Defina um nome de exibição (ex.: 'Vida').", this);
            }
            if (maxValue <= 0)
            {
                Debug.LogError($"ResourceConfigSo '{name}': maxValue deve ser maior que 0.", this);
            }
            if (initialValue < 0 || initialValue > maxValue)
            {
                Debug.LogError($"ResourceConfigSo '{name}': initialValue deve estar entre 0 e maxValue.", this);
            }
        }
#endif
    }

    public enum ResourceType { Health, Mana, Energy, Stamina, Custom }
}