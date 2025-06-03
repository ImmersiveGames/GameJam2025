using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterDesireUI : MonoBehaviour
    {
        [SerializeField] private Image desireIcon;
        [SerializeField] private EaterHunger eaterHunger;
        private EventBinding<EaterDesireChangedEvent> _desireChangedBinding;

        private void Awake()
        {
            if (eaterHunger) return;
            DebugUtility.LogError<EaterDesireUI>("EaterHunger não encontrado na cena!", this);
            enabled = false;
        }

        private void OnEnable()
        {
            _desireChangedBinding = new EventBinding<EaterDesireChangedEvent>(OnDesireChanged);
            EventBus<EaterDesireChangedEvent>.Register(_desireChangedBinding);
            UpdateUI();
        }

        private void OnDisable()
        {
            EventBus<EaterDesireChangedEvent>.Unregister(_desireChangedBinding);
        }

        private void OnDesireChanged(EaterDesireChangedEvent evt)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (!desireIcon)
            {
                DebugUtility.LogWarning<EaterDesireUI>("Image para ícone de desejo não configurada!", this);
                return;
            }

            var desiredResource = eaterHunger.GetDesiredResource();
            if (desiredResource && desiredResource.ResourceIcon)
            {
                desireIcon.sprite = desiredResource.ResourceIcon;
                desireIcon.gameObject.SetActive(true);
                DebugUtility.Log<EaterDesireUI>($"Ícone do desejo do Eater atualizado: {desiredResource.name}.");
            }
            else
            {
                desireIcon.gameObject.SetActive(false);
                DebugUtility.Log<EaterDesireUI>("Nenhum desejo ativo para o Eater. Ícone desativado.");
            }
        }
    }
}