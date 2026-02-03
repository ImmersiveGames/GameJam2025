using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.CameraSystems
{
    /// <summary>
    /// Serviço responsável por resolver qual câmera deve ser usada
    /// para Gameplay, UI e demais sistemas dependentes.
    /// Suporta múltiplas câmeras (para multiplayer futuro),
    /// permitindo registrar e recuperar câmeras por playerId.
    /// </summary>
    public interface IOldCameraResolver
    {
        /// <summary>Registra uma câmera como principal do jogador.</summary>
        void RegisterCamera(int playerId, Camera camera);

        /// <summary>Remove câmera registrada de um jogador.</summary>
        void UnregisterCamera(int playerId, Camera camera);

        /// <summary>Retorna a câmera associada ao playerId.</summary>
        Camera GetCamera(int playerId);

        /// <summary>Retorna a câmera padrão (player 0).</summary>
        Camera GetDefaultCamera();

        /// <summary>Evento disparado quando a câmera padrão muda.</summary>
        event System.Action<Camera> OnDefaultCameraChanged;

        /// <summary>
        /// Retorna um dicionário somente, leitura com todas as câmeras registradas.
        /// </summary>
        IReadOnlyDictionary<int, Camera> AllCameras { get; }
    }
}