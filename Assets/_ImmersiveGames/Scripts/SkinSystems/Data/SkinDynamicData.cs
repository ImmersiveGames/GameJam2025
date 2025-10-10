using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    [System.Serializable]
    public struct SkinDynamicData
    {
        public Vector3 scaleModifier;
        public Color colorModifier;
        public float progressValue;
        public string stateName;
        
        // Métodos de factory para casos comuns
        public static SkinDynamicData CreateProgressData(float progress) => new SkinDynamicData { progressValue = progress };
        public static SkinDynamicData CreateColorData(Color color) => new SkinDynamicData { colorModifier = color };
        public static SkinDynamicData CreateScaleData(Vector3 scale) => new SkinDynamicData { scaleModifier = scale };
    }
}