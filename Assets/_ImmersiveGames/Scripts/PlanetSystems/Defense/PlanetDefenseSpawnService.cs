using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Detectable;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Serviço responsável por abrir e fechar sessões de spawn defensivo conforme detectores ativos.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlanetDefenseSpawnService : MonoBehaviour, IPlanetDefenseActivationListener
    {
        [SerializeField] private PlanetDefenseController defenseController;

        [Header("Defense Spawn Events")]
        [SerializeField] private UnityEvent onSpawnSessionStarted;
        [SerializeField] private UnityEvent onSpawnSessionEnded;

        private DefenseSpawnSession _currentSession;
        private int _activeDetectorsCount;

        private void Awake()
        {
            if (defenseController == null && !TryGetComponent(out defenseController))
            {
                defenseController = GetComponentInParent<PlanetDefenseController>();
            }

            if (defenseController == null)
            {
                DebugUtility.LogError<PlanetDefenseSpawnService>(
                    $"PlanetDefenseController não encontrado para {gameObject.name}.",
                    this);
                return;
            }

            defenseController.RegisterActivationListener(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (defenseController == null)
            {
                defenseController = GetComponentInParent<PlanetDefenseController>();
            }
        }
#endif

        public void OnDefenseEngaged(IDetector detector, DefenseRole role)
        {
            _activeDetectorsCount++;
            if (_activeDetectorsCount == 1)
            {
                StartSpawnSession(detector, role);
            }
        }

        public void OnDefenseDisengaged(IDetector detector, DefenseRole role)
        {
            if (_activeDetectorsCount == 0)
            {
                return;
            }

            _activeDetectorsCount = Mathf.Max(0, _activeDetectorsCount - 1);

            if (_activeDetectorsCount == 0)
            {
                CompleteSpawnSession();
            }
        }

        private void StartSpawnSession(IDetector detector, DefenseRole role)
        {
            _currentSession = new DefenseSpawnSession(detector, role);

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Sessão de spawn defensivo iniciada para {defenseController?.name ?? gameObject.name} por {role}.",
                DebugUtility.Colors.CrucialInfo,
                this);

            onSpawnSessionStarted?.Invoke();
        }

        private void CompleteSpawnSession()
        {
            if (_currentSession == null || !_currentSession.IsActive)
            {
                return;
            }

            _currentSession.Complete();

            DebugUtility.LogVerbose<PlanetDefenseSpawnService>(
                $"Sessão de spawn defensivo encerrada para {defenseController?.name ?? gameObject.name}.",
                null,
                this);

            onSpawnSessionEnded?.Invoke();
            _currentSession = null;
        }
    }
}
