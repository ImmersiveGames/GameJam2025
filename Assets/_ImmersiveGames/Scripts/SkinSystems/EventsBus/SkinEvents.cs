using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.EventsBus
{
    public class FxToggleEvent : IEvent
    {
        public string FxTag { get; }
        public bool Active{ get; }

        public FxToggleEvent(string fxTag, bool active)
        {
            FxTag = fxTag;
            Active = active;
        }
    }

    public class SkinElementToggledEvent : IEvent
    {
        public string Tag{ get; }
        public GameObject Instance{ get; }
        public bool Active{ get; }

        public SkinElementToggledEvent(string tag, GameObject instance, bool active)
        {
            Tag = tag;
            Instance = instance;
            Active = active;
        }
    }
}