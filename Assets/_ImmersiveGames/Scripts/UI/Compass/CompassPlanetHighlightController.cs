using System.Collections.Generic;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.World.Compass;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Controla o destaque do planeta atualmente marcado, ampliando o ícone na bússola.
    /// Escuta o evento PlanetMarkingChangedEvent e aplica SetMarked nos ícones correspondentes.
    /// </summary>
    public class CompassPlanetHighlightController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("HUD da bússola responsável por instanciar os ícones.")]
        [SerializeField] private CompassHUD compassHUD;

        [Header("Highlight Settings")]
        [Tooltip("Multiplicador de escala aplicado ao ícone do planeta marcado.")]
        [SerializeField] private float markedScaleMultiplier = 1.3f;

        [Tooltip("Cor opcional para dar destaque ao planeta marcado. Se não definida, mantém a cor do ícone.")]
        [SerializeField] private Color markedColorTint = Color.white;

        [Tooltip("Se true, aplica o tint de cor ao ícone marcado; caso contrário, apenas escala.")]
        [SerializeField] private bool tintMarkedIcon;

        private EventBinding<PlanetMarkingChangedEvent> _planetMarkingBinding;
        private PlanetsMaster _markedPlanet;

        private void Awake()
        {
            if (compassHUD == null)
            {
                compassHUD = GetComponent<CompassHUD>();
            }
        }

        private void OnEnable()
        {
            _planetMarkingBinding ??= new EventBinding<PlanetMarkingChangedEvent>(HandlePlanetMarkingChanged);
            EventBus<PlanetMarkingChangedEvent>.Register(_planetMarkingBinding);
        }

        private void OnDisable()
        {
            if (_planetMarkingBinding != null)
            {
                EventBus<PlanetMarkingChangedEvent>.Unregister(_planetMarkingBinding);
            }
        }

        /// <summary>
        /// Permite testes manuais ou integrações externas alterarem o planeta marcado.
        /// </summary>
        public void SetMarkedPlanet(PlanetsMaster planet)
        {
            _markedPlanet = planet;
            UpdateIconHighlights();
        }

        private void HandlePlanetMarkingChanged(PlanetMarkingChangedEvent @event)
        {
            var newMarked = @event.NewMarkedPlanet?.Transform?.GetComponentInParent<PlanetsMaster>();
            SetMarkedPlanet(newMarked);
        }

        private void UpdateIconHighlights()
        {
            if (compassHUD == null)
            {
                return;
            }

            IEnumerable<(ICompassTrackable target, CompassIcon icon)> icons = compassHUD.EnumerateIcons();
            foreach ((var target, var icon) in icons)
            {
                if (target == null || icon == null)
                {
                    continue;
                }

                var targetTransform = target.Transform;
                var planetMaster = targetTransform != null ? targetTransform.GetComponentInParent<PlanetsMaster>() : null;
                bool isMarked = planetMaster != null && planetMaster == _markedPlanet;

                Color? tint = tintMarkedIcon ? markedColorTint : (Color?)null;
                icon.SetMarked(isMarked, markedScaleMultiplier, tint);
            }
        }
    }
}
