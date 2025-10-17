using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DisallowMultipleComponent]
    public class PlanetResourceCanvasView : MonoBehaviour
    {
        [SerializeField] private Image resourceImage;
        [SerializeField] private Sprite fallbackIcon;

        private PlanetResourceController _controller;
        private IActor _actor;
        private EventBinding<PlanetResourceChangedEvent> _resourceBinding;

        private void Awake()
        {
            _controller = GetComponentInParent<PlanetResourceController>(true);
            _actor = GetComponentInParent<IActor>(true);
        }

        private void OnEnable()
        {
            if (_controller == null || _actor == null)
            {
                DebugUtility.LogWarning<PlanetResourceCanvasView>($"Configuração incompleta no canvas {gameObject.name}.", this);
                return;
            }

            _resourceBinding ??= new EventBinding<PlanetResourceChangedEvent>(OnPlanetResourceChanged);
            FilteredEventBus<PlanetResourceChangedEvent>.Register(_resourceBinding, _actor.ActorId);

            ApplyResource(_controller.CurrentResource);
        }

        private void OnDisable()
        {
            if (_resourceBinding != null && _actor != null)
            {
                FilteredEventBus<PlanetResourceChangedEvent>.Unregister(_resourceBinding, _actor.ActorId);
            }
        }

        private void OnPlanetResourceChanged(PlanetResourceChangedEvent evt)
        {
            ApplyResource(evt.Resource);
        }

        private void ApplyResource(PlanetResourcesSo resource)
        {
            if (resourceImage == null)
            {
                return;
            }

            var sprite = resource != null ? resource.ResourceIcon : fallbackIcon;
            resourceImage.sprite = sprite;
            resourceImage.enabled = sprite != null;
        }
    }
}
