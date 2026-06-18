using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    [Serializable]
    public readonly struct ActorImpactContact
    {
        public ActorImpactContact(
            bool hasContact,
            Vector3 point,
            Vector3 normal)
        {
            HasContact = hasContact;
            Point = point;
            Normal = normal;
        }

        public bool HasContact { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }

        public static ActorImpactContact None => new(false, Vector3.zero, Vector3.zero);

        public static ActorImpactContact FromCollision(Collision collision)
        {
            if (collision == null || collision.contactCount <= 0)
            {
                return None;
            }

            ContactPoint contactPoint = collision.GetContact(0);
            return new ActorImpactContact(
                true,
                contactPoint.point,
                contactPoint.normal);
        }
    }
}
