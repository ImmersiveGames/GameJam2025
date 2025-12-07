using System;
using UnityEngine;
using _ImmersiveGames.Scripts.SkinSystems.Data;

namespace _ImmersiveGames.Scripts.SkinSystems.Runtime
{
    /// <summary>
    /// Representa o estado geométrico em runtime de uma skin de um determinado ModelType.
    /// É focado em tamanho/posição real no mundo (Bounds).
    /// </summary>
    [Serializable]
    public struct SkinRuntimeState
    {
        /// <summary>
        /// Tipo de modelo ao qual esse estado se refere.
        /// </summary>
        public ModelType modelType;

        /// <summary>
        /// Bounds em espaço de mundo que englobam todas as instâncias da skin.
        /// Calculado usando CalculateRealLength para lidar com objetos compostos.
        /// </summary>
        public Bounds worldBounds;

        /// <summary>
        /// Centro do bounds em espaço de mundo.
        /// </summary>
        public Vector3 Center => worldBounds.center;

        /// <summary>
        /// Tamanho (largura, altura, profundidade) do bounds em espaço de mundo.
        /// </summary>
        public Vector3 Size => worldBounds.size;

        /// <summary>
        /// Maior dimensão entre X, Y e Z (útil para radius aproximado).
        /// </summary>
        public float MaxDimension => Mathf.Max(Size.x, Size.y, Size.z);

        /// <summary>
        /// Raio aproximado (metade da maior dimensão).
        /// Útil para colisão "esférica" simplificada ou distância mínima.
        /// </summary>
        public float ApproxRadius => MaxDimension * 0.5f;

        public SkinRuntimeState(ModelType modelType, Bounds worldBounds)
        {
            this.modelType = modelType;
            this.worldBounds = worldBounds;
        }

        /// <summary>
        /// Cria um estado vazio (sem bounds válidos).
        /// </summary>
        public static SkinRuntimeState Empty(ModelType modelType)
        {
            return new SkinRuntimeState(modelType, new Bounds(Vector3.zero, Vector3.zero));
        }

        /// <summary>
        /// Indica se o bounds é válido (tamanho maior que zero em pelo menos um eixo).
        /// </summary>
        public bool HasValidBounds =>
            worldBounds.size.x > 0f || worldBounds.size.y > 0f || worldBounds.size.z > 0f;
    }
}
