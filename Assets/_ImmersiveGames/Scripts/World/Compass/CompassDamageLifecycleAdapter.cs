using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.World.Compass
{
    /// <summary>
    /// Adaptador genérico que sincroniza o ciclo de vida de DamageSystem com a bússola.
    /// Qualquer ator que possua <see cref="ActorMaster"/>, <see cref="CompassTarget"/> e <see cref="DamageReceiver"/>
    /// pode usar este componente para registrar e remover o alvo automaticamente ao morrer, reviver ou ser resetado.
    /// </summary>
    [RequireComponent(typeof(ActorMaster))]
    public class CompassDamageLifecycleAdapter : MonoBehaviour
    {
        private ActorMaster _actor;
        private ICompassTrackable _trackable;
        private string _entityId;
        private bool _isRegisteredWithCompass;
        private bool _eventsRegistered;

        private EventBinding<DeathEvent> _deathBinding;
        private EventBinding<ReviveEvent> _reviveBinding;
        private EventBinding<ResetEvent> _resetBinding;

        private void Awake()
        {
            CacheDependencies();

            if (_actor == null)
            {
                DebugUtility.LogError<CompassDamageLifecycleAdapter>(
                    "CompassDamageLifecycleAdapter requer ActorMaster no mesmo GameObject.", this);
            }

            if (_trackable == null)
            {
                DebugUtility.LogWarning<CompassDamageLifecycleAdapter>(
                    "Nenhum ICompassTrackable encontrado. O alvo não será sincronizado com a bússola.",
                    this);
            }
        }

        private void OnEnable()
        {
            CacheDependencies();

            if (string.IsNullOrEmpty(_entityId) || _trackable == null)
            {
                return;
            }

            RegisterWithCompass();
            RegisterEventBindings();
        }

        private void OnDisable()
        {
            UnregisterEventBindings();
            UnregisterFromCompass();
        }

        private void OnDestroy()
        {
            UnregisterEventBindings();
            UnregisterFromCompass();
        }

        private void RegisterEventBindings()
        {
            if (_eventsRegistered || string.IsNullOrEmpty(_entityId))
            {
                return;
            }

            _deathBinding ??= new EventBinding<DeathEvent>(OnDeathEvent);
            _reviveBinding ??= new EventBinding<ReviveEvent>(OnReviveEvent);
            _resetBinding ??= new EventBinding<ResetEvent>(OnResetEvent);

            FilteredEventBus<DeathEvent>.Register(_deathBinding, _entityId);
            FilteredEventBus<ReviveEvent>.Register(_reviveBinding, _entityId);
            FilteredEventBus<ResetEvent>.Register(_resetBinding, _entityId);

            _eventsRegistered = true;

            DebugUtility.LogVerbose<CompassDamageLifecycleAdapter>(
                $"🧭 Bridge registrado para {_entityId} (registrado na bússola: {_isRegisteredWithCompass}).",
                this);
        }

        private void UnregisterEventBindings()
        {
            if (!_eventsRegistered || string.IsNullOrEmpty(_entityId))
            {
                return;
            }

            FilteredEventBus<DeathEvent>.Unregister(_deathBinding, _entityId);
            FilteredEventBus<ReviveEvent>.Unregister(_reviveBinding, _entityId);
            FilteredEventBus<ResetEvent>.Unregister(_resetBinding, _entityId);

            _eventsRegistered = false;
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (!IsMatchingEntity(evt.EntityId))
            {
                return;
            }

            if (_isRegisteredWithCompass)
            {
                CompassRuntimeService.UnregisterTarget(_trackable);
                _isRegisteredWithCompass = false;
            }
        }

        private void OnReviveEvent(ReviveEvent evt)
        {
            if (!IsMatchingEntity(evt.EntityId))
            {
                return;
            }

            RegisterWithCompass();
        }

        private void OnResetEvent(ResetEvent evt)
        {
            if (!IsMatchingEntity(evt.EntityId))
            {
                return;
            }

            RegisterWithCompass();
        }

        private void RegisterWithCompass()
        {
            if (_trackable == null || _isRegisteredWithCompass || !_trackable.IsActive)
            {
                return;
            }

            CompassRuntimeService.RegisterTarget(_trackable);
            _isRegisteredWithCompass = true;
        }

        private void UnregisterFromCompass()
        {
            if (_trackable == null || !_isRegisteredWithCompass)
            {
                return;
            }

            CompassRuntimeService.UnregisterTarget(_trackable);
            _isRegisteredWithCompass = false;
        }

        private bool IsMatchingEntity(string entityId)
        {
            return !string.IsNullOrEmpty(_entityId) && _entityId == entityId;
        }

        private void CacheDependencies()
        {
            _actor ??= GetComponent<ActorMaster>();
            _trackable ??= GetComponentInChildren<ICompassTrackable>();
            _entityId = _actor?.ActorId;
        }
    }
}
