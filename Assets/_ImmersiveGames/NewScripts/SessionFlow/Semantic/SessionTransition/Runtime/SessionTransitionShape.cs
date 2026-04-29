using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    /// <summary>
    /// Composition is the declarative shape of the plan: axes, logical order and intents.
    /// It does not execute anything.
    /// </summary>
    public readonly struct SessionTransitionComposition
    {
        private readonly SessionTransitionAxisId[] _orderedAxes;

        public SessionTransitionComposition(
            SessionTransitionAxisMap axisMap,
            SessionTransitionContinuityShape continuityShape,
            SessionTransitionReconstructionShape reconstructionShape,
            IReadOnlyList<SessionTransitionAxisId> orderedAxes)
        {
            AxisMap = axisMap;
            ContinuityShape = continuityShape;
            ReconstructionShape = reconstructionShape;
            _orderedAxes = NormalizeOrderedAxes(orderedAxes);
            IntentKind = axisMap.IntentKind;
            PhaseIntent = axisMap.PhaseTransition;
            WorldResetIntent = axisMap.WorldReset;
            ContentSpawnIntent = axisMap.ContentSpawn;
            CarryOverIntent = axisMap.CarryOver;
        }

        public SessionTransitionAxisMap AxisMap { get; }
        public SessionTransitionContinuityShape ContinuityShape { get; }
        public SessionTransitionReconstructionShape ReconstructionShape { get; }
        public SessionTransitionIntentKind IntentKind { get; }
        public SessionTransitionPhaseAction PhaseIntent { get; }
        public SessionTransitionResetAction WorldResetIntent { get; }
        public bool ContentSpawnIntent { get; }
        public bool CarryOverIntent { get; }
        public IReadOnlyList<SessionTransitionAxisId> OrderedAxes => _orderedAxes;

        public bool HasAxis(SessionTransitionAxisId axis)
        {
            for (int i = 0; i < _orderedAxes.Length; i++)
            {
                if (_orderedAxes[i] == axis)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            string axes = _orderedAxes.Length == 0 ? "<none>" : string.Join(">", _orderedAxes);
            return $"OrderedAxes='{axes}', Intent='{IntentKind}', ContinuityShape=[{ContinuityShape}], ReconstructionShape=[{ReconstructionShape}], PhaseIntent='{PhaseIntent}', WorldResetIntent='{WorldResetIntent}', ContentSpawnIntent='{ContentSpawnIntent}', CarryOverIntent='{CarryOverIntent}'";
        }

        private static SessionTransitionAxisId[] NormalizeOrderedAxes(IReadOnlyList<SessionTransitionAxisId> orderedAxes)
        {
            if (orderedAxes == null || orderedAxes.Count == 0)
            {
                return Array.Empty<SessionTransitionAxisId>();
            }

            var axes = new List<SessionTransitionAxisId>(orderedAxes.Count);
            for (int i = 0; i < orderedAxes.Count; i++)
            {
                SessionTransitionAxisId axis = orderedAxes[i];
                if (axes.Contains(axis))
                {
                    continue;
                }

                axes.Add(axis);
            }

            return axes.ToArray();
        }
    }

    /// <summary>
    /// Execution is the operational shape of the plan: the concrete effect that will be dispatched.
    /// It does not define the composition of axes.
    /// </summary>
    public readonly struct SessionTransitionExecution
    {
        public SessionTransitionExecution(
            SessionTransitionExecutionKind kind,
            SessionTransitionHandoffAction handoffAction)
        {
            Kind = kind;
            HandoffAction = handoffAction;
        }

        public SessionTransitionExecutionKind Kind { get; }
        public SessionTransitionHandoffAction HandoffAction { get; }
        public bool RequiresPhaseLocalEntryReady => RequiresPhaseLocalEntryReadyFor(Kind);
        public bool IsNoOp => Kind == SessionTransitionExecutionKind.NoOp;

        public static bool RequiresPhaseLocalEntryReadyFor(SessionTransitionExecutionKind kind)
        {
            return kind switch
            {
                SessionTransitionExecutionKind.InitialEntry => true,
                SessionTransitionExecutionKind.NextPhase => true,
                SessionTransitionExecutionKind.ResetCurrentPhase => true,
                SessionTransitionExecutionKind.RestartFromFirstPhase => true,
                SessionTransitionExecutionKind.PhaseOrdinalNavigation => true,
                SessionTransitionExecutionKind.ExitToMenu => false,
                SessionTransitionExecutionKind.NoOp => false,
                _ => false,
            };
        }

        public override string ToString()
        {
            return $"Kind='{Kind}', RequiresPhaseLocalEntryReady='{RequiresPhaseLocalEntryReady}', HandoffAction='{HandoffAction}'";
        }
    }

    public enum SessionTransitionExecutionKind
    {
        NoOp = 0,
        NextPhase = 1,
        ResetCurrentPhase = 2,
        ExitToMenu = 3,
        InitialEntry = 4,  /// Entrada inicial (Menu â†’ Gameplay ou SceneFlow primeira fase)
        RestartFromFirstPhase = 5,
        PhaseOrdinalNavigation = 6,
    }

    [Flags]
    public enum SessionTransitionPreservationMask
    {
        None = 0,
        SessionState = 1 << 0,
        PhaseState = 1 << 1,
        WorldState = 1 << 2,
        ContentState = 1 << 3,
        ActorState = 1 << 4,
        ObjectState = 1 << 5,
    }

    public enum SessionTransitionResetScopeKind
    {
        None = 0,
        Phase = 1,
        Scene = 2,
        World = 3,
    }

    public enum SessionTransitionCarryOverKind
    {
        None = 0,
        Selective = 1,
        Full = 2,
    }

    public enum SessionTransitionReconstructionKind
    {
        None = 0,
        ReentryAfterReset = 1,
        RebuildAndReentry = 2,
    }

    public readonly struct SessionTransitionContinuityShape
    {
        public SessionTransitionContinuityShape(
            SessionTransitionPreservationMask preservation,
            SessionTransitionResetScopeKind resetScope,
            SessionTransitionCarryOverKind carryOver)
        {
            Preservation = preservation;
            ResetScope = resetScope;
            CarryOver = carryOver;
        }

        public SessionTransitionPreservationMask Preservation { get; }
        public SessionTransitionResetScopeKind ResetScope { get; }
        public SessionTransitionCarryOverKind CarryOver { get; }

        public bool Preserves(SessionTransitionPreservationMask mask) => (Preservation & mask) == mask;

        public override string ToString()
        {
            return $"Preservation='{Preservation}', ResetScope='{ResetScope}', CarryOver='{CarryOver}'";
        }
    }

    public readonly struct SessionTransitionReconstructionShape
    {
        public SessionTransitionReconstructionShape(
            SessionTransitionReconstructionKind kind,
            SessionTransitionResetScopeKind resetBoundary)
        {
            Kind = kind;
            ResetBoundary = resetBoundary;
        }

        public SessionTransitionReconstructionKind Kind { get; }
        public SessionTransitionResetScopeKind ResetBoundary { get; }

        public bool IsActive => Kind != SessionTransitionReconstructionKind.None;

        public override string ToString()
        {
            return $"Kind='{Kind}', ResetBoundary='{ResetBoundary}'";
        }
    }
}

