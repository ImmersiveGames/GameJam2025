using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    // Exibe o ícone do recurso do planeta associado na UI
    public class PlanetResourceUI : MonoBehaviour
    {
        [SerializeField] private Image resourceIcon; // Imagem para exibir o ícone do recurso
        [SerializeField] private Text planetNameText; // (Opcional) Texto para exibir o nome do planeta
        private Planets _planet; // Referência ao componente Planets do planeta pai
        private EventBinding<PlanetCreatedEvent> _planetCreatedBinding;

        // Inicializa o componente
        private void Awake()
        {
            // Obtém o componente Planets na hierarquia pai
            _planet = GetComponentInParent<Planets>();
            if (_planet == null)
            {
                Debug.LogError($"Componente Planets não encontrado na hierarquia pai de {gameObject.name}!", gameObject);
            }
        }

        // Registra eventos e tenta atualizar a UI
        private void OnEnable()
        {
            _planetCreatedBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreatedBinding);

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
        }

        // Atualiza a UI quando o planeta correspondente é criado
        private void OnPlanetCreated(PlanetCreatedEvent evt)
        {
            // Processa apenas o evento do planeta associado
            if (_planet == null || evt.PlanetObject != _planet.gameObject)
            {
                return;
            }

            UpdateUIWithResources(evt.Resources, evt.PlanetId);
        }

        // Atualiza a UI com o recurso atual do planeta
        private void UpdateUI()
        {
            if (_planet == null)
            {
                ClearUI();
                return;
            }

            var resources = _planet.GetResources();
            if (resources != null)
            {
                UpdateUIWithResources(resources, _planet.GetInstanceID()); // Usa InstanceID como fallback
            }
            else
            {
                ClearUI();
                Debug.Log($"Recurso ainda não definido para o planeta {_planet.gameObject.name}. Aguardando PlanetCreatedEvent.", gameObject);
            }
        }

        // Atualiza a UI com o recurso e ID do planeta
        private void UpdateUIWithResources(PlanetResourcesSo resources, int planetId)
        {
            if (resourceIcon == null)
            {
                Debug.LogWarning("Image para ícone de recurso não configurada em PlanetResourceUI!", gameObject);
                ClearUI();
                return;
            }

            if (resources?.ResourceIcon != null)
            {
                resourceIcon.sprite = resources.ResourceIcon;
                resourceIcon.gameObject.SetActive(true);
                if (planetNameText != null)
                {
                    planetNameText.text = $"Planet {planetId}";
                }
                Debug.Log($"Ícone do recurso {resources.ResourceType} atualizado para o planeta {_planet.gameObject.name} (ID: {planetId}).", gameObject);
            }
            else
            {
                ClearUI();
                Debug.LogWarning($"Nenhum ícone de recurso encontrado para o planeta {_planet.gameObject.name} (ID: {planetId}).", gameObject);
            }
        }

        // Limpa a UI (desativa ícone e texto)
        private void ClearUI()
        {
            if (resourceIcon != null)
            {
                resourceIcon.gameObject.SetActive(false);
            }
            if (planetNameText != null)
            {
                planetNameText.text = "";
            }
        }
    }
}