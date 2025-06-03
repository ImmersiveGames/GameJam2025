using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterDesireUI : MonoBehaviour
    {
        [SerializeField] private Image desireIcon;
        [SerializeField] private EaterHunger eaterHunger;
        private EventBinding<EaterDesireChangedEvent> desireChangedBinding;

        private void Awake()
        {
            if (eaterHunger == null)
            {
                Debug.LogError("EaterHunger não encontrado na cena!", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            desireChangedBinding = new EventBinding<EaterDesireChangedEvent>(OnDesireChanged);
            EventBus<EaterDesireChangedEvent>.Register(desireChangedBinding);
            UpdateUI();
        }

        private void OnDisable()
        {
            EventBus<EaterDesireChangedEvent>.Unregister(desireChangedBinding);
        }

        private void OnDesireChanged(EaterDesireChangedEvent evt)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (desireIcon == null)
            {
                Debug.LogWarning("Image para ícone de desejo não configurada!", this);
                return;
            }

            PlanetResourcesSo desiredResource = eaterHunger.GetDesiredResource();
            if (desiredResource != null && desiredResource.ResourceIcon != null)
            {
                desireIcon.sprite = desiredResource.ResourceIcon;
                desireIcon.gameObject.SetActive(true);
                Debug.Log($"Ícone do desejo do Eater atualizado: {desiredResource.name}.");
            }
            else
            {
                desireIcon.gameObject.SetActive(false);
                Debug.Log("Nenhum desejo ativo para o Eater. Ícone desativado.");
            }
        }
    }
}