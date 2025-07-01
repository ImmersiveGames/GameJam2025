using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Logs), DefaultExecutionOrder(10)]
    public class EaterDesireUI : MonoBehaviour
    {
        [SerializeField] private Image desireIcon;
         private EaterDesire _eaterDesire; // Alterado para EaterDesire
        private EventBinding<DesireChangedEvent> _desireChangedBinding;

        private void OnEnable()
        {
            _eaterDesire = FindFirstObjectByType<EaterDesire>();
            if (!_eaterDesire)
            {
                DebugUtility.LogError<EaterDesireUI>("EaterDesire não encontrado na cena!", this);
                enabled = false;
                return;
            }
            _desireChangedBinding = new EventBinding<DesireChangedEvent>(OnDesireChanged);
            EventBus<DesireChangedEvent>.Register(_desireChangedBinding);
            UpdateUI();
        }

        private void OnDisable()
        {
            EventBus<DesireChangedEvent>.Unregister(_desireChangedBinding);
        }

        private void OnDesireChanged()
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

            var desiredResource = _eaterDesire.GetDesiredResource();
            if (desiredResource && desiredResource.ResourceIcon)
            {
                desireIcon.sprite = desiredResource.ResourceIcon;
                desireIcon.gameObject.SetActive(true);
                DebugUtility.LogVerbose<EaterDesireUI>($"Ícone do desejo do Eater atualizado: {desiredResource.name}.");
            }
            else
            {
                desireIcon.gameObject.SetActive(false);
                DebugUtility.LogVerbose<EaterDesireUI>("Nenhum desejo ativo para o Eater. Ícone desativado.");
            }
        }
    }
}