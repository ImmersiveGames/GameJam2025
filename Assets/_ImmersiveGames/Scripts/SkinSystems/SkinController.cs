using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.SkinSystems.EventsBus;
using _ImmersiveGames.Scripts.SkinSystems.Loaders;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class SkinController : MonoBehaviour, IResettable
    {
        [SerializeField] private List<GameObjectSelectorData> skinSelectors;

        private SkinModelLoader _loader;
        private EventBinding<ResetEvent> _resetBinding;
        private EventBinding<FxToggleEvent> _fxBinding;

        private void Awake()
        {
            _loader = new SkinModelLoader(transform);
        }

        private void OnEnable()
        {
            _resetBinding = new EventBinding<ResetEvent>(Reset);
            _fxBinding = new EventBinding<FxToggleEvent>(OnFxToggle);

            EventBus<ResetEvent>.Register(_resetBinding);
            FilteredEventBus<FxToggleEvent>.Register(_fxBinding, this);
        }

        private void OnDisable()
        {
            EventBus<ResetEvent>.Unregister(_resetBinding);
            FilteredEventBus<FxToggleEvent>.Unregister(this);
        }

        private void Start()
        {
            _loader.Load(skinSelectors, GetInstanceID());
        }

        public void Reset()
        {
            _loader.ClearAll();
            _loader.Load(skinSelectors, GetInstanceID());
        }

        private void OnFxToggle(FxToggleEvent evt)
        {
            _loader.SetActive(evt.FxTag, evt.Active);
        }

        public void SetObjectActive(string tagName, bool active)
        {
            _loader.SetActive(tagName, active);
        }

        public void ClearSkin()
        {
            _loader.ClearAll();
        }

        public GameObject GetObjectByTag(string tagName)
        {
            return _loader.TryGetInstanceByTag(tagName, out var info) ? info.GameObject : null;
        }
    }
}