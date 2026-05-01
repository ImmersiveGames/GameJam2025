using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core
{
    /// <summary>
    /// Identidade explicita minima para sinais tecnicos do GameLoop.
    /// Mantem os eventos comparaveis sem transformar o loop em owner de lifecycle.
    /// </summary>
    public sealed class GameLoopSignalIdentity : IEquatable<GameLoopSignalIdentity>
    {
        private readonly bool _technicalInternal;

        public GameLoopSignalIdentity(
            string phaseEntryIdentity = null,
            string sessionSignature = null,
            string entrySignature = null,
            string cycleSignature = null,
            string reason = null,
            string source = null,
            string handshake = null,
            string routeKind = null,
            string targetScene = null,
            bool technicalInternal = false)
        {
            PhaseEntryIdentity = Normalize(phaseEntryIdentity);
            SessionSignature = Normalize(sessionSignature);
            EntrySignature = Normalize(entrySignature);
            CycleSignature = Normalize(cycleSignature);
            Reason = Normalize(reason);
            Source = Normalize(source);
            Handshake = Normalize(handshake);
            RouteKind = Normalize(routeKind);
            TargetScene = Normalize(targetScene);
            _technicalInternal = technicalInternal;
        }

        public string PhaseEntryIdentity { get; }
        public string SessionSignature { get; }
        public string EntrySignature { get; }
        public string CycleSignature { get; }
        public string Reason { get; }
        public string Source { get; }
        public string Handshake { get; }
        public string RouteKind { get; }
        public string TargetScene { get; }
        public bool HasCanonicalIdentity =>
            !string.IsNullOrWhiteSpace(PhaseEntryIdentity) ||
            !string.IsNullOrWhiteSpace(SessionSignature) ||
            !string.IsNullOrWhiteSpace(EntrySignature) ||
            !string.IsNullOrWhiteSpace(CycleSignature);

        public bool IsTechnicalInternal => _technicalInternal || !HasCanonicalIdentity;

        public static GameLoopSignalIdentity TechnicalInternal(
            string reason,
            string source,
            string handshake = null,
            string routeKind = null,
            string targetScene = null)
        {
            return new GameLoopSignalIdentity(
                reason: reason,
                source: source,
                handshake: handshake,
                routeKind: routeKind,
                targetScene: targetScene,
                technicalInternal: true);
        }

        public bool Matches(GameLoopSignalIdentity other)
        {
            if (other == null)
            {
                return !HasCanonicalIdentity;
            }

            if (!HasCanonicalIdentity && !other.HasCanonicalIdentity)
            {
                return true;
            }

            return MatchesField(PhaseEntryIdentity, other.PhaseEntryIdentity) &&
                   MatchesField(SessionSignature, other.SessionSignature) &&
                   MatchesField(EntrySignature, other.EntrySignature) &&
                   MatchesField(CycleSignature, other.CycleSignature) &&
                   MatchesField(RouteKind, other.RouteKind) &&
                   MatchesField(TargetScene, other.TargetScene);
        }

        public string Describe()
        {
            return $"phaseEntry='{Format(PhaseEntryIdentity)}' session='{Format(SessionSignature)}' entry='{Format(EntrySignature)}' cycle='{Format(CycleSignature)}' reason='{Format(Reason)}' source='{Format(Source)}' handshake='{Format(Handshake)}' routeKind='{Format(RouteKind)}' targetScene='{Format(TargetScene)}' technicalInternal='{IsTechnicalInternal.ToString().ToLowerInvariant()}'";
        }

        public override string ToString() => Describe();

        public bool Equals(GameLoopSignalIdentity other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(PhaseEntryIdentity, other.PhaseEntryIdentity, StringComparison.Ordinal) &&
                   string.Equals(SessionSignature, other.SessionSignature, StringComparison.Ordinal) &&
                   string.Equals(EntrySignature, other.EntrySignature, StringComparison.Ordinal) &&
                   string.Equals(CycleSignature, other.CycleSignature, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Handshake, other.Handshake, StringComparison.Ordinal) &&
                   string.Equals(RouteKind, other.RouteKind, StringComparison.Ordinal) &&
                   string.Equals(TargetScene, other.TargetScene, StringComparison.Ordinal) &&
                   _technicalInternal == other._technicalInternal;
        }

        public override bool Equals(object obj) => obj is GameLoopSignalIdentity other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(PhaseEntryIdentity, StringComparer.Ordinal);
            hash.Add(SessionSignature, StringComparer.Ordinal);
            hash.Add(EntrySignature, StringComparer.Ordinal);
            hash.Add(CycleSignature, StringComparer.Ordinal);
            hash.Add(Reason, StringComparer.Ordinal);
            hash.Add(Source, StringComparer.Ordinal);
            hash.Add(Handshake, StringComparer.Ordinal);
            hash.Add(RouteKind, StringComparer.Ordinal);
            hash.Add(TargetScene, StringComparer.Ordinal);
            hash.Add(_technicalInternal);
            return hash.ToHashCode();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string Format(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<null>" : value;
        }

        private static bool MatchesField(string expected, string received)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(received))
            {
                return true;
            }

            return string.Equals(expected, received, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// REQUEST (intencao): "quero iniciar a simulacao".
    /// Contrato canonico: usado apenas pelo boot/start-plan.
    /// </summary>
    public sealed class BootStartPlanRequestedEvent : IEvent { }

    /// <summary>
    /// REQUEST (intencao): "quero entrar em gameplay".
    /// Contrato canonico para intent de Play vinda de UI/Frontend.
    /// </summary>
    public sealed class GamePlayRequestedEvent : IEvent
    {
        public GamePlayRequestedEvent(string reason = null, GameLoopSignalIdentity identity = null)
        {
            Reason = reason;
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                reason,
                nameof(GamePlayRequestedEvent),
                handshake: nameof(GamePlayRequestedEvent));
        }

        public string Reason { get; }
        public GameLoopSignalIdentity Identity { get; }
    }

    /// <summary>
    /// Evento definitivo para pausa / despausa.
    /// </summary>
    public sealed class GamePauseCommandEvent : IEvent
    {
        public GamePauseCommandEvent(bool isPaused, string reason = null, GameLoopSignalIdentity identity = null)
        {
            IsPaused = isPaused;
            Reason = reason;
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                reason,
                nameof(GamePauseCommandEvent),
                handshake: nameof(GamePauseCommandEvent));
        }

        public bool IsPaused { get; }
        public string Reason { get; }
        public GameLoopSignalIdentity Identity { get; }
    }

    /// <summary>
    /// Hook precoce para preparar sistemas quando a pausa vai entrar.
    /// </summary>
    public sealed class PauseWillEnterEvent : IEvent
    {
        public PauseWillEnterEvent(string reason = null) => Reason = reason;
        public string Reason { get; }
    }

    /// <summary>
    /// Hook precoce para preparar sistemas quando a pausa vai sair.
    /// </summary>
    public sealed class PauseWillExitEvent : IEvent
    {
        public PauseWillExitEvent(string reason = null) => Reason = reason;
        public string Reason { get; }
    }

    /// <summary>
    /// Hook oficial de observacao do estado canonico de pause.
    /// </summary>
    public sealed class PauseStateChangedEvent : IEvent
    {
        public PauseStateChangedEvent(bool isPaused, GameLoopSignalIdentity identity = null)
        {
            IsPaused = isPaused;
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                isPaused ? "pause_entered" : "pause_exited",
                nameof(PauseStateChangedEvent),
                handshake: nameof(PauseStateChangedEvent));
        }

        public bool IsPaused { get; }
        public GameLoopSignalIdentity Identity { get; }
    }

    /// <summary>
    /// Resultado final da run atual.
    /// </summary>
    public enum GameRunOutcome
    {
        Unknown = 0,
        Victory = 1,
        Defeat = 2,
    }

    /// <summary>
    /// Evento de alto nivel para solicitar o encerramento da run.
    ///
    /// Este evento e a "entrada" recomendada em producao para vitoria/derrota.
    /// Diferentes condicoes podem dispara-lo (timer, morte do player, objetivos, sequencia de eventos etc.)
    /// sem amarrar a logica de encerramento a um unico sistema.
    /// </summary>
    public sealed class GameRunEndRequestedEvent : IEvent
    {
        public GameRunEndRequestedEvent(GameRunOutcome outcome, string reason = null, GameLoopSignalIdentity identity = null)
        {
            Outcome = outcome;
            Reason = reason;
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                reason,
                nameof(GameRunEndRequestedEvent),
                handshake: nameof(GameRunEndRequestedEvent));
        }

        public GameRunOutcome Outcome { get; }
        public string Reason { get; }
        public GameLoopSignalIdentity Identity { get; }
    }

    /// <summary>
    /// Representa o fim da run atual do jogo, para orquestrar pos-gameplay.
    /// </summary>
    public sealed class GameRunEndedEvent : IEvent
    {
        public GameRunEndedEvent(GameRunOutcome outcome, string reason = null, GameLoopSignalIdentity identity = null)
        {
            Outcome = outcome;
            Reason = reason;
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                reason,
                nameof(GameRunEndedEvent),
                handshake: nameof(GameRunEndedEvent));
        }

        /// <summary>
        /// Resultado da run (vitoria/derrota).
        /// </summary>
        public GameRunOutcome Outcome { get; }

        /// <summary>
        /// Texto livre para logs (ex.: "AllPlanetsDestroyed", "BossDefeated", "QA_ForcedEnd").
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Identidade explicita minima do encerramento.
        /// </summary>
        public GameLoopSignalIdentity Identity { get; }
    }

    /// <summary>
    /// Evento de telemetria para mudancas de atividade do GameLoop.
    /// Permite que outros sistemas (UI, QA, etc.) observem quando o loop entra/sai de estados ativos.
    /// </summary>
    public sealed class GameLoopActivityChangedEvent : IEvent
    {
        public GameLoopActivityChangedEvent(GameLoopStateId currentStateId, bool isActive)
        {
            CurrentStateId = currentStateId;
            IsActive = isActive;
        }

        /// <summary>
        /// Estado atual do GameLoop apos a mudanca.
        /// </summary>
        public GameLoopStateId CurrentStateId { get; }

        /// <summary>
        /// Indica se o jogo esta em um estado considerado "ativo" (ex.: Playing).
        /// </summary>
        public bool IsActive { get; }
    }

    public sealed class GameRunStartedEvent : IEvent
    {
        public GameRunStartedEvent(GameLoopStateId stateId, GameLoopSignalIdentity identity = null)
        {
            StateId = stateId;
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                stateId.ToString(),
                nameof(GameRunStartedEvent),
                handshake: nameof(GameRunStartedEvent));
        }

        /// <summary>
        /// Estado atual do GameLoop no momento em que a run e iniciada.
        /// </summary>
        public GameLoopStateId StateId { get; }

        /// <summary>
        /// Identidade explicita minima do start observado.
        /// </summary>
        public GameLoopSignalIdentity Identity { get; }
    }

    public sealed class GameResumeRequestedEvent : IEvent
    {
        public GameResumeRequestedEvent(string reason = null, GameLoopSignalIdentity identity = null)
        {
            Reason = reason;
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                reason,
                nameof(GameResumeRequestedEvent),
                handshake: nameof(GameResumeRequestedEvent));
        }

        public string Reason { get; }
        public GameLoopSignalIdentity Identity { get; }
    }

    /// <summary>
    /// REQUEST (intencao): "reiniciar a run" (Restart).
    /// </summary>
    public sealed class GameResetRequestedEvent : IEvent
    {
        public GameResetRequestedEvent(string reason = null, GameLoopSignalIdentity identity = null)
        {
            Reason = GameLoopReasonFormatter.NormalizeOptional(reason, "Restart/Unspecified");
            Identity = identity ?? GameLoopSignalIdentity.TechnicalInternal(
                Reason,
                nameof(GameResetRequestedEvent),
                handshake: nameof(GameResetRequestedEvent));
        }

        public string Reason { get; }
        public GameLoopSignalIdentity Identity { get; }
    }
}

