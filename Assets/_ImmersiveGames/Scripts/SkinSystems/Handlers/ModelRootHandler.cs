using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Handlers
{
    public class GenericRootHandler : IRootHandler
    {
        private readonly string _rootName;
        private readonly System.Type _tagComponentType;
        private Transform _rootTransform;

        public GenericRootHandler(string rootName, System.Type tagComponentType)
        {
            _rootName = rootName;
            _tagComponentType = tagComponentType;
        }

        public Transform GetOrCreateRoot(Transform parent)
        {
            if (_rootTransform != null) return _rootTransform;

            var go = new GameObject(_rootName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.AddComponent(_tagComponentType);
            _rootTransform = go.transform;

            return _rootTransform;
        }

        public void Clear()
        {
            if (_rootTransform == null) return;

            for (int i = _rootTransform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_rootTransform.GetChild(i).gameObject);
            }
        }

        public void SetActive(bool active)
        {
            if (_rootTransform != null)
                _rootTransform.gameObject.SetActive(active);
        }
    }
}