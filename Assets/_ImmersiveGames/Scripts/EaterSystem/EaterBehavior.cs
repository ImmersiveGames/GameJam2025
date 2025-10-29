using System;
using _ImmersiveGames.Scripts.EaterSystem.Debug;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    /// <summary>
    /// Implementação simplificada do comportamento do Eater.
    /// Mantém apenas uma FSM básica com cinco estados e eventos de transição.
    /// </summary>
    [RequireComponent(typeof(EaterMaster))]
    [AddComponentMenu("ImmersiveGames/Eater/Eater Behavior")]
    [DefaultExecutionOrder(10)]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterBehavior : MonoBehaviour
    {
        private EaterMaster _master;
        private IState _currentState;
        private EaterIdleState _idleState;
        private EaterWanderingState _wanderingState;
        private EaterHungryState _hungryState;
        private EaterChasingState _chasingState;
        private EaterEatingState _eatingState;

        private bool _isHungry;
        private bool _isEating;
        private bool _hasProximityContact;
        private Vector3 _lastProximityPoint;
        private PlanetsMaster _currentTarget;
        private EaterDesireInfo _currentDesireInfo = EaterDesireInfo.Inactive;

        public event Action<IState, IState> EventStateChanged;
        public event Action<EaterDesireInfo> EventDesireChanged;
        public event Action<PlanetsMaster> EventTargetChanged;

        public IState CurrentState => _currentState;
        public string CurrentStateName => _currentState?.ToString() ?? string.Empty;
        public PlanetsMaster CurrentTarget => _currentTarget;
        public bool IsEating => _isEating;
        public bool IsHungry => _isHungry;
        public bool ShouldEnableProximitySensor => _currentState == _chasingState || _currentState == _eatingState;

        private void Awake()
        {
            _master = GetComponent<EaterMaster>();
            BuildStates();
        }

        private void Start()
        {
            ChangeState(_idleState, "Inicialização");
            EventDesireChanged?.Invoke(_currentDesireInfo);
            EventTargetChanged?.Invoke(_currentTarget);
        }

        private void Update()
        {
            _currentState?.Update();
        }

        private void FixedUpdate()
        {
            _currentState?.FixedUpdate();
        }

        /// <summary>
        /// Atualiza o estado de fome do Eater.
        /// </summary>
        public void SetHungry(bool isHungry)
        {
            if (_isHungry == isHungry)
            {
                return;
            }

            _isHungry = isHungry;
            if (_master != null)
            {
                _master.InHungry = isHungry;
            }

            DebugUtility.LogVerbose<EaterBehavior>($"Estado de fome atualizado: {_isHungry}.", null, this);
            ReevaluateState("Mudança de fome");
        }

        /// <summary>
        /// Define o planeta alvo atual.
        /// </summary>
        public void SetTarget(PlanetsMaster target)
        {
            if (_currentTarget == target)
            {
                return;
            }

            _currentTarget = target;
            if (_currentTarget == null)
            {
                ClearProximityContact();
            }

            EventTargetChanged?.Invoke(_currentTarget);
            DebugUtility.LogVerbose<EaterBehavior>($"Alvo atualizado: {GetPlanetName(_currentTarget)}.", null, this);
            ReevaluateState("Mudança de alvo");
        }

        /// <summary>
        /// Remove o alvo atual, caso exista.
        /// </summary>
        public void ClearTarget()
        {
            if (_currentTarget == null)
            {
                return;
            }

            SetTarget(null);
        }

        /// <summary>
        /// Inicia o processo de comer caso haja um alvo válido.
        /// </summary>
        public void BeginEating()
        {
            if (_currentTarget == null)
            {
                DebugUtility.LogWarning<EaterBehavior>("Tentativa de iniciar consumo sem alvo.", this);
                return;
            }

            if (_isEating)
            {
                return;
            }

            _isEating = true;
            _master.IsEating = true;
            _master.OnEventStartEatPlanet(_currentTarget);
            DebugUtility.LogVerbose<EaterBehavior>($"Início do consumo: {GetPlanetName(_currentTarget)}.", null, this);
            ReevaluateState("Início do consumo");
        }

        /// <summary>
        /// Finaliza o processo de comer.
        /// </summary>
        public void EndEating(bool satisfied)
        {
            if (!_isEating)
            {
                return;
            }

            _isEating = false;
            _master.IsEating = false;
            _master.OnEventEndEatPlanet(_currentTarget);
            DebugUtility.LogVerbose<EaterBehavior>($"Fim do consumo (satisfeito={satisfied}).", null, this);
            ReevaluateState("Fim do consumo");
        }

        /// <summary>
        /// Informa que o planeta alvo está no alcance de proximidade.
        /// </summary>
        public void RegisterProximityContact(PlanetsMaster planet, Vector3 eaterPosition)
        {
            if (planet == null)
            {
                return;
            }

            if (_currentTarget != null && planet != _currentTarget)
            {
                return;
            }

            _hasProximityContact = true;
            _lastProximityPoint = eaterPosition;
            DebugUtility.LogVerbose<EaterBehavior>($"Contato de proximidade registrado em {eaterPosition}.", null, this);
            BeginEating();
        }

        /// <summary>
        /// Cancela o contato de proximidade atualmente registrado.
        /// </summary>
        public void ClearProximityContact(PlanetsMaster planet = null)
        {
            if (planet != null && planet != _currentTarget)
            {
                return;
            }

            if (!_hasProximityContact)
            {
                return;
            }

            _hasProximityContact = false;
            _lastProximityPoint = Vector3.zero;
            DebugUtility.LogVerbose<EaterBehavior>("Contato de proximidade limpo.", null, this);

            if (_isEating)
            {
                EndEating(false);
            }
        }

        /// <summary>
        /// Reexecuta a lógica de seleção de estado considerando os dados atuais.
        /// </summary>
        public void ForceStateEvaluation()
        {
            ReevaluateState("Forçado externamente");
        }

        /// <summary>
        /// Obtém o desejo atual. Nesta versão simplificada sempre retorna inativo.
        /// </summary>
        public EaterDesireInfo GetCurrentDesireInfo()
        {
            return _currentDesireInfo;
        }

        /// <summary>
        /// Atualiza o desejo atual e notifica ouvintes.
        /// </summary>
        public void SetDesireInfo(EaterDesireInfo info)
        {
            _currentDesireInfo = info;
            EventDesireChanged?.Invoke(_currentDesireInfo);
        }

        /// <summary>
        /// Cria um snapshot resumido para ferramentas de debug.
        /// </summary>
        public EaterBehaviorDebugSnapshot CreateDebugSnapshot()
        {
            if (_currentState == null)
            {
                return EaterBehaviorDebugSnapshot.Empty;
            }

            return new EaterBehaviorDebugSnapshot(
                isValid: true,
                currentState: CurrentStateName,
                isHungry: _isHungry,
                isEating: _isEating,
                hasTarget: _currentTarget != null,
                targetName: GetPlanetName(_currentTarget),
                hasProximityContact: _hasProximityContact,
                lastProximityPoint: _lastProximityPoint);
        }

        private void BuildStates()
        {
            _idleState = new EaterIdleState(this);
            _wanderingState = new EaterWanderingState(this);
            _hungryState = new EaterHungryState(this);
            _chasingState = new EaterChasingState(this);
            _eatingState = new EaterEatingState(this);
        }

        private void ChangeState(IState nextState, string reason)
        {
            if (nextState == null || _currentState == nextState)
            {
                return;
            }

            IState previous = _currentState;
            previous?.OnExit();
            _currentState = nextState;
            _currentState.OnEnter();

            string previousName = previous?.ToString() ?? "Nenhum";
            string currentName = _currentState?.ToString() ?? "Nenhum";
            DebugUtility.LogVerbose<EaterBehavior>($"Transição de estado: {previousName} → {currentName} ({reason}).", null, this);
            EventStateChanged?.Invoke(previous, _currentState);
        }

        private void ReevaluateState(string reason)
        {
            IState desiredState;

            if (_isEating && _currentTarget != null)
            {
                desiredState = _eatingState;
            }
            else if (_isHungry && _currentTarget != null)
            {
                desiredState = _chasingState;
            }
            else if (_isHungry)
            {
                desiredState = _hungryState;
            }
            else if (_currentTarget != null)
            {
                desiredState = _chasingState;
            }
            else
            {
                desiredState = _wanderingState;
            }

            if (_currentState == null)
            {
                ChangeState(desiredState, reason);
            }
            else
            {
                ChangeState(desiredState, reason);
            }
        }

        private static string GetPlanetName(PlanetsMaster planet)
        {
            return planet != null ? planet.name : "Nenhum";
        }

#if UNITY_EDITOR
        [ContextMenu("Debug/Forçar estado/Idle")]
        private void DebugForceIdleState()
        {
            ForceDebugState(_idleState, "Idle", requiresTarget: false, markHungry: false, markEating: false);
        }

        [ContextMenu("Debug/Forçar estado/Wandering")]
        private void DebugForceWanderingState()
        {
            ForceDebugState(_wanderingState, "Wandering", requiresTarget: false, markHungry: false, markEating: false);
        }

        [ContextMenu("Debug/Forçar estado/Hungry")]
        private void DebugForceHungryState()
        {
            ForceDebugState(_hungryState, "Hungry", requiresTarget: false, markHungry: true, markEating: false);
        }

        [ContextMenu("Debug/Forçar estado/Chasing")]
        private void DebugForceChasingState()
        {
            ForceDebugState(_chasingState, "Chasing", requiresTarget: true, markHungry: true, markEating: false);
        }

        [ContextMenu("Debug/Forçar estado/Eating")]
        private void DebugForceEatingState()
        {
            ForceDebugState(_eatingState, "Eating", requiresTarget: true, markHungry: true, markEating: true);
        }

        private void ForceDebugState(IState desiredState, string label, bool requiresTarget, bool markHungry, bool markEating)
        {
            if (desiredState == null)
            {
                DebugUtility.LogWarning<EaterBehavior>($"Estado {label} indisponível para debug.", this);
                return;
            }

            if (requiresTarget && _currentTarget == null)
            {
                DebugUtility.LogWarning<EaterBehavior>($"Estado {label} exige um planeta alvo configurado antes do teste.", this);
                return;
            }

            _isHungry = markHungry;
            _isEating = markEating;

            if (_master != null)
            {
                _master.InHungry = _isHungry;
                _master.IsEating = _isEating;
            }

            if (!markEating)
            {
                _lastProximityPoint = Vector3.zero;
                _hasProximityContact = false;
            }
            else
            {
                _hasProximityContact = true;
                _lastProximityPoint = transform.position;
            }

            ChangeState(desiredState, $"Menu de contexto ({label})");
            DebugUtility.Log<EaterBehavior>($"Estado {label} forçado via menu de contexto.", context: this);
        }
#endif
    }
}
