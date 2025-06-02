using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    // Configuração de recursos via ScriptableObject
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "ImmersiveGames/ResourceConfig")]
    public class ResourceConfigSo : ScriptableObject
    {
        [SerializeField] private string resourceName = "Recurso"; // Nome do recurso
        [SerializeField] private ResourceType resourceType = ResourceType.Custom; // Tipo do recurso
        [SerializeField] private float maxValue = 100f; // Valor máximo do recurso
        [SerializeField] private float initialValue = 100f; // Valor inicial do recurso
        [SerializeField] private List<float> thresholds = new()
            { 0.75f, 0.5f, 0.25f }; // Limiares de porcentagem
        [SerializeField] private Sprite resourceIcon; // Ícone do recurso
        [SerializeField] private bool autoFillEnabled; // Habilita auto-preenchimento
        [SerializeField] private bool autoDrainEnabled; // Habilita auto-drenagem
        [SerializeField] private float autoFillRate; // Taxa de preenchimento (unidades/s)
        [SerializeField] private float autoDrainRate; // Taxa de drenagem (unidades/s)
        [SerializeField] private float autoChangeDelay = 1f; // Atraso antes de iniciar mudanças automáticas

        public string ResourceName => resourceName;
        public ResourceType ResourceType => resourceType;
        public float MaxValue => maxValue;
        public float InitialValue => initialValue;
        public List<float> Thresholds => thresholds;
        public Sprite ResourceIcon => resourceIcon;
        public bool AutoFillEnabled => autoFillEnabled;
        public bool AutoDrainEnabled => autoDrainEnabled;
        public float AutoFillRate => autoFillRate;
        public float AutoDrainRate => autoDrainRate;
        public float AutoChangeDelay => autoChangeDelay;
    }

    // Tipos de recursos disponíveis
    public enum ResourceType { Health, Mana, Energy, Stamina, Custom }
}