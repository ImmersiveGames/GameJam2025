using System;
using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Configs
{
    /// <summary>
    /// Mapa lógico de grupos de cena.
    /// 
    /// Objetivos:
    /// - Centralizar quais SceneGroupProfile representam:
    ///   - Menu padrão;
    ///   - Gameplay padrão;
    ///   - Grupo inicial do jogo;
    ///   - Outros grupos nomeados (Boss_01, Cutscene_Intro, Hub, etc.).
    /// - Permitir que o código de alto nível peça transições usando chaves lógicas
    ///   em vez de nomes de cenas ou enums fixos.
    /// 
    /// Não conhece SceneTransitionService, GameManager, etc.
    /// É puramente dado.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneFlowMap",
        menuName = "ImmersiveGames/Scene Flow/Scene Flow Map",
        order = 2)]
    
    public class SceneFlowMap : ScriptableObject
    {
        [Header("Grupos Padrão")]
        [Tooltip("Grupo inicial ao abrir o jogo (opcional). Se não definido, o bootstrap decide.")]
        [SerializeField] private SceneGroupProfile initialGroup;

        [Tooltip("Grupo lógico padrão do Menu. Usado por fluxos como ReturnToMenu, etc.")]
        [SerializeField] private SceneGroupProfile menuGroup;

        [Tooltip("Grupo lógico padrão de Gameplay. Usado por fluxos como StartGameplay / ResetGame.")]
        [SerializeField] private SceneGroupProfile gameplayGroup;

        [Header("Grupos Nomeados (Extensões)")]
        [Tooltip("Mapa genérico de chave lógica → SceneGroupProfile.")]
        [SerializeField] private List<NamedGroupEntry> namedGroups = new List<NamedGroupEntry>();

        /// <summary>
        /// Entrada (linha) do mapa: key lógica → grupo.
        /// Ex.: key = 'Boss_01', group = Boss01GroupProfile.
        /// </summary>
        [Serializable]
        public class NamedGroupEntry
        {
            [Tooltip("Chave lógica única (ex.: 'Boss_01', 'Cutscene_Intro', 'HubPlanet').")]
            public string key;

            [Tooltip("Grupo de cena associado a esta chave lógica.")]
            public SceneGroupProfile group;
        }

        public SceneGroupProfile InitialGroup => initialGroup;
        public SceneGroupProfile MenuGroup => menuGroup;
        public SceneGroupProfile GameplayGroup => gameplayGroup;

        public IReadOnlyList<NamedGroupEntry> NamedGroups => namedGroups;

        /// <summary>
        /// Obtém um grupo por chave lógica.
        /// Retorna null se não encontrar.
        /// </summary>
        public SceneGroupProfile GetGroupByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || namedGroups == null)
                return null;

            for (int i = 0; i < namedGroups.Count; i++)
            {
                var entry = namedGroups[i];
                if (entry != null && string.Equals(entry.key, key, StringComparison.Ordinal))
                {
                    return entry.group;
                }
            }

            return null;
        }

        /// <summary>
        /// Tenta obter um grupo por chave lógica.
        /// </summary>
        public bool TryGetGroupByKey(string key, out SceneGroupProfile group)
        {
            group = GetGroupByKey(key);
            return group != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (namedGroups == null)
                namedGroups = new List<NamedGroupEntry>();

            // Apenas detecta chaves duplicadas e avisa, sem apagar nada.
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in namedGroups)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                if (!seenKeys.Add(entry.key))
                {
                    Debug.LogWarning(
                        $"[SceneFlowMap] Chave duplicada '{entry.key}' detectada em '{name}'. " +
                        "Chaves devem ser únicas (ajuste manualmente no inspector).");
                }
            }
        }
#endif
    }
}
