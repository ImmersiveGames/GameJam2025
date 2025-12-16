using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.Extensions
{

    public static class TransformExtensions
    {
        /// <summary>
        /// Checks if the specified transform or Actor is the same as this transform or a child of it.
        /// </summary>
        /// <param name="self">The parent transform.</param>
        /// <param name="other">The transform to check against this transform.</param>
        /// <returns>True if the transform is self or a descendant, false otherwise.</returns>
        public static bool IsChildOrSelf(this Transform self, Transform other)
        {
            return other == self || other.IsChildOf(self);
        }

        /// <summary>
        /// Checks if the specified Actor is this transform or one of its children.
        /// </summary>
        /// <param name="self">The parent transform.</param>
        /// <param name="other">The Actor to check.</param>
        /// <returns>True if the Actor is part of this transform hierarchy, false otherwise.</returns>
        public static bool IsChildOrSelf(this Transform self, GameObject other)
        {
            return other != null && self.IsChildOrSelf(other.transform);
        }

    }
}