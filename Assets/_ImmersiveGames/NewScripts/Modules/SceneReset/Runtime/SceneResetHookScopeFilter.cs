using System;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Interop;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;

namespace _ImmersiveGames.NewScripts.Modules.SceneReset.Runtime
{
    internal sealed class SceneResetHookScopeFilter
    {
        private readonly WorldResetContext? _resetContext;

        public SceneResetHookScopeFilter(WorldResetContext? resetContext)
        {
            _resetContext = resetContext;
        }

        public bool ShouldInclude(object candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (_resetContext == null)
            {
                return true;
            }

            if (candidate is IActorGroupRearmWorldParticipant scopedParticipant)
            {
                return _resetContext.Value.ContainsScope(scopedParticipant.Scope);
            }

            return false;
        }

        public int CompareResetScopeParticipants(IActorGroupRearmWorldParticipant left, IActorGroupRearmWorldParticipant right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int scopeComparison = left.Scope.CompareTo(right.Scope);
            if (scopeComparison != 0)
            {
                return scopeComparison;
            }

            int orderComparison = left.Order.CompareTo(right.Order);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            string leftType = left.GetType().FullName ?? left.GetType().Name;
            string rightType = right.GetType().FullName ?? right.GetType().Name;
            return string.Compare(leftType, rightType, StringComparison.Ordinal);
        }

        public int CompareScope(object left, object right)
        {
            if (_resetContext == null)
            {
                return 0;
            }

            int leftScope = GetParticipantScope(left);
            int rightScope = GetParticipantScope(right);
            return leftScope.CompareTo(rightScope);
        }

        private static int GetParticipantScope(object participant)
        {
            return participant is IActorGroupRearmWorldParticipant scoped ? (int)scoped.Scope : int.MaxValue;
        }
    }
}
