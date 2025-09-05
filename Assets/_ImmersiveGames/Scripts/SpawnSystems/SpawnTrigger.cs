using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    /// <summary>
    /// Factory para criação de gatilhos de spawn baseados na configuração
    /// </summary>
    public static class SpawnTriggerFactory
    {
        /// <summary>
        /// Lista de tipos de gatilhos suportados
        /// </summary>
        public static readonly List<string> SupportedTriggerTypes = new List<string> { "KeyPress" };

        /// <summary>
        /// Cria um gatilho de spawn com base na configuração fornecida
        /// </summary>
        /// <param name="config">Configuração do gatilho</param>
        /// <returns>Instância do gatilho ou null se inválido</returns>
        public static ISpawnTrigger CreateTrigger(SpawnSystem.TriggerConfig config)
        {
            if (!ValidateTriggerConfig(config))
                return null;

            string type = config.type.ToLower();

            return type switch
            {
                "keypress" => new KeyPressTrigger(config.key),
                _ => LogUnknownTriggerError(config.type)
            };
        }

        private static bool ValidateTriggerConfig(SpawnSystem.TriggerConfig config)
        {
            if (config == null)
            {
                DebugUtility.LogError(typeof(SpawnTriggerFactory), "TriggerConfig is null.");
                return false;
            }

            if (!SupportedTriggerTypes.Contains(config.type, System.StringComparer.OrdinalIgnoreCase))
            {
                DebugUtility.LogError(typeof(SpawnTriggerFactory), 
                    $"Unknown trigger type: {config.type}. Supported types: {string.Join(", ", SupportedTriggerTypes)}.");
                return false;
            }

            return true;
        }

        private static ISpawnTrigger LogUnknownTriggerError(string triggerType)
        {
            DebugUtility.LogError(typeof(SpawnTriggerFactory), 
                $"Unknown trigger type: {triggerType}. Supported types: {string.Join(", ", SupportedTriggerTypes)}.");
            return null;
        }
    }

    /// <summary>
    /// Gatilho que aciona o spawn quando uma tecla específica é pressionada
    /// </summary>
    public class KeyPressTrigger : ISpawnTrigger
    {
        private readonly KeyCode _key;

        /// <summary>
        /// Cria um gatilho de tecla com a tecla especificada
        /// </summary>
        /// <param name="key">Tecla que ativa o spawn</param>
        public KeyPressTrigger(KeyCode key)
        {
            _key = key;
        }

        /// <summary>
        /// Verifica se o spawn deve ocorrer (tecla pressionada)
        /// </summary>
        /// <returns>True se a tecla configurada foi pressionada</returns>
        public bool ShouldSpawn()
        {
            bool shouldSpawn = Input.GetKeyDown(_key);

            if (shouldSpawn)
            {
                LogTriggerActivated();
            }

            return shouldSpawn;
        }

        private void LogTriggerActivated() => 
            DebugUtility.Log<KeyPressTrigger>($"Trigger activated for key: {_key}.", "cyan");
    }
}