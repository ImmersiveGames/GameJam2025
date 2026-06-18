using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems.Data
{
    [Serializable]
    public struct SkinDynamicData
    {
        public Vector3 scaleModifier;
        public Color colorModifier;
        public float progressValue;
        public string stateName;

        // Métodos de factory para casos comuns
        public static SkinDynamicData CreateProgressData(float progress)
        {
            return new SkinDynamicData
                { progressValue = progress };
        }
        public static SkinDynamicData CreateColorData(Color color)
        {
            return new SkinDynamicData
                { colorModifier = color };
        }
        public static SkinDynamicData CreateScaleData(Vector3 scale)
        {
            return new SkinDynamicData
                { scaleModifier = scale };
        }
    }
}
