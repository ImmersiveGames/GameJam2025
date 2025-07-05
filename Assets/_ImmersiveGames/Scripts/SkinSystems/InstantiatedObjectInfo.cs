using _ImmersiveGames.Scripts.Utils.SelectorGeneric;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    public class InstantiatedObjectInfo
    {
        public GameObject GameObject { get; }
        public ObjectRootType RootType { get; }
        public string Tag { get; }

        public InstantiatedObjectInfo(GameObject go, ObjectRootType rootType, string tag)
        {
            GameObject = go;
            RootType = rootType;
            Tag = tag;
        }
    }

}