using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.StateMachineSystems.GameStates
{
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class OldGameStateBase : OldIState
    {
        protected readonly GameManager gameManager;
        private readonly HashSet<OldActionType> _allowedActions;

        protected OldGameStateBase(GameManager gameManager)
        {
            this.gameManager = gameManager;
            _allowedActions = new HashSet<OldActionType>();
        }

        /// <summary>
        /// Acesso tardio ao gate global. Não falha caso o serviço ainda não exista.
        /// </summary>
        protected IOldSimulationGateService Gate
        {
            get
            {
                return DependencyManager.Provider.TryGetGlobal<IOldSimulationGateService>(out var gate) ? gate : null;
            }
        }

        protected void AcquireGate(string token)
        {
            var gate = Gate;
            if (gate == null) return;

            // Idempotente: evita "double acquire" no mesmo token (que leva a logs duplicados)
            if (gate.IsTokenActive(token)) return;

            gate.Acquire(token);
        }

        protected void ReleaseGate(string token)
        {
            var gate = Gate;
            if (gate == null) return;

            // Idempotente: evita spam de "Release ignorado (token não estava ativo)"
            if (!gate.IsTokenActive(token)) return;

            gate.Release(token);
        }

        protected void AllowActions(params OldActionType[] actions)
        {
            foreach (OldActionType action in actions)
            {
                _allowedActions.Add(action);
            }
        }

        /// <summary>
        /// Perfil comum para telas que exibem UI e pausam o jogo, mas precisam aceitar navegação.
        /// </summary>
        protected void AllowMenuNavigationWithExitShortcuts()
        {
            AllowActions(
                OldActionType.Navigate,
                OldActionType.UiSubmit,
                OldActionType.UiCancel,
                OldActionType.RequestReset,
                OldActionType.RequestQuit
            );
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Update() { }

        public virtual bool CanPerformAction(OldActionType action)
        {
            return _allowedActions.Contains(action);
        }

        public virtual bool IsGameActive() => false;

        public virtual void FixedUpdate() { }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class OldMenuState : OldGameStateBase
    {
        public OldMenuState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override void OnEnter()
        {
            AcquireGate(OldSimulationGateTokens.Menu);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(false));
            DebugUtility.LogVerbose<OldMenuState>("Iniciando o menu do jogo.");
        }

        public override void OnExit()
        {
            ReleaseGate(OldSimulationGateTokens.Menu);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<OldMenuState>("Saindo do menu do jogo.");
        }

        public override bool IsGameActive() => false;
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class OldPlayingState : OldGameStateBase
    {
        public OldPlayingState(GameManager gameManager) : base(gameManager)
        {
            AllowActions(OldActionType.Move, OldActionType.Shoot, OldActionType.Spawn, OldActionType.Interact);
        }

        public override void OnEnter()
        {
            // Libera tokens conhecidos de forma defensiva e silenciosa (idempotente).
            ReleaseGate(OldSimulationGateTokens.Menu);
            ReleaseGate(OldSimulationGateTokens.Pause);
            ReleaseGate(OldSimulationGateTokens.GameOver);
            ReleaseGate(OldSimulationGateTokens.Victory);
            ReleaseGate(OldSimulationGateTokens.SceneTransition);
            ReleaseGate(OldSimulationGateTokens.Cinematic);
            ReleaseGate(OldSimulationGateTokens.SoftReset);
            ReleaseGate(OldSimulationGateTokens.Loading);

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<OldPlayingState>("Entrou no estado Playing.");
        }

        public override bool IsGameActive() => true;
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class OldPausedState : OldGameStateBase
    {
        public OldPausedState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override void OnEnter()
        {
            AcquireGate(OldSimulationGateTokens.Pause);

            Time.timeScale = 0f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(false));
            DebugUtility.LogVerbose<OldPausedState>("Entrou no estado Paused.");
        }

        public override void OnExit()
        {
            ReleaseGate(OldSimulationGateTokens.Pause);

            Time.timeScale = 1f;
            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
            EventBus<ActorStateChangedEvent>.Raise(new ActorStateChangedEvent(true));
            DebugUtility.LogVerbose<OldPausedState>("Saiu do estado Paused.");
        }

        public override bool IsGameActive() => false;
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class OldGameOverState : OldGameStateBase
    {
        public OldGameOverState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override bool IsGameActive() => false;

        public override void OnEnter()
        {
            Debug.Log("Gate IsOpen=" + Gate?.IsOpen);
            AcquireGate(OldSimulationGateTokens.GameOver);

            // IMPORTANTE: estado terminal NÃO deve congelar timeScale; overlay/animações precisam continuar.
            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<OldGameOverState>("game over!");
        }

        public override void OnExit()
        {
            ReleaseGate(OldSimulationGateTokens.GameOver);

            // Normaliza por segurança.
            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class OldVictoryState : OldGameStateBase
    {
        public OldVictoryState(GameManager gameManager) : base(gameManager)
        {
            AllowMenuNavigationWithExitShortcuts();
        }

        public override bool IsGameActive() => false;

        public override void OnEnter()
        {
            Debug.Log("Gate IsOpen=" + Gate?.IsOpen);
            AcquireGate(OldSimulationGateTokens.Victory);

            // IMPORTANTE: estado terminal NÃO deve congelar timeScale; overlay/animações precisam continuar.
            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(false));
            DebugUtility.LogVerbose<OldVictoryState>("Terminou o jogo!");
        }

        public override void OnExit()
        {
            ReleaseGate(OldSimulationGateTokens.Victory);

            // Normaliza por segurança.
            Time.timeScale = 1f;

            EventBus<StateChangedEvent>.Raise(new StateChangedEvent(true));
        }
    }
}



