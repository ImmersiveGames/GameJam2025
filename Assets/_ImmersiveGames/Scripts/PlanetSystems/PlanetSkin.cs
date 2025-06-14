using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;
using Random = UnityEngine.Random;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetSkin : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] planetsParts;
        [SerializeField]
        private GameObject planetRing;
        [SerializeField]
        private Material[] planetColorMaterials;
        
        private MeshRenderer[] _planetMaterials;
        private PlanetsMaster _planetMaster; // Referência ao PlanetsMaster associado
        private PlanetHealth _planetHealth; // Referência ao PlanetHealth
        
        private EventBinding<PlanetCreatedEvent> _planetCreateBinding; // Binding para evento de desmarcação

        private void Awake()
        {
            _planetMaterials = GetComponentsInChildren<MeshRenderer>();
            _planetMaster = GetComponentInParent<PlanetsMaster>();
        }

        private void OnEnable()
        {
            _planetCreateBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreateBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetCreatedEvent>.Unregister(_planetCreateBinding);
            if (_planetHealth != null)
            {
                _planetHealth.onThresholdReached.RemoveListener(OnHealthThresholdReached);
            }
        }
        
        private void OnPlanetCreated(PlanetCreatedEvent obj)
        {
            if(obj.PlanetsMaster != _planetMaster)
                return;
            ChangeMaterialColor();
            planetRing.SetActive(Random.value < obj.PlanetsMaster.GetPlanetData().ringChance);
            
            _planetHealth = _planetMaster.GetComponent<PlanetHealth>();
            if (_planetHealth == null)
            {
                DebugUtility.LogWarning<PlanetSkin>($"PlanetHealth não encontrado em {_planetMaster.name}!");
                return;
            }
            _planetHealth.onThresholdReached.AddListener(OnHealthThresholdReached);
            DebugUtility.Log<PlanetSkin>($"Registrado onThresholdReached para planeta {_planetMaster.name}.");

            // Inicializa as partes com base na saúde atual
            UpdatePlanetParts(_planetHealth.GetPercentage());
        }
        
        private void OnHealthThresholdReached(float threshold)
        {
            DebugUtility.Log<PlanetSkin>($"Planeta {_planetMaster.name} atingiu limiar de saúde: {threshold * 100:F0}%");
            UpdatePlanetParts(threshold);
        }
        private void UpdatePlanetParts(float healthPercentage)
        {
            if (planetsParts is not { Length: 4 })
            {
                DebugUtility.LogWarning<PlanetSkin>($"planetsParts deve conter exatamente 4 GameObjects! Atualmente: {planetsParts?.Length ?? 0}");
                return;
            }

            // Mapeia o percentual de saúde para o número de partes ativas
            int activeParts = healthPercentage switch
            {
                > 0.75f => 4,
                > 0.5f => 3,
                > 0.25f => 2,
                > 0f => 1,
                _ => 0
            };

            // Ativa/desativa as partes
            for (int i = 0; i < planetsParts.Length; i++)
            {
                if (planetsParts[i] != null)
                {
                    planetsParts[i].SetActive(i < activeParts);
                }
                else
                {
                    DebugUtility.LogWarning<PlanetSkin>($"planetsParts[{i}] é nulo em {_planetMaster.name}!");
                }
            }

            DebugUtility.Log<PlanetSkin>($"Planeta {_planetMaster.name}: {activeParts} de 4 partes ativas (saúde: {healthPercentage * 100:F0}%).");
        }

        private void ChangeMaterialColor()
        {
            if (_planetMaterials == null || _planetMaterials.Length == 0)
                return;

            // Referência ao primeiro MeshRenderer válido
            var referenceRenderer = _planetMaterials[0];
            if (referenceRenderer == null)
                return;

            // Usa o número de materiais do primeiro renderer como padrão
            Material[] referenceMaterials = referenceRenderer.sharedMaterials;
            int materialCount = referenceMaterials.Length;

            if (materialCount == 0)
                return;

            // Gera a sequência de materiais aleatórios uma vez
            var newMaterials = new Material[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                newMaterials[i] = planetColorMaterials.Random<Material>();
            }

            // Aplica nos renderizadores compatíveis
            foreach (var meshRenderer in _planetMaterials)
            {
                if (meshRenderer != null && meshRenderer.sharedMaterials.Length == materialCount)
                {
                    meshRenderer.sharedMaterials = newMaterials;
                }
#if UNITY_EDITOR
                else
                {
                    DebugUtility.LogWarning<PlanetSkin>($"Renderer \"{meshRenderer?.name}\" skipped: material count mismatch.");
                }
#endif
            }
        }


    }
}