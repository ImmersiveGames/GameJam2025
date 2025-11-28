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
        public ModelType ModelType;

        /// <summary>
        /// Bounds em espaço de mundo que englobam todas as instâncias da skin.
        /// Calculado usando CalculateRealLength para lidar com objetos compostos.
        /// </summary>
        public Bounds WorldBounds;

        /// <summary>
        /// Centro do bounds em espaço de mundo.
        /// </summary>
        public Vector3 Center => WorldBounds.center;

        /// <summary>
        /// Tamanho (largura, altura, profundidade) do bounds em espaço de mundo.
        /// </summary>
        public Vector3 Size => WorldBounds.size;

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
            ModelType = modelType;
            WorldBounds = worldBounds;
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
            WorldBounds.size.x > 0f || WorldBounds.size.y > 0f || WorldBounds.size.z > 0f;
    }
}
