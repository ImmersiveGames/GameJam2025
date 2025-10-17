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

            foreach (var command in _commands)
            {
                if (!command.Execute(context))
                {
                    UndoInternal(context, executed);
                    return false;
                }

                executed.Add(command);
            }

            if (executed.Count > 0)
            {
                _history.Push(new DamageCommandRecord(context, executed));
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
            UndoInternal(record.Context, record.ExecutedCommands);
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
    }
}
