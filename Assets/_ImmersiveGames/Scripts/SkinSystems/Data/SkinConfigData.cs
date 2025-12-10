using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    public interface ISkinConfig
    {
        string ConfigName { get; }
        ModelType ModelType { get; }
        List<GameObject> GetSelectedPrefabs();
        Vector3 GetPosition();
        Vector3 GetRotation();
        Vector3 GetScale();
        bool GetActiveState();
    }

    [CreateAssetMenu(fileName = "SkinConfigData", menuName = "ImmersiveGames/Skin/SkinConfigData", order = 1)]
    public class SkinConfigData : ScriptableObject, ISkinConfig
    {
        [Header("Identity")]
        [SerializeField] private string configName = "Skin";
        [SerializeField] private ModelType modelType = ModelType.ModelRoot;

        [Header("Prefabs")]
        [SerializeField] private List<GameObject> prefabs = new List<GameObject>();
        [SerializeField] private InstantiationMode instantiationMode = InstantiationMode.First;
        [SerializeField] private int specificIndex;

        [Header("Transform")]
        [SerializeField] private Vector3 position = Vector3.zero;
        [SerializeField] private Vector3 rotation = Vector3.zero;
        [SerializeField] private Vector3 scale = Vector3.one;

        [Header("Active State")]
        [SerializeField] private bool activeState = true;

        public string ConfigName => configName;
        public ModelType ModelType => modelType;

        public Vector3 GetPosition() => position;
        public Vector3 GetRotation() => rotation;
        public Vector3 GetScale() => scale;
        public bool GetActiveState() => activeState;

        public List<GameObject> GetSelectedPrefabs()
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                return new List<GameObject>();
            }

            switch (instantiationMode)
            {
                case InstantiationMode.All:
                    return new List<GameObject>(prefabs);

                case InstantiationMode.First:
                    return new List<GameObject> { prefabs[0] };

                case InstantiationMode.Random:
                    int index = Random.Range(0, prefabs.Count);
                    return new List<GameObject> { prefabs[index] };

                case InstantiationMode.Specific:
                    int safeIndex = Mathf.Clamp(specificIndex, 0, prefabs.Count - 1);
                    return new List<GameObject> { prefabs[safeIndex] };

                default:
                    return new List<GameObject>();
            }
        }
    }

    public enum ModelType
    {
        ModelRoot,
        CanvasRoot,
        FxRoot,
        SoundRoot,
        FlagMark,
    }

    public enum InstantiationMode
    {
        All,
        First,
        Random,
        Specific
    }
}
