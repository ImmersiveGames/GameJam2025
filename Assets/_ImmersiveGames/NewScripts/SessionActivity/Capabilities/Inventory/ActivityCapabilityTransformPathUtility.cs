using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    internal static class ActivityCapabilityTransformPathUtility
    {
        public static string BuildTransformPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            string path = target.name;
            var current = target.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }

            return path;
        }
    }
}
