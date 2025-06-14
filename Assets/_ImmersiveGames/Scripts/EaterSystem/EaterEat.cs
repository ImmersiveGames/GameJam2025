using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using System.Collections;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterEat : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Intervalo entre cada aplicação de dano ao planeta, em segundos.")]
        private float damageInterval = 2f; // Intervalo de 2 segundos

        private EaterMaster _eater;
        private EaterConfigSo _config;
        private PlanetHealth _planetHealth;
        private Coroutine _damageCoroutine;
        private EventBinding<DeathEvent> _deathEventBinding;

        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
            if (_eater == null)
            {
                DebugUtility.LogError<EaterEat>($"EaterMaster não encontrado em {gameObject.name}!");
            }
            _config = _eater.GetConfig;
            if (_config == null)
            {
                DebugUtility.LogError<EaterEat>($"EaterConfigSo não atribuído em {gameObject.name}!");
            }
        }

        private void OnEnable()
        {
            _eater.EventStartEatPlanet += OnEatPlanetEvent;
            _deathEventBinding = new EventBinding<DeathEvent>(OnPlanetDeath);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            DebugUtility.Log<EaterEat>($"Registrado DeathEvent em {gameObject.name}.");
        }

        private void OnDisable()
        {
            if (_eater != null)
            {
                _eater.EventStartEatPlanet -= OnEatPlanetEvent;
            }
            EventBus<DeathEvent>.Unregister(_deathEventBinding);
            _planetHealth = null;
            StopDamageCoroutine();
            DebugUtility.Log<EaterEat>($"Desativado EaterEat em {gameObject.name}.");
        }

        private void OnEatPlanetEvent(IDetectable obj)
        {
            if (obj == null || obj.GetPlanetsMaster() == null)
            {
                DebugUtility.LogWarning<EaterEat>("IDetectable ou PlanetsMaster é nulo no evento EventStartEatPlanet!");
                return;
            }

            _planetHealth = obj.GetPlanetsMaster().GetComponent<PlanetHealth>();
            if (_planetHealth == null)
            {
                DebugUtility.LogWarning<EaterEat>($"PlanetHealth não encontrado em {obj.GetPlanetsMaster().name}!");
                return;
            }

            StopDamageCoroutine();
            _damageCoroutine = StartCoroutine(ApplyDamageOverTime());
            DebugUtility.Log<EaterEat>($"Iniciado dano automático ao planeta {obj.GetPlanetsMaster().name} a cada {damageInterval} segundos.");
        }

        private void OnPlanetDeath(DeathEvent evt)
        {
            if (_planetHealth == null || evt.Source != _planetHealth.gameObject) return;
            StopDamageCoroutine();
            _planetHealth = null;
            DebugUtility.Log<EaterEat>($"Planeta {evt.Source.name} destruído. Dano automático interrompido.");
        }

        private IEnumerator ApplyDamageOverTime()
        {
            while (_planetHealth != null)
            {
                if (_planetHealth == null || _planetHealth.GetCurrentValue() <= 0 || !_planetHealth.gameObject.activeInHierarchy)
                {
                    DebugUtility.Log<EaterEat>($"Interrompendo dano automático: planeta {(_planetHealth != null ? _planetHealth.gameObject.name : "nulo")} destruído ou inativo.");
                    break;
                }

                _planetHealth.TakeDamage(_config.BiteDamage);
                if (_planetHealth != null)
                {
                    DebugUtility.Log<EaterEat>($"Aplicado dano de {_config.BiteDamage} ao planeta {_planetHealth.gameObject.name}. Saúde atual: {_planetHealth.GetCurrentValue()}.");
                }
                else
                {
                    DebugUtility.Log<EaterEat>("Interrompendo dano automático: _planetHealth tornou-se nulo após TakeDamage.");
                    break;
                }
                yield return new WaitForSeconds(damageInterval);
            }

            StopDamageCoroutine();
            _planetHealth = null;
            DebugUtility.Log<EaterEat>("Coroutine de dano interrompido: planeta destruído ou inativo.");
        }

        private void StopDamageCoroutine()
        {
            if (_damageCoroutine != null)
            {
                StopCoroutine(_damageCoroutine);
                _damageCoroutine = null;
                DebugUtility.Log<EaterEat>("Coroutine de dano automático interrompido.");
            }
        }
    }
}