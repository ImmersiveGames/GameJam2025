using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class DamageCommandInvoker
    {
        private readonly List<IDamageCommand> _commands;
        private readonly Stack<DamageCommandRecord> _history = new();

        public DamageCommandInvoker(IEnumerable<IDamageCommand> commands)
        {
            _commands = commands?.Where(command => command != null).ToList() ?? new List<IDamageCommand>();
        }

        public bool Execute(DamageCommandContext context)
        {
            if (context == null)
            {
                return false;
            }

            var executed = new List<IDamageCommand>();
            RaisePipelineStarted(context);

            foreach (var command in _commands)
            {
                if (!command.Execute(context))
                {
                    UndoInternal(context, executed);
                    RaisePipelineFailed(context, command);
                    return false;
                }

                executed.Add(command);
            }

            if (executed.Count > 0)
            {
                _history.Push(new DamageCommandRecord(context, executed));
                RaisePipelineCompleted(context);
            }

            return true;
        }

        public bool UndoLast()
        {
            if (_history.Count == 0)
            {
                return false;
            }

            var record = _history.Pop();
            var restoredDamage = record.Context?.CalculatedDamage ?? 0f;
            UndoInternal(record.Context, record.ExecutedCommands);
            RaisePipelineUndo(record.Context, restoredDamage);
            return true;
        }

        private static void UndoInternal(DamageCommandContext context, IList<IDamageCommand> executed)
        {
            if (context == null || executed == null)
            {
                return;
            }

            for (int i = executed.Count - 1; i >= 0; i--)
            {
                executed[i]?.Undo(context);
            }
        }

        private sealed class DamageCommandRecord
        {
            public DamageCommandContext Context { get; }
            public IList<IDamageCommand> ExecutedCommands { get; }

            public DamageCommandRecord(DamageCommandContext context, IList<IDamageCommand> executedCommands)
            {
                Context = context;
                ExecutedCommands = executedCommands;
            }
        }

        private static void RaisePipelineStarted(DamageCommandContext context)
        {
            var request = context.Request;
            if (request == null)
            {
                return;
            }

            var payload = new DamagePipelineStarted(
                request.AttackerId,
                request.TargetId,
                context.TargetResource,
                request.DamageType,
                request.DamageValue);

            DamageEventDispatcher.RaiseForParticipants(payload, request.AttackerId, request.TargetId);
        }

        private static void RaisePipelineCompleted(DamageCommandContext context)
        {
            var request = context.Request;
            if (request == null)
            {
                return;
            }

            var payload = new DamagePipelineCompleted(
                request.AttackerId,
                request.TargetId,
                context.TargetResource,
                request.DamageType,
                request.DamageValue,
                context.CalculatedDamage);

            DamageEventDispatcher.RaiseForParticipants(payload, request.AttackerId, request.TargetId);
        }

        private static void RaisePipelineFailed(DamageCommandContext context, IDamageCommand failedCommand)
        {
            var request = context.Request;
            if (request == null)
            {
                return;
            }

            var payload = new DamagePipelineFailed(
                request.AttackerId,
                request.TargetId,
                context.TargetResource,
                request.DamageType,
                request.DamageValue,
                failedCommand?.GetType().Name ?? "UnknownCommand");

            DamageEventDispatcher.RaiseForParticipants(payload, request.AttackerId, request.TargetId);
        }

        private static void RaisePipelineUndo(DamageCommandContext context, float restoredDamage)
        {
            var request = context.Request;
            if (request == null)
            {
                return;
            }

            var payload = new DamagePipelineUndone(
                request.AttackerId,
                request.TargetId,
                context.TargetResource,
                request.DamageType,
                restoredDamage);

            DamageEventDispatcher.RaiseForParticipants(payload, request.AttackerId, request.TargetId);
        }

    }
}
