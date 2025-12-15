using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems.Compass
{
    /// <summary>
    /// Controla o destaque do planeta atualmente marcado, ampliando o ícone na bússola.
    /// </summary>
    [RequireComponent(typeof(CompassHUD))]
    public class CompassPlanetHighlightController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("HUD da bússola responsável por instanciar os ícones.")]
        [SerializeField] private CompassHUD compassHUD;

        [Header("Highlight Settings")]
        [Tooltip("Multiplicador de escala aplicado ao ícone do planeta marcado.")]
        [SerializeField] private float markedScaleMultiplier = 1.3f;

        [Tooltip("Cor opcional para dar destaque ao planeta marcado.")]
        [SerializeField] private Color markedColorTint = Color.white;

        [Tooltip("Se true, aplica o tint de cor ao ícone marcado.")]
        [SerializeField] private bool tintMarkedIcon;

        private EventBinding<PlanetMarkingChangedEvent> _planetMarkingBinding;
        private PlanetsMaster _markedPlanet;

        private void Reset()
        {
            compassHUD = GetComponent<CompassHUD>();
        }

        private void OnValidate()
        {
            if (compassHUD == null)
                compassHUD = GetComponent<CompassHUD>();
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

        private void SetMarkedPlanet(PlanetsMaster planet)
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
            if (compassHUD == null || _markedPlanet == null && !tintMarkedIcon && markedScaleMultiplier <= 1f)
                return;

            compassHUD.ForEachIcon((target, icon) =>
            {
                var planetMaster = target.Transform != null
                    ? target.Transform.GetComponentInParent<PlanetsMaster>()
                    : null;

                bool isMarked = planetMaster != null && planetMaster == _markedPlanet;

                Color? tint = tintMarkedIcon ? markedColorTint : null;
                icon.SetMarked(isMarked, markedScaleMultiplier, tint);
            });
        }
    }
}