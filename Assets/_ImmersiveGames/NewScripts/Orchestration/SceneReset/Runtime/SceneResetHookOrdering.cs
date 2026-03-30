using System;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime
{
    internal static class SceneResetHookOrdering
    {
        public static int CompareHookEntriesWithScope<THook>(
            (string Label, THook Hook) left,
            (string Label, THook Hook) right,
            SceneResetHookScopeFilter scopeFilter)
            where THook : class
        {
            int scopeComparison = scopeFilter?.CompareScope(left.Hook, right.Hook) ?? 0;
            if (scopeComparison != 0)
            {
                return scopeComparison;
            }

            return CompareHookEntries(left, right);
        }

        public static int CompareHookEntries<THook>((string Label, THook Hook) left, (string Label, THook Hook) right)
            where THook : class
        {
            int leftOrder = GetHookOrder(left.Hook);
            int rightOrder = GetHookOrder(right.Hook);
            int orderComparison = leftOrder.CompareTo(rightOrder);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            string leftTypeName = left.Hook?.GetType().FullName ?? string.Empty;
            string rightTypeName = right.Hook?.GetType().FullName ?? string.Empty;
            return string.Compare(leftTypeName, rightTypeName, StringComparison.Ordinal);
        }

        public static int GetHookOrder(object hook)
        {
            if (hook is ISceneResetHookOrdered orderedHook)
            {
                return orderedHook.Order;
            }

            return 0;
        }
    }
}
