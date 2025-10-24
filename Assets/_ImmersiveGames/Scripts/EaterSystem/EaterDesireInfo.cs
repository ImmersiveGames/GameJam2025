using _ImmersiveGames.Scripts.PlanetSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    /// <summary>
    /// Representa o estado atual dos desejos do Eater, permitindo que consumidores externos
    /// descubram qual recurso está sendo desejado e se ele está disponível.
    /// </summary>
    public readonly struct EaterDesireInfo
    {
        public static readonly EaterDesireInfo Inactive = new(false, false, null, false, 0, 0f, 0f, 0f);

        public EaterDesireInfo(
            bool serviceActive,
            bool hasDesire,
            PlanetResources? resource,
            bool isAvailable,
            int availableCount,
            float weight,
            float duration,
            float remainingTime)
        {
            ServiceActive = serviceActive;
            HasDesire = hasDesire;
            Resource = resource;
            IsAvailable = isAvailable;
            AvailableCount = availableCount;
            Weight = weight;
            Duration = duration;
            RemainingTime = remainingTime;
        }

        /// <summary>
        /// Indica se o serviço de desejos está ativo.
        /// </summary>
        public bool ServiceActive { get; }

        /// <summary>
        /// Indica se existe um desejo atual.
        /// </summary>
        public bool HasDesire { get; }

        /// <summary>
        /// Tipo de recurso desejado, quando houver.
        /// </summary>
        public PlanetResources? Resource { get; }

        /// <summary>
        /// Facilita o acesso ao recurso desejado sem precisar lidar com nulos.
        /// </summary>
        public bool HasResource => Resource.HasValue;

        /// <summary>
        /// Indica se há planetas ativos que atendem ao desejo atual.
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        /// Quantidade de planetas ativos capazes de satisfazer o desejo.
        /// </summary>
        public int AvailableCount { get; }

        /// <summary>
        /// Peso calculado para o desejo durante o sorteio.
        /// </summary>
        public float Weight { get; }

        /// <summary>
        /// Duração prevista do desejo atual em segundos.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// Tempo restante do desejo atual em segundos.
        /// </summary>
        public float RemainingTime { get; }

        /// <summary>
        /// Obtém o recurso desejado quando disponível.
        /// </summary>
        public bool TryGetResource(out PlanetResources resource)
        {
            if (Resource.HasValue)
            {
                resource = Resource.Value;
                return true;
            }

            resource = default;
            return false;
        }
    }
}
