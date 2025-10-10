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
        [SerializeField] private string configName = "Skin";
        [SerializeField] private ModelType modelType = ModelType.ModelRoot;
        [SerializeField] private List<GameObject> prefabs = new();
        [SerializeField] private InstantiationMode instantiationMode = InstantiationMode.First;
        [SerializeField] private int specificIndex;
        
        [SerializeField] private Vector3 position = Vector3.zero;
        [SerializeField] private Vector3 rotation = Vector3.zero;
        [SerializeField] private Vector3 scale = Vector3.one;
        
        // Novo campo para estado ativo
        [SerializeField] private bool activeState = true;

        public string ConfigName => configName;
        public ModelType ModelType => modelType;
        
        public Vector3 GetPosition() => position;
        public Vector3 GetRotation() => rotation;
        public Vector3 GetScale() => scale;
        public bool GetActiveState() => activeState;
        public List<GameObject> GetSelectedPrefabs()
        {
            if (prefabs == null || prefabs.Count == 0) return new List<GameObject>();

            return instantiationMode switch
            {
                InstantiationMode.All => prefabs,
                InstantiationMode.First => new List<GameObject> { prefabs[0] },
                InstantiationMode.Random => new List<GameObject> { prefabs[Random.Range(0, prefabs.Count)] },
                InstantiationMode.Specific when specificIndex >= 0 && specificIndex < prefabs.Count =>
                    new List<GameObject> { prefabs[specificIndex] },
                InstantiationMode.Specific => new List<GameObject> { prefabs[0] }, // fallback
                _ => new List<GameObject>()
            };
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