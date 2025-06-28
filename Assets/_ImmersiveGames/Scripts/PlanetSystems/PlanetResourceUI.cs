using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class PlanetResourceUI : MonoBehaviour
    {
        [SerializeField] private Transform resourceCanvas; // Imagem para exibir o ícone do recurso
        [SerializeField] private Image resourceIcon; // Imagem para exibir o ícone do recurso
        [SerializeField] private Text planetNameText; // (Opcional) Texto para exibir o nome do planeta
        private PlanetsMaster _planetMaster; // Referência ao componente PlanetsMaster do planeta pai
        private EventBinding<PlanetCreatedEvent> _planetCreatedBinding;
        private EventBinding<PlanetConsumedEvent> _planetDestroyBinding;

        // Inicializa o componente
        private void Awake()
        {
            // Obtém o componente PlanetsMaster na hierarquia pai
            _planetMaster = GetComponentInParent<PlanetsMaster>();
            if (!_planetMaster)
            {
                DebugUtility.LogError<PlanetResourceUI>($"Componente PlanetsMaster não encontrado na hierarquia pai de {gameObject.name}!", gameObject);
            }
        }

        // Registra eventos e tenta atualizar a UI
        private void OnEnable()
        {
            _planetCreatedBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreatedBinding);
            _planetDestroyBinding = new EventBinding<PlanetConsumedEvent>(OnPlanetDestroyed);
            EventBus<PlanetConsumedEvent>.Register(_planetDestroyBinding);
        }

        private void Start()
        {
            // Tenta atualizar a UI, caso o recurso já esteja definido
            UpdateUI();
        }

        // Desregistra eventos
        private void OnDisable()
        {
            if (_planetCreatedBinding != null)
            {
                EventBus<PlanetCreatedEvent>.Unregister(_planetCreatedBinding);
            }
            if(_planetDestroyBinding != null)
            {
                EventBus<PlanetConsumedEvent>.Unregister(_planetDestroyBinding);
            }
        }

        // Atualiza a UI quando o planeta correspondente é criado
        private void OnPlanetCreated(PlanetCreatedEvent evt)
        {
            // Processa apenas o evento do planeta associado
            if(evt.Detected.GetPlanetsMaster() != _planetMaster)
                return; 
            var planetInfo = evt.Detected.GetPlanetsMaster().GetPlanetInfo();
            UpdateUIWithResources(planetInfo.Resources, planetInfo.ID);
        }
        
        private void OnPlanetDestroyed(PlanetConsumedEvent evt)
        {
            if(evt.Detected.GetPlanetsMaster() != _planetMaster) return;
            ClearUI();
            resourceCanvas.gameObject.SetActive(false);
        }

        // Atualiza a UI com o recurso atual do planeta
        private void UpdateUI()
        {
            if (!_planetMaster)
            {
                ClearUI();
                return;
            }

            var resources = _planetMaster.GetResource();
            if (resources)
            {
                UpdateUIWithResources(resources, _planetMaster.GetInstanceID()); // Usa InstanceID como fallback
            }
            else
            {
                ClearUI();
                DebugUtility.LogVerbose<PlanetResourceUI>($"Recurso ainda não definido para o planeta {_planetMaster.gameObject.name}. Aguardando PlanetCreatedEvent.");
            }
        }

        // Atualiza a UI com o recurso e ID do planeta
        private void UpdateUIWithResources(PlanetResourcesSo resources, int planetId)
        {
            if (!resourceIcon)
            {
                DebugUtility.LogWarning<PlanetResourceUI>("Image para ícone de recurso não configurada em PlanetResourceUI!", gameObject);
                ClearUI();
                return;
            }

            if (resources?.ResourceIcon)
            {
                resourceIcon.sprite = resources.ResourceIcon;
                resourceIcon.gameObject.SetActive(true);
                if (planetNameText)
                {
                    planetNameText.text = $"Detected {planetId}";
                }
                DebugUtility.LogVerbose<PlanetResourceUI>($"Ícone do recurso {resources.ResourceType} atualizado para o planeta {_planetMaster.gameObject.name} (ID: {planetId}).");
            }
            else
            {
                ClearUI();
                DebugUtility.LogWarning<PlanetResourceUI>($"Nenhum ícone de recurso encontrado para o planeta {_planetMaster.gameObject.name} (ID: {planetId}).", gameObject);
            }
        }

        // Limpa a UI (desativa ícone e texto)
        private void ClearUI()
        {
            if (resourceCanvas)
            {
                resourceCanvas.gameObject.SetActive(true);
            }
            if (resourceIcon)
            {
                resourceIcon.gameObject.SetActive(false);
            }
            if (planetNameText)
            {
                planetNameText.text = "";
            }
        }
    }
}